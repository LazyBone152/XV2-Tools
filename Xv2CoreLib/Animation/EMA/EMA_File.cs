using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Xv2CoreLib.AnimationFramework;
using Xv2CoreLib.EAN;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.EMA
{
    public enum ValueType : ushort
    {
        Vector4 = 0, //Shader vector4, used exclusively by mat.ema
        Float16 = 1,
        Float32 = 2
    }

    public enum EmaType : ushort
    {
        obj = 3,
        light = 4,
        mat = 8
    }

    public enum EmaAnimationType : byte
    {
        obj = 0,
        cam = 1, //Not used in the Xenoverse games, but was in older games on the engine apparantly
        light = 2,
        mat = 3,
    }

    [Serializable]
    [YAXSerializeAs("EMA")]
    public class EMA_File
    {
        public const int EMA_SIGNATURE = 1095583011;

        [YAXDontSerialize]
        public bool HasSkeleton => Skeleton != null;


        [YAXAttributeForClass]
        public int Version { get; set; }
        [YAXAttributeForClass]
        public EmaType EmaType { get; set; }
        [YAXAttributeForClass]
        public int I_20 { get; set; }
        [YAXAttributeForClass]
        public int I_24 { get; set; }
        [YAXAttributeForClass]
        public int I_28 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Skeleton")]
        public Skeleton Skeleton { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Animation")]
        public AsyncObservableCollection<EMA_Animation> Animations { get; set; } = new AsyncObservableCollection<EMA_Animation>();

        #region LoadSave
        public static EMA_File Serialize(string path, bool writeXml)
        {
            byte[] bytes = File.ReadAllBytes(path);

            EMA_File emaFile = Load(bytes);
            //emaFile.AddInterpolatedKeyframes();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EMA_File));
                serializer.SerializeToFile(emaFile, path + ".xml");
            }

            return emaFile;
        }

        public static EMA_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static EMA_File Load(byte[] rawBytes)
        {
            EMA_File emaFile = new EMA_File();

            //Header
            emaFile.Version = BitConverter.ToInt32(rawBytes, 8);
            emaFile.EmaType = (EmaType)BitConverter.ToUInt16(rawBytes, 18);
            emaFile.I_20 = BitConverter.ToInt32(rawBytes, 20);
            emaFile.I_24 = BitConverter.ToInt32(rawBytes, 24);
            emaFile.I_28 = BitConverter.ToInt32(rawBytes, 28);

            int skeletonOffset = BitConverter.ToInt32(rawBytes, 12);
            ushort animationCount = BitConverter.ToUInt16(rawBytes, 16);
            int animationsStart = 32;

            //Parse skeleton
            if (skeletonOffset > 0)
            {
                emaFile.Skeleton = Skeleton.Parse(rawBytes, skeletonOffset);
            }

            //Parse animations
            for (int i = 0; i < animationCount; i++)
            {
                int animOffset = BitConverter.ToInt32(rawBytes, animationsStart + (i * 4));

                if (animOffset != 0)
                {
                    emaFile.Animations.Add(EMA_Animation.Read(rawBytes, BitConverter.ToInt32(rawBytes, animationsStart + (i * 4)), i, emaFile));
                }
            }

            /*
            //TEST REMOVE
            foreach(var anim in emaFile.Animations)
            {
                foreach(var node in anim.Nodes)
                {
                    foreach(var command in node.Commands)
                    {
                        for (int i = command.Keyframes.Count - 1; i >= 0; i--)
                        {
                            if(command.Keyframes[i].InterpolationType == KeyframeInterpolation.CubicBezier)
                            {
                                command.Keyframes.RemoveAt(i);
                            }
                        }
                    }
                }
            }
            */

            return emaFile;
        }

        public static void Deserialize(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(EMA_File), YAXSerializationOptions.DontSerializeNullObjects);
            EMA_File emaFile = (EMA_File)serializer.DeserializeFromFile(xmlPath);

            byte[] bytes = emaFile.Write();
            File.WriteAllBytes(saveLocation, bytes);
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int animCount = Animations.Count > 0 ? Animations.Max(x => x.Index) + 1 : 0;
            List<int> animNameOffsets = new List<int>();

            //Header
            bytes.AddRange(BitConverter.GetBytes(EMA_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)32));
            bytes.AddRange(BitConverter.GetBytes((int)Version));
            bytes.AddRange(new byte[4]); //Skeleton offset
            bytes.AddRange(BitConverter.GetBytes((ushort)animCount));
            bytes.AddRange(BitConverter.GetBytes((ushort)EmaType));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_28));

            //Animation pointers
            bytes.AddRange(new byte[4 * animCount]);

            //Animations
            foreach (var anim in Animations)
            {
                int animStartOffset = bytes.Count;
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 32 + (anim.Index * 4));

                List<float> values = anim.GetValues();

                bytes.AddRange(BitConverter.GetBytes((ushort)anim.GetEndFrame()));
                bytes.AddRange(BitConverter.GetBytes((ushort)anim.CommandCount));
                bytes.AddRange(BitConverter.GetBytes((int)values.Count));
                bytes.Add((byte)anim.EmaType);
                bytes.Add((byte)anim.LightUnknown);
                bytes.AddRange(BitConverter.GetBytes((ushort)anim.FloatPrecision));
                animNameOffsets.Add(bytes.Count);
                bytes.AddRange(new byte[4]); //Name offset
                bytes.AddRange(new byte[4]); //value offset
                bytes.AddRange(new byte[4 * anim.CommandCount]);

                //Commands
                int commandIndex = 0;
                foreach (EMA_Node node in anim.Nodes)
                {
                    if (node.Commands == null) continue;

                    foreach (EMA_Command command in node.Commands)
                    {
                        int startCommandOffset = bytes.Count;
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - animStartOffset), animStartOffset + 20 + (commandIndex * 4));

                        //Calculate time and index size (uint8 or uint16)
                        foreach (var keyframe in command.Keyframes)
                        {
                            if (keyframe.Time > byte.MaxValue)
                                command.Int16ForTime = true;
                            if (keyframe.index > byte.MaxValue)
                                command.Int16ForValueIndex = true;
                        }

                        if (HasSkeleton)
                        {
                            bytes.AddRange(BitConverter.GetBytes((ushort)GetBoneIndex(node.BoneName)));
                        }
                        else
                        {
                            bytes.AddRange(new byte[2]);
                        }

                        bytes.Add(command.Parameter);

                        var bitArray_b = new BitArray(new bool[8] { command.I_03_b1, command.Int16ForTime, command.Int16ForValueIndex, command.I_03_b4, false, false, false, false });
                        var bitArray_a = new BitArray(new byte[1] { command.Component });

                        bitArray_a[2] = command.NoInterpolation;
                        bitArray_a[3] = command.I_03_a4;
                        bytes.Add((byte)Int4Converter.GetByte(Utils.ConvertToByte(bitArray_a), Utils.ConvertToByte(bitArray_b)));
                        bytes.AddRange(BitConverter.GetBytes((ushort)command.KeyframeCount));
                        bytes.AddRange(new byte[2]);

                        //Write Time
                        foreach (var keyframe in command.Keyframes.OrderBy(x => x.Time))
                        {

                            if (command.Int16ForTime)
                            {
                                bytes.AddRange(BitConverter.GetBytes(keyframe.Time));
                            }
                            else
                            {
                                bytes.Add((byte)keyframe.Time);
                            }
                        }

                        //Add padding
                        bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count - startCommandOffset, 4)]);

                        //Write value/index
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((ushort)(bytes.Count - startCommandOffset)), startCommandOffset + 6);

                        foreach (EMA_Keyframe keyframe in command.Keyframes.OrderBy(x => x.Time))
                        {
                            if (command.Int16ForValueIndex)
                            {
                                bytes.AddRange(BitConverter.GetBytes((ushort)keyframe.index));
                                bytes.Add(0);
                                bytes.Add((byte)keyframe.InterpolationType);
                            }
                            else
                            {
                                bytes.Add((byte)keyframe.index);
                                bytes.Add((byte)keyframe.InterpolationType);
                            }
                        }

                        //Add padding
                        bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count - startCommandOffset, 4)]);

                        commandIndex++;
                    }
                }

                //Values
                int valuesStartOffset = bytes.Count;
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - animStartOffset), animStartOffset + 16);
                foreach (var value in values)
                {
                    if (anim.FloatPrecision == ValueType.Float16)
                    {
                        bytes.AddRange(Half.GetBytes((Half)value));
                    }
                    else if (anim.FloatPrecision == ValueType.Float32 || anim.FloatPrecision == ValueType.Vector4)
                    {
                        bytes.AddRange(BitConverter.GetBytes(value));
                    }
                    else
                    {
                        throw new InvalidDataException("Unknown ValueType. Cannot continue.");
                    }
                }
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count - valuesStartOffset, 4)]);

            }

            //Skeleton
            if (HasSkeleton)
            {
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 12);
                bytes.AddRange(Skeleton.Write());
            }

            //Strings (animations)
            for (int i = 0; i < Animations.Count; i++)
            {
                if (!String.IsNullOrWhiteSpace(Animations[i].Name))
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - animNameOffsets[i] + 12), animNameOffsets[i]);
                    bytes.AddRange(new byte[10]);
                    bytes.Add((byte)Animations[i].Name.Length);
                    bytes.AddRange(Encoding.ASCII.GetBytes(Animations[i].Name));
                    bytes.Add(0);
                }
            }

            return bytes.ToArray();
        }
        
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }
        #endregion

        public string GetBoneName(int boneIndex)
        {
            if (!HasSkeleton) throw new InvalidOperationException("EMA_File.GetBoneName: emaFile has no skeleton.");

            foreach (var bone in Skeleton.Bones)
            {
                if (bone.Index == boneIndex) return bone.Name;
            }

            return null;
        }

        public ushort GetBoneIndex(string boneName)
        {
            if (!HasSkeleton) throw new InvalidOperationException("EMA_File.GetBoneIndex: emaFile has no skeleton.");

            foreach (var bone in Skeleton.Bones)
            {
                if (bone.Name == boneName) return bone.Index;
            }

            throw new Exception(String.Format("Could not find a bone wih the name: {0}", boneName));
        }

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();
            if (Animations == null) Animations = new AsyncObservableCollection<EMA_Animation>();

            foreach (var anim in Animations)
            {
                colors.AddRange(anim.GetUsedColors());
            }

            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0, EMM.EMM_File emmFile = null)
        {
            if (Animations == null) return;
            if (undos == null) undos = new List<IUndoRedo>();

            foreach (EMA_Animation anim in Animations)
            {
                anim.ChangeHue(hue, saturation, lightness, undos, hueSet, variance, emmFile);
            }
        }

        #region Conversion
        public EAN_File ConvertToEan(bool isPersistentInstance)
        {
            if (EmaType != EmaType.obj) throw new InvalidOperationException($"EMA_File.ConvertToEan: this operation only supports obj ema files.");

            //Create copy of EMA so edits dont affect the original, because we need to add in interpolated keyframes when non-linear interpolation is used, as EAN does not support that
            EMA_File copyEma = isPersistentInstance ? Load(Write()) : this;
            copyEma.AddInterpolatedKeyframes();

            EAN_File ean = new EAN_File();
            ean.Skeleton = Skeleton.Convert().Skeleton;
            ean.Animations.AddRange(SerializedAnimation.DeserializeToEan(SerializedAnimation.Serialize(copyEma.Animations, ean.Skeleton), ean.Skeleton));

            return ean;
        }

        public void AddInterpolatedKeyframes()
        {
            //Manually add interpolated keyframes from the bezier curve definitions
            //Right now this is just for testing, but it will be needed for any EMA -> EAN conversions in the future

            foreach (EMA_Animation animation in Animations)
            {
                foreach (EMA_Node node in animation.Nodes)
                {
                    foreach (EMA_Command command in node.Commands)
                    {
                        int endFrame = animation.GetEndFrame();

                        for (int i = 0; i < endFrame; i++)
                        {
                            EMA_Keyframe keyframe = command.Keyframes.FirstOrDefault(x => x.Time == i);
                            if (keyframe == null) continue;

                            if (keyframe.InterpolationType == KeyframeInterpolation.CubicBezier || keyframe.InterpolationType == KeyframeInterpolation.QuadraticBezier)
                            {
                                EMA_Keyframe nextKeyframe = command.GetKeyframeAfter(i);
                                if (nextKeyframe?.Time == keyframe.Time + 1 || nextKeyframe == null) continue; //No interpolation required
                                if (nextKeyframe.Value == keyframe.Value && MathHelpers.FloatEquals(keyframe.ControlPoint1, 0f) && MathHelpers.FloatEquals(keyframe.ControlPoint2, 0f)) continue;

                                float controlPoint1 = keyframe.Value + keyframe.ControlPoint1;
                                float controlPoint2 = nextKeyframe.Value - keyframe.ControlPoint2;

                                for (int a = keyframe.Time + 1; a < nextKeyframe.Time; a++)
                                {
                                    float factor = (float)(a - keyframe.Time) / (float)(nextKeyframe.Time - keyframe.Time);
                                    float value = 0f;

                                    if (keyframe.InterpolationType == KeyframeInterpolation.CubicBezier)
                                    {
                                        value = MathHelpers.CubicBezier(factor, keyframe.Value, controlPoint1, controlPoint2, nextKeyframe.Value);
                                    }
                                    else if (keyframe.InterpolationType == KeyframeInterpolation.QuadraticBezier)
                                    {
                                        value = MathHelpers.QuadraticBezier(factor, keyframe.Value, controlPoint1, nextKeyframe.Value);
                                    }

                                    command.Keyframes.Add(new EMA_Keyframe(a, value));
                                }

                                //Remove interp from keyframe
                                keyframe.InterpolationType = KeyframeInterpolation.Linear;

                                //Jump straight to next keyframe
                                i = nextKeyframe.Time;
                            }
                        }

                        command.Keyframes.Sort((x, y) => x.Time - y.Time);
                    }

                }
            }
        }
        
        #endregion

        #region MaterialAnimation
        /// <summary>
        /// Updates all material names to correctly match the actual materials defined in the EMM file.
        /// </summary>
        public void FixMaterialNames(EMM.EMM_File emmFile)
        {
            //The EMA file uses the skeleton index to match material nodes with the actual materials. The node name is gibberish and means nothing. So, this method will rename them all to the correct name.
            //On save, the skeleton will be dynamically rereated based on the material file.

            foreach (EMA_Animation anim in Animations)
            {
                foreach (EMA_Node node in anim.Nodes)
                {
                    int materialIdx = Skeleton.Bones.IndexOf(Skeleton.Bones.FirstOrDefault(x => x.Name == node.BoneName));

                    if (materialIdx != -1 && materialIdx <= emmFile.Materials.Count - 1)
                    {
                        node.BoneName = emmFile.Materials[materialIdx].Name;
                    }
                }
            }

            for (int i = 0; i < Skeleton.Bones.Count; i++)
            {
                if (i != -1 && i <= emmFile.Materials.Count - 1)
                {
                    Skeleton.Bones[i].Name = emmFile.Materials[i].Name;
                }
            }
        }

        /// <summary>
        /// Dynamically creates the EMA skeleton to match the material index.
        /// </summary>
        public void CreateMaterialSkeleton(EMM.EMM_File emmFile)
        {
            Skeleton skeleton = new Skeleton();

            //Create bones for all materials
            for (int i = 0; i < emmFile.Materials.Count; i++)
            {
                Bone bone = new Bone();
                bone.Name = emmFile.Materials[i].Name;
                bone.Index = (ushort)i;
                bone.EmoPartIndex = 0;
                bone.I_08 = 0;
                bone.IKFlag = 1;
                bone.RelativeMatrix = SkeletonMatrix.ZeroMatrix();

                skeleton.Bones.Add(bone);
            }

            //Create bones for all nodes in this EMA file that aren't actually materials.
            //These nodes dont actually do anything, but to be in a sane state when saving this step is needed, becuases mats could be renamed or deleted...
            foreach (EMA_Animation anim in Animations)
            {
                foreach (EMA_Node node in anim.Nodes)
                {
                    if (skeleton.Bones.FirstOrDefault(x => x.Name == node.BoneName) == null)
                    {
                        Bone bone = new Bone();
                        bone.Name = node.BoneName;
                        bone.Index = (ushort)skeleton.Bones.Count;
                        bone.EmoPartIndex = 0;
                        bone.I_08 = 0;
                        bone.IKFlag = 1;
                        bone.RelativeMatrix = SkeletonMatrix.ZeroMatrix();

                        skeleton.Bones.Add(bone);
                    }
                }
            }

            Skeleton = skeleton;
        }
        #endregion
    }

    [Serializable]
    [YAXSerializeAs("Animation")]
    public class EMA_Animation
    {
        [YAXDontSerialize]
        public int CommandCount
        {
            get
            {
                if (Nodes == null) return 0;
                int count = 0;

                foreach (var node in Nodes)
                    count += node.Commands.Count;

                return count;
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public int Index { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public EmaAnimationType EmaType { get; set; }
        [YAXAttributeForClass]
        public byte LightUnknown { get; set; } //Light = 1, all other types = 0
        [YAXAttributeForClass]
        [YAXSerializeAs("FloatType")]
        public ValueType FloatPrecision { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Node")]
        public AsyncObservableCollection<EMA_Node> Nodes { get; set; } = new AsyncObservableCollection<EMA_Node>();

        #region LoadSave
        public static EMA_Animation Read(byte[] rawBytes, int offset, int index, EMA_File emaFile)
        {
            EMA_Animation animation = new EMA_Animation();
            animation.Index = index;
            //animation.EndFrame = BitConverter.ToUInt16(rawBytes, offset + 0);
            animation.EmaType = (EmaAnimationType)rawBytes[offset + 8];
            animation.LightUnknown = rawBytes[offset + 9];
            animation.FloatPrecision = (ValueType)BitConverter.ToUInt16(rawBytes, offset + 10);

            int commandCount = BitConverter.ToUInt16(rawBytes, offset + 2);
            int valueCount = BitConverter.ToInt32(rawBytes, offset + 4);
            int valueOffset = BitConverter.ToInt32(rawBytes, offset + 16) + offset;
            int nameOffset = (BitConverter.ToInt32(rawBytes, offset + 12) != 0) ? BitConverter.ToInt32(rawBytes, offset + 12) + offset : 0;

            //Name
            if (nameOffset > 0)
            {
                animation.Name = StringEx.GetString(rawBytes, nameOffset + 11, false, StringEx.EncodingType.UTF8);
            }

            //Values
            float[] values = new float[valueCount];

            for (int i = 0; i < valueCount; i++)
            {
                if (animation.FloatPrecision == ValueType.Float16)
                {
                    values[i] = Half.ToHalf(rawBytes, valueOffset + (i * 2));
                }
                else if (animation.FloatPrecision == ValueType.Float32 || animation.FloatPrecision == ValueType.Vector4)
                {
                    values[i] = BitConverter.ToSingle(rawBytes, valueOffset + (i * 4));
                }
                else
                {
                    throw new InvalidDataException(string.Format("EMA_Animation: Unknown float type ({0}).", animation.FloatPrecision));
                }

                //Console.WriteLine(string.Format("{1}: {0}", values[i], i));
            }
            //Console.ReadLine();

            //Commands
            for (int i = 0; i < commandCount; i++)
            {
                int commandOffset = BitConverter.ToInt32(rawBytes, offset + 20 + (i * 4));

                if (commandOffset != 0)
                {
                    string boneName = emaFile.HasSkeleton ? emaFile.GetBoneName(BitConverter.ToUInt16(rawBytes, commandOffset + offset)) : null;

                    animation.AddCommand(boneName, EMA_Command.Read(rawBytes, commandOffset + offset, values, animation.EmaType));
                }

            }

            return animation;
        }

        public List<float> GetValues()
        {
            //Merges all unique keyframe values into an array and assigns the index value
            List<float> floats = new List<float>();

            List<int> dualIndex = new List<int>();

            foreach (var node in Nodes)
            {
                foreach (var command in node.Commands)
                {
                    foreach (var keyframe in command.Keyframes)
                    {
                        //Sloppy code for now, refactor it later...
                        if (keyframe.InterpolationType == KeyframeInterpolation.QuadraticBezier)
                        {
                            //Always add new dual values. Don't reuse, and dont let them be used by other keyframes.
                            floats.Add(keyframe.Value);
                            keyframe.index = floats.Count - 1;
                            floats.Add(keyframe.ControlPoint1);
                            dualIndex.Add(keyframe.index);
                            dualIndex.Add(keyframe.index + 1);
                        }
                        else if (keyframe.InterpolationType == KeyframeInterpolation.CubicBezier)
                        {
                            //Always add new dual values. Don't reuse, and dont let them be used by other keyframes.
                            floats.Add(keyframe.Value);
                            keyframe.index = floats.Count - 1;
                            floats.Add(keyframe.ControlPoint1);
                            floats.Add(keyframe.ControlPoint2);
                            dualIndex.Add(keyframe.index);
                            dualIndex.Add(keyframe.index + 1);
                            dualIndex.Add(keyframe.index + 2);
                        }
                        else
                        {
                            //Value is not dual, so reuse any NON-DUAL value or add a new value
                            int idx = -1;
                            for (int i = 0; i < floats.Count; i++)
                            {
                                if (floats[i] == keyframe.Value && !dualIndex.Contains(i))
                                {
                                    //Reuse this index
                                    idx = i;
                                    break;
                                }
                            }

                            if (idx != -1)
                            {
                                keyframe.index = idx;
                            }
                            else
                            {
                                //Value not found. Add it.
                                floats.Add(keyframe.Value);
                                keyframe.index = floats.Count - 1;
                            }

                        }
                    }
                }

            }

            return floats;
        }

        #endregion

        #region KeyframeManipulation

        public List<IUndoRedo> AddKeyframes(List<SerializedBone> bones, bool removeCollisions, bool rebase, bool addPos, bool addRot, bool addScale)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //Remove collisions 
            if (removeCollisions && !rebase)
            {
                int min = SerializedBone.GetMinKeyframe(bones);
                int max = SerializedBone.GetMaxKeyframe(bones);

                if (min != max)
                {
                    foreach (var bone in bones)
                    {
                        EMA_Node node = GetNode(bone.Name);

                        if (node != null)
                            node.RemoveCollisions(min, max, undos);
                    }
                }
            }

            if (rebase)
            {
                int min = SerializedBone.GetMinKeyframe(bones);
                int max = SerializedBone.GetMaxKeyframe(bones);
                int rebaseAmount = max - min + 1;

                foreach (var bone in bones)
                {
                    EMA_Node node = GetNode(bone.Name);

                    if (node != null)
                        node.RebaseKeyframes(min, rebaseAmount, removeCollisions, addPos, addRot, addScale, undos);
                }
            }

            //Paste keyframes
            foreach (var bone in bones)
            {
                EMA_Node node = GetNode(bone.Name, true, undos);

                if (node == null) throw new InvalidDataException($"EMA_Animation.AddKeyframes: \"{bone.Name}\" not found.");

                bool hasPos = addPos ? bone.HasPos : false;
                bool hasRot = addRot ? bone.HasRot : false;
                bool hasScale = addScale ? bone.HasScale : false;

                foreach (var keyframe in bone.Keyframes)
                {
                    node.AddKeyframe(keyframe.Frame, keyframe.PosX, keyframe.PosY, keyframe.PosZ, keyframe.PosW,
                                     keyframe.RotX, keyframe.RotY, keyframe.RotZ, keyframe.RotW,
                                     keyframe.ScaleX, keyframe.ScaleY, keyframe.ScaleZ, keyframe.ScaleW, undos, hasPos, hasRot, hasScale);
                }
            }

            return undos;
        }

        public List<IUndoRedo> SyncMatCommands(byte parameter, EMM.EMM_File emmFile)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //MatCol = 4 (x,y,z,w), TexScrl = 2 (u,v)
            //int numComponents = parameter > 3 ? 2 : 4;
            int numComponents = 3; //Hardcode to 3 components for RGB.
            int endFrame = GetEndFrame();

            foreach (EMA_Node node in Nodes)
            {
                EMA_Command[] components = new EMA_Command[numComponents];

                //Grab the material and get the default values
                EMM.EmmMaterial material = emmFile.GetMaterial(node.BoneName);
                if (material == null) continue;

                //float[] defaultValues = parameter > 3 ? material.DecompiledParameters.GetTexScrl(parameter - 4).Values : material.DecompiledParameters.GetMatCol(parameter).Values;
                float[] defaultValues = material.DecompiledParameters.GetMatCol(parameter).Values;

                //If ALL of the components dont exist, then none need to be added. SO skip.
                if (components.All(x => x == null))
                    continue;

                for (byte i = 0; i < numComponents; i++)
                {
                    components[i] = node.GetCommand(parameter, i);

                    //Create the component with the default values from EMM
                    if (components[i] == null)
                    {
                        components[i] = EMA_Command.GetNew(parameter, i, EmaAnimationType.mat);
                        components[i].Keyframes.Add(new EMA_Keyframe(0, defaultValues[i]));
                        components[i].Keyframes.Add(new EMA_Keyframe(endFrame, defaultValues[i]));
                        AddCommand(node.BoneName, components[i], undos);
                    }
                }


                //Now sync the commands
                foreach (EMA_Command anim in node.Commands)
                {
                    foreach (EMA_Command anim2 in node.Commands)
                    {
                        if (anim.Parameter == parameter && anim2.Parameter == parameter)
                        {
                            anim.AddKeyframesFromCommand(anim2);
                        }
                    }
                }
            }

            return undos;
        }

        /// <summary>
        /// Ensures that color components (R, G, B) are always in sync (e.x: R must exist at same frame as G, and so on)
        /// </summary>
        public List<IUndoRedo> SyncColorCommands()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (EmaType != EmaAnimationType.light) throw new InvalidOperationException("EMA_Animation.SyncColorCommands: Method not valid for type = " + EmaType);

            EMA_Command r_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_R);
            EMA_Command g_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_G);
            EMA_Command b_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_B);

            int endFrame = GetEndFrame();

            //There is atleast one color component on this animation
            if (r_command != null || g_command != null || b_command != null)
            {
                //Now we need to add the components that dont exist
                if (r_command == null)
                {
                    EMA_Command newCommand = EMA_Command.GetNewLight();
                    newCommand.Component = EMA_Command.COMPONENT_R;
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = 0, Value = 0 }); //First keyframe
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = (ushort)endFrame, Value = 0 }); //Last keyframe
                    AddCommand(null, newCommand, undos);
                }

                if (g_command == null)
                {
                    EMA_Command newCommand = EMA_Command.GetNewLight();
                    newCommand.Component = EMA_Command.COMPONENT_G;
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = 0, Value = 0 }); //First keyframe
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = (ushort)endFrame, Value = 0 }); //Last keyframe
                    AddCommand(null, newCommand, undos);
                }

                if (b_command == null)
                {
                    EMA_Command newCommand = EMA_Command.GetNewLight();
                    newCommand.Component = EMA_Command.COMPONENT_B;
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = 0, Value = 0 }); //First keyframe
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = (ushort)endFrame, Value = 0 }); //Last keyframe
                    AddCommand(null, newCommand, undos);
                }

            }

            //Reload the commands now that they are all added.
            r_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_R);
            g_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_G);
            b_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_B);

            //Now sync the commands
            foreach (EMA_Command anim in Nodes[0].Commands)
            {
                foreach (EMA_Command anim2 in Nodes[0].Commands)
                {
                    if (anim.Parameter == 2 && anim2.Parameter == 2 && anim.Component != 3 && anim2.Component != 3)
                    {
                        anim.AddKeyframesFromCommand(anim2);
                    }
                }
            }

            return undos;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet = false, int variance = 0, EMM.EMM_File emmFile = null)
        {
            if (EmaType == EmaAnimationType.light)
            {
                EMA_Command r_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_R);
                EMA_Command g_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_G);
                EMA_Command b_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_B);

                ChangeHue(hue, saturation, lightness, undos, hueSet, variance, r_command, g_command, b_command);
            }
            else if (EmaType == EmaAnimationType.mat && emmFile != null)
            {
                undos.AddRange(SyncMatCommands(0, emmFile));
                undos.AddRange(SyncMatCommands(1, emmFile));
                undos.AddRange(SyncMatCommands(2, emmFile));
                undos.AddRange(SyncMatCommands(3, emmFile));

                foreach (EMA_Node node in Nodes)
                {
                    for (byte i = 0; i < 4; i++)
                    {
                        EMA_Command r_command = GetCommand(i, EMA_Command.COMPONENT_R, node.BoneName);
                        EMA_Command g_command = GetCommand(i, EMA_Command.COMPONENT_G, node.BoneName);
                        EMA_Command b_command = GetCommand(i, EMA_Command.COMPONENT_B, node.BoneName);

                        ChangeHue(hue, saturation, lightness, undos, hueSet, variance, r_command, g_command, b_command);
                    }
                }
            }
        }

        private void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet, int variance, params EMA_Command[] commands)
        {
            if (commands.All(x => x == null)) return;
            //EMA_Command r_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_R);
            //EMA_Command g_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_G);
            //EMA_Command b_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_B);

            foreach (EMA_Keyframe r in commands[0].Keyframes)
            {
                float g = commands[1].GetKeyframeValue(r.Time);
                float b = commands[2].GetKeyframeValue(r.Time);

                HslColor.HslColor hslColor = new RgbColor(r.Value, g, b).ToHsl();
                RgbColor convertedColor;

                if (hueSet)
                {
                    hslColor.SetHue(hue, variance);
                }
                else
                {
                    hslColor.ChangeHue(hue);
                    hslColor.ChangeSaturation(saturation);
                    hslColor.ChangeLightness(lightness);
                }

                convertedColor = hslColor.ToRgb();

                commands[0].SetValue(r.Time, (float)convertedColor.R, undos);
                commands[1].SetValue(r.Time, (float)convertedColor.G, undos);
                commands[2].SetValue(r.Time, (float)convertedColor.B, undos);
            }
        }

        #endregion

        public EMA_Node GetNode(string BoneName, bool createNode, List<IUndoRedo> undos = null)
        {
            EMA_Node node = GetNode(BoneName);

            if (node == null && createNode)
            {
                node = CreateNode(BoneName, undos);
            }

            return node;
        }

        public EMA_Node GetNode(string boneName)
        {
            return Nodes.FirstOrDefault(x => x.BoneName == boneName);
        }

        public EMA_Node CreateNode(string bone, List<IUndoRedo> undos = null)
        {
            if (undos == null) undos = new List<IUndoRedo>();

            EMA_Node existing = GetNode(bone);
            if (existing != null) return existing;

            EMA_Node node = new EMA_Node();
            node.BoneName = bone;

            Nodes.Add(node);
            undos.Add(new UndoableListAdd<EMA_Node>(Nodes, node));

            return node;
        }

        public EMA_Command GetCommand(int parameter, int component, string nodeName = null)
        {
            if (Nodes == null) Nodes = new AsyncObservableCollection<EMA_Node>();

            foreach (EMA_Node node in Nodes.Where(x => x.BoneName == nodeName))
            {
                foreach (EMA_Command command in node.Commands)
                {
                    if (command.Parameter == parameter && command.Component == component) return command;
                }
            }

            return null;
        }

        public List<RgbColor> GetUsedColors()
        {
            SyncColorCommands();

            List<RgbColor> colors = new List<RgbColor>();

            EMA_Command r_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_R);
            EMA_Command g_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_G);
            EMA_Command b_command = GetCommand(EMA_Command.PARAMETER_COLOR, EMA_Command.COMPONENT_B);

            foreach (var r in r_command.Keyframes)
            {
                float g = g_command.GetKeyframeValue(r.Time);
                float b = b_command.GetKeyframeValue(r.Time);

                colors.Add(new RgbColor(r.Value, g, b));
            }

            return colors;
        }

        public void AddCommand(string bone, EMA_Command command, List<IUndoRedo> undos = null)
        {
            EMA_Node node = Nodes.FirstOrDefault(x => x.BoneName == bone);

            if (node == null)
            {
                node = new EMA_Node();
                node.BoneName = bone;
                Nodes.Add(node);

                undos?.Add(new UndoableListAdd<EMA_Node>(Nodes, node));
            }

            EMA_Command existingCommand = node.Commands.FirstOrDefault(x => x.Parameter == command.Parameter && x.Component == command.Component);

            if (existingCommand != null)
            {
                int idx = node.Commands.IndexOf(existingCommand);
                undos?.Add(new UndoableListInsert<EMA_Command>(node.Commands, idx, command));
                node.Commands[idx] = command;
            }
            else
            {
                node.Commands.Add(command);
                undos?.Add(new UndoableListAdd<EMA_Command>(node.Commands, command));
            }
        }

        public int GetEndFrame()
        {
            int max = 0;

            foreach(var node in Nodes)
            {
                foreach(var command in node.Commands)
                {
                    if (command.Keyframes.Count == 0) continue;

                    if (command.Keyframes[command.Keyframes.Count - 1].Time > max)
                        max = command.Keyframes[command.Keyframes.Count - 1].Time;
                }
            }

            return max;
        }
    }

    [Serializable]
    [YAXSerializeAs("Node")]
    public class EMA_Node
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("BoneName")]
        [YAXDontSerializeIfNull]
        public string BoneName { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Command")]
        public AsyncObservableCollection<EMA_Command> Commands { get; set; } = new AsyncObservableCollection<EMA_Command>();
        
        public EMA_Command GetCommand(int parameter, int component, bool create, List<IUndoRedo> undos = null)
        {
            EMA_Command command = GetCommand(parameter, component);

            if(command == null && create)
            {
                command = new EMA_Command();
                command.Parameter = (byte)parameter;
                command.Component = (byte)component;

                if(undos != null)
                    undos.Add(new UndoableListAdd<EMA_Command>(Commands, command));

                Commands.Add(command);
            }

            return command;
        }

        public EMA_Command GetCommand(int parameter, int component)
        {
            return Commands.FirstOrDefault(x => x.Parameter == parameter && x.Component == component);
        }

        public float GetKeyframeValueOrDefault(int parameter, int component, int frame, float defaultValue)
        {
            EMA_Command command = GetCommand(parameter, component);
            return command?.GetKeyframeValue(frame) ?? defaultValue;
        }

        public List<ushort> GetAllKeyframes(bool usePos = true, bool useRot = true, bool useScale = true)
        {
            List<ushort> keyframes = new List<ushort>();

            foreach(var command in Commands)
            {
                if (command.Parameter == EMA_Command.PARAMETER_POSITION && !usePos) continue;
                if (command.Parameter == EMA_Command.PARAMETER_ROTATION && !useRot) continue;
                if (command.Parameter == EMA_Command.PARAMETER_SCALE && !useScale) continue;

                foreach(var keyframe in command.Keyframes)
                {
                    if (!keyframes.Contains(keyframe.Time))
                        keyframes.Add(keyframe.Time);
                }
            }

            keyframes.Sort();

            return keyframes;
        }
    
        public bool HasParameter(int parameter)
        {
            return Commands.Any(x => x.Parameter == parameter);
        }

        public float[] GetKeyframeValues(int frame, bool usePos = true, bool useRot = true, bool useScale = true)
        {
            float[] values = new float[12];

            //Set default values
            values[3] = 1f; // Pos W
            values[7] = 1f; //Rot W
            values[8] = 1f; //Scale X
            values[9] = 1f; //Scale Y
            values[10] = 1f; //Scale Z
            values[11] = 1f; //Scale W

            if (usePos)
            {
                values[0] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_X, frame, 0f);
                values[1] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_Y, frame, 0f);
                values[2] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_Z, frame, 0f);
                values[3] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_W, frame, 1f);
            }
            if (useRot)
            {
                values[4] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_X, frame, 0f);
                values[5] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_Y, frame, 0f);
                values[6] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_Z, frame, 0f);
                values[7] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_W, frame, 1f);
            }
            if (useScale)
            {
                values[8] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_X, frame, 1f);
                values[9] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_Y, frame, 1f);
                values[10] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_Z, frame, 1f);
                values[11] = GetKeyframeValueOrDefault(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_W, frame, 1f);
            }

            return values;
        }
        
        public List<int> GetAllKeyframesInt(bool usePos = true, bool useRot = true, bool useScale = true)
        {
            return ArrayConvert.ConvertToIntList(GetAllKeyframes(usePos, useRot, useScale));
        }

        private bool IsValidComponent(EMA_Command command, bool pos, bool rot, bool scale)
        {
            if (command.Parameter == EMA_Command.PARAMETER_POSITION && !pos) return false;
            if (command.Parameter == EMA_Command.PARAMETER_ROTATION && !rot) return false;
            if (command.Parameter == EMA_Command.PARAMETER_SCALE && !scale) return false;

            return true;
        }

        #region KeyframeManipulation
        /// <summary>
        /// Add a keyframe at the specified frame (will overwrite any existing keyframe).
        /// </summary>
        public void AddKeyframe(int frame, float posX, float posY, float posZ, float posW, float rotX, float rotY, float rotZ, float rotW,
            float scaleX, float scaleY, float scaleZ, float scaleW, List<IUndoRedo> undos, bool hasPosition = true, bool hasRotation = true, bool hasScale = true)
        {
            //Only add the keyframe if the values aren't default.
            if (hasPosition)
            {
                AddKeyframe(EMA_Command.PARAMETER_POSITION, frame, posX, posY, posZ, posW, undos);
            }

            if (hasRotation)
            {
                AddKeyframe(EMA_Command.PARAMETER_ROTATION, frame, rotX, rotY, rotZ, rotW, undos);
            }

            if (hasScale)
            {
                AddKeyframe(EMA_Command.PARAMETER_SCALE, frame, scaleX, scaleY, scaleZ, scaleW, undos);
            }
        }

        public void AddKeyframe(int parameter, int time, float x, float y, float z, float w, List<IUndoRedo> undos = null)
        {
            EMA_Command xCommand = GetCommand(parameter, 0, true, undos);
            EMA_Command yCommand = GetCommand(parameter, 1, true, undos);
            EMA_Command zCommand = GetCommand(parameter, 2, true, undos);
            EMA_Command wCommand = GetCommand(parameter, 3, true, undos);

            xCommand.AddKeyframe(time, x, undos: undos);
            yCommand.AddKeyframe(time, y, undos: undos);
            zCommand.AddKeyframe(time, z, undos: undos);
            wCommand.AddKeyframe(time, w, undos: undos);
        }

        public void AddKeyframe(int parameter, int component, int time, float value, float cp1, float cp2, KeyframeInterpolation interpolation, List<IUndoRedo> undos = null)
        {
            EMA_Command command = GetCommand(parameter, component, true, undos);
            command.AddKeyframe(time, value, cp1, cp2, interpolation, undos);
        }

        public void RemoveCollisions(int startFrame, int endFrame, List<IUndoRedo> undos = null)
        {
            foreach (var command in Commands)
                command.RemoveCollisions(startFrame, endFrame, undos);
        }

        public void RebaseKeyframes(int startFrame, int rebaseAmount, bool removeCollisions, bool pos = true, bool rot = true, bool scale = true, List<IUndoRedo> undos = null)
        {
            if (rebaseAmount == 0) return;

            var frames = GetAllKeyframesInt();

            int endFrame = frames.Max();
            int min = startFrame + rebaseAmount;

            //The calculation differs if rebase is negative or positive
            int max = (rebaseAmount > 0) ? min + (endFrame - startFrame) : min + Math.Abs(rebaseAmount);

            foreach (var command in Commands)
            {
                if (!IsValidComponent(command, pos, rot, scale)) continue;

                if (removeCollisions)
                    command.RemoveCollisions(min, max, undos);

                foreach (var keyframe in command.Keyframes.Where(x => x.Time >= startFrame))
                {
                    ushort frameIndex = (ushort)(keyframe.Time + rebaseAmount);

                    if (undos != null)
                        undos.Add(new UndoableProperty<EMA_Keyframe>(nameof(keyframe.Time), keyframe, keyframe.Time, frameIndex));

                    keyframe.Time = frameIndex;
                }
            }
        }
        #endregion
    }

    [Serializable]
    [YAXSerializeAs("Command")]
    public class EMA_Command
    {
        internal const int PARAMETER_POSITION = 0;
        internal const int PARAMETER_ROTATION = 1;
        internal const int PARAMETER_SCALE = 2;
        internal const byte PARAMETER_COLOR = 2;
        internal const int COMPONENT_X = 0;
        internal const int COMPONENT_Y = 1;
        internal const int COMPONENT_Z = 2;
        internal const int COMPONENT_W = 3;
        internal const byte COMPONENT_R = 0;
        internal const byte COMPONENT_G = 1;
        internal const byte COMPONENT_B = 2;

        private EmaAnimationType emaType = EmaAnimationType.obj;

        [YAXDontSerialize]
        public int KeyframeCount
        {
            get
            {
                if (Keyframes == null) return 0;
                return Keyframes.Count;
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Parameter")]
        public string ParameterXmlBinding
        {
            get => GetParameterString();
            set => Parameter = GetParameterInt(value);
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("Component")]
        public string ComponentXmlBinding
        {
            get => GetComponentString();
            set => Component = GetComponentInt(value);
        }

        [YAXDontSerialize]
        public byte Parameter { get; set; }

        [YAXDontSerialize]
        public byte Component { get; set; }
        [YAXDontSerialize]
        public bool Int16ForTime = false; //Calculated on save
        [YAXDontSerialize]
        public bool Int16ForValueIndex = false; //Calculated on save
        [YAXAttributeFor("Flags")]
        [YAXSerializeAs("Unk1")]
        public bool I_03_b1 { get; set; }
        [YAXAttributeFor("Flags")]
        [YAXSerializeAs("Unk4")]
        public bool I_03_b4 { get; set; }
        [YAXAttributeFor("Flags")]
        [YAXSerializeAs("Unk5")]
        public bool I_03_a4 { get; set; }
        [YAXAttributeFor("Flags")]
        public bool NoInterpolation { get; set; }

        public AsyncObservableCollection<EMA_Keyframe> Keyframes { get; set; } = new AsyncObservableCollection<EMA_Keyframe>();


        public static EMA_Command GetNewLight()
        {
            return new EMA_Command()
            {
                emaType = EmaAnimationType.light,
                Parameter = 2, //Color
            };
        }

        public static EMA_Command GetNew(byte parameter, byte component, EmaAnimationType type)
        {
            return new EMA_Command()
            {
                emaType = type,
                Parameter = parameter,
                Component = component
            };
        }

        public static EMA_Command Read(byte[] rawBytes, int offset, float[] values, EmaAnimationType _emaType)
        {
            EMA_Command command = new EMA_Command();
            command.emaType = _emaType;

            command.Parameter = rawBytes[offset + 2];

            BitArray flags_b = new BitArray(new byte[1] { Int4Converter.ToInt4(rawBytes[offset + 3])[1] });
            BitArray flags_a = new BitArray(new byte[1] { Int4Converter.ToInt4(rawBytes[offset + 3])[0] });
            command.I_03_b1 = flags_b[0];
            command.Int16ForTime = flags_b[1];
            command.Int16ForValueIndex = flags_b[2];
            command.I_03_b4 = flags_b[3];
            command.I_03_a4 = flags_a[3];
            command.NoInterpolation = flags_a[2];
            flags_a[2] = false;
            flags_a[3] = false;
            command.Component = Int4Converter.GetByte(Utils.ConvertToByte(flags_a), 0);

            ushort keyframeCount = BitConverter.ToUInt16(rawBytes, offset + 4);
            ushort indexOffset = BitConverter.ToUInt16(rawBytes, offset + 6);

            for (int i = 0; i < keyframeCount; i++)
            {
                ushort time;
                float value;
                float controlPoint1 = 0f;
                float controlPoint2 = 0f;
                KeyframeInterpolation interpolation;

                if (command.Int16ForTime)
                {
                    time = BitConverter.ToUInt16(rawBytes, offset + 8 + (i * 2));
                }
                else
                {
                    time = rawBytes[offset + 8 + i];
                }

                if (command.Int16ForValueIndex)
                {
                    value = values[BitConverter.ToUInt16(rawBytes, offset + indexOffset + (i * 4))];
                    interpolation = (KeyframeInterpolation)rawBytes[offset + indexOffset + 3 + (i * 4)];
                    int extraOffset = 0;

                    if (interpolation == KeyframeInterpolation.QuadraticBezier)
                    {
                        ushort idx = (ushort)(BitConverter.ToUInt16(rawBytes, offset + indexOffset + (i * 4)) + 1);

                        if (idx <= values.Length - 1)
                            controlPoint1 = values[idx];

                        extraOffset++;
                    }
                    else if (interpolation == KeyframeInterpolation.CubicBezier)
                    {
                        ushort idx = (ushort)(BitConverter.ToUInt16(rawBytes, offset + indexOffset + (i * 4)) + 1 + extraOffset);

                        if (idx + 1 <= values.Length - 1)
                        {
                            controlPoint1 = values[idx];
                            controlPoint2 = values[idx + 1];
                        }

                        extraOffset++;
                    }

                }
                else
                {
                    int valueIdx = BitConverter.ToUInt16(rawBytes, offset + indexOffset + (i * 2)) & 0x3fff;

                    //The index could be bad for older serialized EMAs since the value index was handled incorrectly in old versions
                    if (values.Length - 1 < valueIdx)
                        valueIdx = 0;

                    value = values[valueIdx];
                    interpolation = (KeyframeInterpolation)(rawBytes[offset + indexOffset + 1 + (i * 2)] & 0xc0);
                    int extraOffset = 0;

                    if (interpolation == KeyframeInterpolation.QuadraticBezier)
                    {
                        byte idx = (byte)(rawBytes[offset + indexOffset + (i * 2)] + 1);

                        if (idx <= values.Length - 1)
                            controlPoint1 = values[idx];
                    }
                    else if (interpolation == KeyframeInterpolation.CubicBezier)
                    {
                        byte idx = (byte)(rawBytes[offset + indexOffset + (i * 2)] + 1 + extraOffset);

                        if (idx + 1 <= values.Length - 1)
                        {
                            controlPoint1 = values[idx];
                            controlPoint2 = values[idx + 1];
                        }
                    }
                }

                command.Keyframes.Add(new EMA_Keyframe()
                {
                    Time = time,
                    Value = value,
                    InterpolationType = interpolation,
                    ControlPoint1 = controlPoint1,
                    ControlPoint2 = controlPoint2
                });
            }

            return command;
        }

        private string GetParameterString()
        {
            if (emaType == EmaAnimationType.obj)
            {
                switch (Parameter)
                {
                    case 0:
                        return "Position";
                    case 1:
                        return "Rotation";
                    case 2:
                        return "Scale";
                }
            }
            else if (emaType == EmaAnimationType.light)
            {
                switch (Parameter)
                {
                    case 2:
                        return "Color";
                    case 3:
                        return "Light";
                }
            }
            else if (emaType == EmaAnimationType.mat)
            {
                switch (Parameter)
                {
                    case 0:
                        return "MatCol0";
                    case 1:
                        return "MatCol1";
                    case 2:
                        return "MatCol2";
                    case 3:
                        return "MatCol3";
                    case 4:
                        return "TexScrl0";
                    case 5:
                        return "TexScrl1";
                    case 6:
                        return "TexScrl2";
                    case 7:
                        return "TexScrl3";
                }
            }

            return Parameter.ToString();
        }

        private byte GetParameterInt(string parameter)
        {
            parameter = parameter.ToLower();

            switch (parameter)
            {
                case "position":
                case "matcol0":
                    return 0;
                case "rotation":
                case "matcol1":
                    return 1;
                case "scale":
                case "color":
                case "matcol2":
                    return 2;
                case "light":
                case "matcol3":
                    return 3;
                case "texscrl0":
                    return 4;
                case "texscrl1":
                    return 5;
                case "texscrl2":
                    return 6;
                case "texscrl3":
                    return 7;
                default:
                    try
                    {
                        return byte.Parse(parameter);
                    }
                    catch
                    {
                        throw new Exception(String.Format("\"{0}\" is not a valid Parameter.", parameter));
                    }
            }
        }

        private string GetComponentString()
        {
            if (emaType != EmaAnimationType.light)
            {
                switch (Component)
                {
                    case 0:
                        return "X";
                    case 1:
                        return "Y";
                    case 2:
                        return "Z";
                    case 3:
                        return "W";
                    default:
                        return Component.ToString();
                }
            }
            else
            {
                if (Parameter == 3)
                {
                    switch (Component)
                    {
                        case 0:
                            return "InnerRadius";
                        case 1:
                            return "OuterRadius";
                        default:
                            return Component.ToString();
                    }
                }
                else
                {
                    switch (Component)
                    {
                        case 0:
                            return (Parameter == 2) ? "R" : Component.ToString();
                        case 1:
                            return (Parameter == 2) ? "G" : Component.ToString();
                        case 2:
                            return (Parameter == 2) ? "B" : Component.ToString();
                        case 3:
                            return (Parameter == 2) ? "A" : Component.ToString();
                        default:
                            return Component.ToString();
                    }
                }
            }
        }

        private byte GetComponentInt(string component)
        {
            switch (component.ToLower())
            {
                case "x":
                case "r":
                case "innerradius":
                    return 0;
                case "g":
                case "y":
                case "outerradius":
                    return 1;
                case "b":
                case "z":
                    return 2;
                case "a":
                case "w":
                    return 3;
                default:
                    try
                    {
                        return byte.Parse(component);
                    }
                    catch
                    {
                        throw new Exception(String.Format("\"{0}\" is not a valid Component.", component));
                    }
            }
        }

        public void AddKeyframesFromCommand(EMA_Command anim)
        {
            if (anim == this) return;

            foreach (EMA_Keyframe keyframe in anim.Keyframes)
            {
                EMA_Keyframe existing = GetKeyframe(keyframe.Time);

                if (existing == null)
                {
                    EMA_Keyframe newKeyframe = new EMA_Keyframe() { Time = keyframe.Time, Value = GetKeyframeValue(keyframe.Time) };
                    Keyframes.Add(newKeyframe);
                }
            }
        }

        public void SetValue(int time, float value, List<IUndoRedo> undos = null)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Time == time)
                {
                    float oldValue = keyframe.Value;
                    keyframe.Value = value;

                    if (undos != null)
                        undos.Add(new UndoableProperty<EMA_Keyframe>(nameof(keyframe.Value), keyframe, oldValue, keyframe.Value));

                    return;
                }
            }

            //Keyframe doesn't exist. Add it.
            Keyframes.Add(new EMA_Keyframe() { Time = (ushort)time, Value = value });
        }

        #region EanConversion
        /*
        public static EMA_Command ConvertToEma(EAN_AnimationComponent eanComponent, Axis axis, string boneName)
        {
            EMA_Command command = new EMA_Command();
            command.BoneName = boneName;
            command.ComponentXmlBinding = axis.ToString();
            command.ParameterXmlBinding = eanComponent.Type.ToString();

            foreach (var keyframe in eanComponent.Keyframes)
            {
                var emaKeyframe = new EMA_Keyframe();
                emaKeyframe.Time = keyframe.FrameIndex;

                switch (axis)
                {
                    case Axis.X:
                        emaKeyframe.Value = keyframe.X * keyframe.W;
                        break;
                    case Axis.Y:
                        emaKeyframe.Value = keyframe.Y * keyframe.W;
                        break;
                    case Axis.Z:
                        emaKeyframe.Value = keyframe.Z * keyframe.W;
                        break;
                }

                command.Keyframes.Add(emaKeyframe);
            }

            return command;
        }

        public static EMA_Command[] ConvertToEma_Rotation(EAN_AnimationComponent eanComponent, string boneName)
        {
            EMA_Command command1 = new EMA_Command();
            command1.BoneName = boneName;
            command1.ComponentXmlBinding = "X";
            command1.ParameterXmlBinding = eanComponent.Type.ToString();

            EMA_Command command2 = new EMA_Command();
            command2.BoneName = boneName;
            command2.ComponentXmlBinding = "Y";
            command2.ParameterXmlBinding = eanComponent.Type.ToString();

            EMA_Command command3 = new EMA_Command();
            command3.BoneName = boneName;
            command3.ComponentXmlBinding = "Z";
            command3.ParameterXmlBinding = eanComponent.Type.ToString();

            foreach (var keyframe in eanComponent.Keyframes)
            {
                Quaternion rot = new Quaternion(keyframe.X, keyframe.Y, keyframe.Z, keyframe.W);
                Vector3 angles = MathHelpers.QuaternionToEulerAngles_OLD(rot);

                command1.Keyframes.Add(new EMA_Keyframe(keyframe.FrameIndex, angles.X));
                command2.Keyframes.Add(new EMA_Keyframe(keyframe.FrameIndex, angles.Y));
                command3.Keyframes.Add(new EMA_Keyframe(keyframe.FrameIndex, angles.Z));

            }

            return new EMA_Command[] { command1, command2, command3 };
        }
        */
        #endregion

        #region Get
        public EMA_Keyframe GetKeyframe(int time)
        {
            for (int i = Math.Max(CurrentKeyframeIndex, 0); i < Keyframes.Count; i++)
            {
                if (Keyframes[i].Time == time)
                {
                    CurrentKeyframeIndex = i;
                    return Keyframes[i];
                }
            }

            if (CurrentKeyframeIndex != 0)
            {
                CurrentKeyframeIndex = 0;
                return GetKeyframe(time);
            }

            return null;
        }

        /// <summary>
        /// Returns the keyframe that appears just before the specified frame
        /// </summary>
        /// <returns></returns>
        public EMA_Keyframe GetKeyframeBefore(int time)
        {
            EMA_Keyframe prev = null;

            foreach (var keyframe in Keyframes.OrderBy(x => x.Time))
            {
                if (keyframe.Time >= time) break;
                prev = keyframe;
            }

            return prev;
        }

        /// <summary>
        /// Returns the keyframe that appears just after the specified frame
        /// </summary>
        /// <returns></returns>
        public EMA_Keyframe GetKeyframeAfter(int time)
        {
            foreach (var keyframe in Keyframes.OrderBy(x => x.Time))
            {
                if (keyframe.Time > time) return keyframe;
            }

            return null;
        }

        #endregion

        #region Interpolation

        private EMA_Keyframe _defaultKeyframe = null;
        [YAXDontSerialize]
        internal EMA_Keyframe DefaultKeyframe
        {
            get
            {
                if (_defaultKeyframe == null)
                    _defaultKeyframe = new EMA_Keyframe();
                return _defaultKeyframe;
            }
        }

        private int CurrentKeyframeIndex = 0;


        /// <summary>
        /// Get an interpolated keyframe value, from the specified floating-point frame. Allows time-scaled animations.
        /// </summary>
        public float GetKeyframeValue(float frame)
        {
            bool isWhole = Math.Floor(frame) == frame;

            if (isWhole)
            {
                return GetKeyframeValue((int)frame);
            }

            int flooredFrame = (int)Math.Floor(frame);

            float beforeValue = GetKeyframeValue(flooredFrame);
            float afterValue = GetKeyframeValue(flooredFrame + 1);
            float factor = (float)(frame - Math.Floor(frame));

            return MathHelpers.Lerp(beforeValue, afterValue, factor);
        }

        /// <summary>
        /// Get an interpolated keyframe value.
        /// </summary>
        public float GetKeyframeValue(int time)
        {
            EMA_Keyframe existing = GetKeyframe(time);

            if (existing != null)
                return existing.Value;

            //No keyframe existed. Calculate the value.
            int prevFrame = 0;
            int nextFrame = 0;
            EMA_Keyframe prevKeyframe = GetNearestKeyframeBefore(time, ref prevFrame);
            EMA_Keyframe nextKeyframe = GetNearestKeyframeAfter(time, ref nextFrame);

            if ((prevKeyframe != null && nextKeyframe == null) || (prevKeyframe == nextKeyframe && prevKeyframe != null))
            {
                return prevKeyframe.Value;
            }

            float factor = (float)(time - prevFrame) / (float)(nextFrame - prevFrame);

            if (prevKeyframe.InterpolationType == KeyframeInterpolation.CubicBezier)
            {
                return MathHelpers.CubicBezier(factor, prevKeyframe.Value, prevKeyframe.ControlPoint1 + prevKeyframe.Value, prevKeyframe.ControlPoint2 + prevKeyframe.Value, nextKeyframe.Value);
            }
            else if(prevKeyframe.InterpolationType == KeyframeInterpolation.QuadraticBezier)
            {
                return MathHelpers.QuadraticBezier(factor, prevKeyframe.Value, prevKeyframe.ControlPoint1 + prevKeyframe.Value, nextKeyframe.Value);
            }
            else
            {
                return MathHelpers.Lerp(prevKeyframe.Value, nextKeyframe.Value, factor);
            }
        }

        /// <summary>
        /// Get a non-persistent instance of the nearest keyframe BEFORE the specified frame. This instance MAY belong to another keyframe entirely, or not exist in Keyframes at all (if its a default keyframe generated because no keyframes currently exist) - so this method is ONLY intended for reading purposes! 
        /// </summary>
        /// <param name="frame">The specified frame.</param>
        /// <param name="nearFrame">The frame the returned <see cref="EAN_Keyframe"/> belongs to (ignore FrameIndex on the keyframe) </param>
        private EMA_Keyframe GetNearestKeyframeBefore(int frame, ref int nearFrame)
        {
            EMA_Keyframe nearest = null;

            int nearIdx = GetClosestKeyframeIndexBefore(frame);

            if (nearIdx != -1)
            {
                nearest = Keyframes[nearIdx];
                nearFrame = nearest.Time;
            }

            //None found, so use default
            if (nearest == null)
            {
                nearest = DefaultKeyframe;
                nearFrame = frame - 1;
            }

            return nearest;
        }

        /// <summary>
        /// Get a non-persistent instance of the nearest keyframe AFTER the specified frame. This instant MAY belong to another keyframe entirely, or not exist in Keyframes at all (if its a default keyframe generated because no keyframes currently exist) - so this method is ONLY intended for reading purposes! 
        /// </summary>
        /// <param name="frame">The specified frame.</param>
        /// <param name="nearFrame">The frame the returned <see cref="EAN_Keyframe"/> belongs to (ignore <see cref="EAN_Keyframe.FrameIndex"/> on the keyframe) </param>
        private EMA_Keyframe GetNearestKeyframeAfter(int frame, ref int nearFrame)
        {
            EMA_Keyframe nearest = null;

            int nearIdx = GetClosestKeyframeIndexAfter(frame);

            if (nearIdx != -1)
            {
                nearest = Keyframes[nearIdx];
                nearFrame = nearest.Time;
            }

            //None found, so use default
            if (nearest == null)
            {
                nearest = DefaultKeyframe;
                nearFrame = frame + 1;
            }

            return nearest;
        }

        private int GetClosestKeyframeIndexBefore(int frame)
        {
            if (CurrentKeyframeIndex < 0) CurrentKeyframeIndex = 0;

            if (Keyframes.Count == 1)
            {
                CurrentKeyframeIndex = 0;
                return CurrentKeyframeIndex;
            }

            for (int i = CurrentKeyframeIndex; i < Keyframes.Count; i++)
            {
                if (Keyframes[i].Time >= frame)
                {
                    CurrentKeyframeIndex = i - 1;

                    if (CurrentKeyframeIndex < 0)
                        CurrentKeyframeIndex = 0;

                    return CurrentKeyframeIndex;
                }
            }

            CurrentKeyframeIndex = Keyframes.Count - 1;
            return Keyframes.Count - 1;
        }

        private int GetClosestKeyframeIndexAfter(int frame)
        {
            if (CurrentKeyframeIndex < 0) CurrentKeyframeIndex = 0;

            if (Keyframes.Count == 1)
            {
                CurrentKeyframeIndex = 0;
                return CurrentKeyframeIndex;
            }

            for (int i = CurrentKeyframeIndex; i < Keyframes.Count; i++)
            {
                if (Keyframes[i].Time >= frame)
                {
                    CurrentKeyframeIndex = i;
                    return CurrentKeyframeIndex;
                }
            }

            CurrentKeyframeIndex = Keyframes.Count - 1;
            return Keyframes.Count - 1;
        }

        #endregion

        #region KeyframeManipulation
        public void AddKeyframe(int frame, float value, float cp1 = 0f, float cp2 = 0f, KeyframeInterpolation interpolation = KeyframeInterpolation.Linear, List<IUndoRedo> undos = null)
        {
            EMA_Keyframe keyframe = Keyframes.FirstOrDefault(x => x.Time == frame);

            if(keyframe != null)
            {
                if (undos != null)
                {
                    undos.Add(new UndoablePropertyGeneric(nameof(keyframe.Value), keyframe, keyframe.Value, value));
                    undos.Add(new UndoablePropertyGeneric(nameof(keyframe.ControlPoint1), keyframe, keyframe.ControlPoint1, cp1));
                    undos.Add(new UndoablePropertyGeneric(nameof(keyframe.ControlPoint2), keyframe, keyframe.ControlPoint2, cp2));
                    undos.Add(new UndoablePropertyGeneric(nameof(keyframe.InterpolationType), keyframe, keyframe.InterpolationType, interpolation));
                }

                keyframe.Value = value;
                keyframe.ControlPoint1 = cp1;
                keyframe.ControlPoint2 = cp2;
                keyframe.InterpolationType = interpolation;
            }
            else
            {
                keyframe = new EMA_Keyframe();
                keyframe.Value = value;
                keyframe.Time = (ushort)frame;
                keyframe.ControlPoint1 = cp1;
                keyframe.ControlPoint2 = cp2;
                keyframe.InterpolationType = interpolation;

                if(undos != null)
                    undos.Add(new UndoableListAdd<EMA_Keyframe>(Keyframes, keyframe));

                Keyframes.Add(keyframe);
            }
        }

        public void RemoveCollisions(int startFrame, int endFrame, List<IUndoRedo> undos = null)
        {
            for (int i = Keyframes.Count - 1; i >= 0; i--)
            {
                if (Keyframes[i].Time >= startFrame && Keyframes[i].Time <= endFrame)
                {
                    if (undos != null)
                        undos.Add(new UndoableListRemove<EMA_Keyframe>(Keyframes, Keyframes[i]));

                    Keyframes.RemoveAt(i);
                }
            }
        }
        #endregion
    }

    [Serializable]
    [YAXSerializeAs("Keyframe")]
    public class EMA_Keyframe
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Time")]
        public ushort Time { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Value")]
        [YAXFormat("0.0########")]
        public float Value { get; set; }

        [YAXDontSerialize]
        public float ControlPoint1 { get; set; }
        [YAXDontSerialize]
        public float ControlPoint2 { get; set; }

        //ControlPoint serialization in XML. These values will only appear if the correct Flags is set.
        [YAXAttributeForClass]
        [YAXSerializeAs("ControlPoint1")]
        [YAXDontSerializeIfNull]
        public string StrCtrlPoint1
        {
            get => InterpolationType == KeyframeInterpolation.CubicBezier || InterpolationType == KeyframeInterpolation.QuadraticBezier ? ControlPoint1.ToString() : null;
            set
            {
                if (value != null)
                {
                    float val;
                    if (float.TryParse(value, out val))
                        ControlPoint1 = val;
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("ControlPoint2")]
        [YAXDontSerializeIfNull]
        public string StrCtrlPoint2
        {
            get => InterpolationType == KeyframeInterpolation.CubicBezier ? ControlPoint2.ToString() : null;
            set
            {
                if (value != null)
                {
                    float val;
                    if (float.TryParse(value, out val))
                        ControlPoint2 = val;
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("Interpolation")]
        public KeyframeInterpolation InterpolationType { get; set; }

        [YAXDontSerialize]
        public int index = -1;
        public EMA_Keyframe() { }

        public EMA_Keyframe(int frame, float value)
        {
            Time = (ushort)frame;
            Value = value;
        }

        public override string ToString()
        {
            return $"Time: {Time}, Value: {Value}, Interpolation: {InterpolationType}";
        }
    }

    public enum KeyframeInterpolation
    {
        Linear,
        QuadraticBezier = 0x40, //0x40 / 0x4000 (depends on value index type)
        CubicBezier = 0x80 //0x80 / 0x8000
    }

}
