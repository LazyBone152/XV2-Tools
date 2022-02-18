using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.EMA
{
    public enum ValueType : ushort
    {
        Float32_2 = 0,
        Float16 = 1,
        Float32 = 2
    }

    public enum EmaType : ushort
    {
        obj = 0,
        cam = 1,
        mat = 3,
        light = 0x102
    }

    [Flags]
    public enum KeyframeFlags : ushort
    {
        Flag0 = 0x1,
        Flag1 = 0x2,
        Flag2 = 0x4,
        Flag3 = 0x8,
        Flag4 = 0x10,
        Flag5 = 0x20,
        QuadraticBezier = 0x40, //Extra float value
        CubicBezier = 0x80, //Extra float value
        Flag8 = 0x100,
        Flag9 = 0x200,
        Flag10 = 0x400,
        Flag11 = 0x800,
        Flag12 = 0x1000,
        Flag13 = 0x2000,
        Flag14 = 0x4000,
        Flag15 = 0x8000
    }

    [Serializable]
    [YAXSerializeAs("EMA")]
    public class EMA_File
    {
        public const int EMA_SIGNATURE = 1095583011;

        [YAXDontSerialize]
        public bool HasSkeleton
        {
            get { return (skeleton != null); }
        }
        [YAXDontSerialize]
        public EmaType EmaType
        {
            get
            {
                if(Animations.Count > 0)
                {
                    return Animations[0].EmaType;
                }
                return EmaType.obj;
            }
        }

        [YAXAttributeForClass]
        public int Version { get; set; }
        [YAXAttributeForClass]
        public ushort I_18 { get; set; }
        [YAXAttributeForClass]
        public int I_20 { get; set; }
        [YAXAttributeForClass]
        public int I_24 { get; set; }
        [YAXAttributeForClass]
        public int I_28 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Skeleton")]
        public Skeleton skeleton { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Animation")]
        public AsyncObservableCollection<EMA_Animation> Animations { get; set; }

        public static EMA_File Serialize(string path, bool writeXml)
        {
            byte[] bytes = File.ReadAllBytes(path);

            EMA_File emaFile = Load(bytes);

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
            emaFile.I_18 = BitConverter.ToUInt16(rawBytes, 18);
            emaFile.I_20 = BitConverter.ToInt32(rawBytes, 20);
            emaFile.I_24 = BitConverter.ToInt32(rawBytes, 24);
            emaFile.I_28 = BitConverter.ToInt32(rawBytes, 28);

            int skeletonOffset = BitConverter.ToInt32(rawBytes, 12);
            ushort animationCount = BitConverter.ToUInt16(rawBytes, 16);
            int animationsStart = 32;

            //Parse skeleton
            if(skeletonOffset > 0)
            {
                emaFile.skeleton = Skeleton.Parse(rawBytes, skeletonOffset);
            }

            //Parse animations
            emaFile.Animations = AsyncObservableCollection<EMA_Animation>.Create();

            for(int i = 0; i < animationCount; i++)
            {
                int animOffset = BitConverter.ToInt32(rawBytes, animationsStart + (i * 4));

                if (animOffset != 0)
                {
                    emaFile.Animations.Add(EMA_Animation.Read(rawBytes, BitConverter.ToInt32(rawBytes, animationsStart + (i * 4)), i, emaFile));
                }
            }

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

            int animCount = GetIndexedAnimationCount();
            List<int> animNameOffsets = new List<int>();

            //Header
            bytes.AddRange(BitConverter.GetBytes(EMA_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)32));
            bytes.AddRange(BitConverter.GetBytes((int)Version));
            bytes.AddRange(new byte[4]); //Skeleton offset
            bytes.AddRange(BitConverter.GetBytes((ushort)animCount));
            bytes.AddRange(BitConverter.GetBytes(I_18));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_28));

            //Animation pointers
            bytes.AddRange(new byte[4 * animCount]);

            //Animations
            foreach(var anim in Animations)
            {
                int animStartOffset = bytes.Count;
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 32 + (anim.Index * 4));

                List<float> values = anim.GetValues();

                bytes.AddRange(BitConverter.GetBytes(anim.EndFrame));
                bytes.AddRange(BitConverter.GetBytes((ushort)anim.CommandCount));
                bytes.AddRange(BitConverter.GetBytes((int)values.Count));
                bytes.AddRange(BitConverter.GetBytes((ushort)anim.EmaType));
                bytes.AddRange(BitConverter.GetBytes((ushort)anim.FloatPrecision));
                animNameOffsets.Add(bytes.Count);
                bytes.AddRange(new byte[4]); //Name offset
                bytes.AddRange(new byte[4]); //value offset
                bytes.AddRange(new byte[4 * anim.CommandCount]);

                //Commands
                for(int i = 0; i < anim.CommandCount; i++)
                {
                    int startCommandOffset = bytes.Count;
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - animStartOffset), animStartOffset + 20 + (i * 4));
                    
                    anim.Commands[i].SetFlags(); //Calculate the known flags

                    if (HasSkeleton)
                    {
                        bytes.AddRange(BitConverter.GetBytes((ushort)GetBoneIndex(anim.Commands[i].BoneName)));
                    }
                    else
                    {
                        bytes.AddRange(new byte[2]);
                    }

                    bytes.Add(anim.Commands[i].I_02);

                    var bitArray_b = new BitArray(new bool[8] { anim.Commands[i].I_03_b1, anim.Commands[i].I_03_b2_Int16ForTime, anim.Commands[i].I_03_b3_Int16ForValueIndex, anim.Commands[i].I_03_b4, false, false, false, false });
                    var bitArray_a = new BitArray(new byte[1] { anim.Commands[i].I_03_a });

                    bitArray_a[3] = anim.Commands[i].I_03_a4;
                    bytes.Add((byte)Int4Converter.GetByte(Utils.ConvertToByte(bitArray_a), Utils.ConvertToByte(bitArray_b)));
                    bytes.AddRange(BitConverter.GetBytes((ushort)anim.Commands[i].KeyframeCount));
                    bytes.AddRange(new byte[2]);

                    //Sort keyframes
                    if(anim.Commands[i].KeyframeCount > 0)
                    {
                        anim.Commands[i].SortKeyframes();
                    }

                    //Write Time
                    for(int a = 0; a < anim.Commands[i].KeyframeCount; a++)
                    {
                        if(anim.Commands[i].I_03_b2_Int16ForTime)
                        {
                            bytes.AddRange(BitConverter.GetBytes(anim.Commands[i].Keyframes[a].Time));
                        }
                        else
                        {
                            bytes.Add((byte)anim.Commands[i].Keyframes[a].Time);
                        }
                    }

                    //Add padding
                    bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count - startCommandOffset, 4)]);

                    //Write value/index
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((ushort)(bytes.Count - startCommandOffset)), startCommandOffset + 6);
                    for (int a = 0; a < anim.Commands[i].KeyframeCount; a++)
                    {
                        if (anim.Commands[i].I_03_b3_Int16ForValueIndex)
                        {
                            bytes.AddRange(BitConverter.GetBytes((ushort)anim.Commands[i].Keyframes[a].index));
                            bytes.AddRange(BitConverter.GetBytes((ushort)anim.Commands[i].Keyframes[a].Flags));
                        }
                        else
                        {
                            bytes.Add((byte)anim.Commands[i].Keyframes[a].index);
                            bytes.Add((byte)anim.Commands[i].Keyframes[a].Flags);
                        }
                    }

                    //Add padding
                    bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count - startCommandOffset, 4)]);

                }

                //Values
                int valuesStartOffset = bytes.Count;
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - animStartOffset), animStartOffset + 16);
                foreach(var value in values)
                {
                    if(anim.FloatPrecision == ValueType.Float16)
                    {
                        bytes.AddRange(Half.GetBytes((Half)value));
                    }
                    else if (anim.FloatPrecision == ValueType.Float32 || anim.FloatPrecision == ValueType.Float32_2)
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
                bytes.AddRange(skeleton.Write());
            }

            //Strings (animations)
            for(int i = 0; i < Animations.Count; i++)
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

        private int GetIndexedAnimationCount()
        {
            if (Animations == null) return 0;

            int max = 0;

            foreach(var anim in Animations)
            {
                if (anim.Index > max) max = anim.Index;
            }

            return max + 1;
        }

        public string GetBoneName(int boneIndex)
        {
            if (!HasSkeleton) throw new InvalidOperationException("EMA_File.GetBoneName: emaFile has no skeleton.");

            foreach(var bone in skeleton.Bones)
            {
                if (bone.Index == boneIndex) return bone.Name;
            }

            return null;
        }

        public ushort GetBoneIndex(string boneName)
        {
            if (!HasSkeleton) throw new InvalidOperationException("EMA_File.GetBoneIndex: emaFile has no skeleton.");

            foreach (var bone in skeleton.Bones)
            {
                if (bone.Name == boneName) return bone.Index;
            }

            throw new Exception(String.Format("Could not find a bone wih the name: {0}", boneName));
        }

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();
            if (Animations == null) Animations = AsyncObservableCollection<EMA_Animation>.Create();

            foreach(var anim in Animations)
            {
                colors.AddRange(anim.GetUsedColors());
            }

            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {
            if (Animations == null) return;
            if (undos == null) undos = new List<IUndoRedo>();

            foreach (var anim in Animations)
            {
                anim.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
            }
        }

    }

    [Serializable]
    [YAXSerializeAs("Animation")]
    public class EMA_Animation
    {
        [YAXDontSerialize]
        public string ToolName
        {
            get
            {
                return string.Format("[{0}] {1}", Index, Name);
            }
        }

        [YAXDontSerialize]
        public int CommandCount
        {
            get
            {
                if (Commands == null) return 0;
                return Commands.Count;
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public int Index { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("EndFrame")]
        public ushort EndFrame { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public EmaType EmaType { get; set; } //ushort
        [YAXAttributeForClass]
        [YAXSerializeAs("FloatType")]
        public ValueType FloatPrecision { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Command")]
        public AsyncObservableCollection<EMA_Command> Commands { get; set; }

        public static EMA_Animation Read(byte[] rawBytes, int offset, int index, EMA_File emaFile)
        {
            EMA_Animation animation = new EMA_Animation();
            animation.Index = index;
            animation.EndFrame = BitConverter.ToUInt16(rawBytes, offset + 0);
            animation.EmaType = (EmaType)BitConverter.ToUInt16(rawBytes, offset + 8);
            animation.FloatPrecision = (ValueType)BitConverter.ToUInt16(rawBytes, offset + 10);
            animation.Commands = AsyncObservableCollection<EMA_Command>.Create();

            int commandCount = BitConverter.ToUInt16(rawBytes, offset + 2);
            int valueCount = BitConverter.ToInt32(rawBytes, offset + 4);
            int valueOffset = BitConverter.ToInt32(rawBytes, offset + 16) + offset;
            int nameOffset = (BitConverter.ToInt32(rawBytes, offset + 12) != 0) ? BitConverter.ToInt32(rawBytes, offset + 12) + offset : 0;

            //Name
            if(nameOffset > 0)
            {
                animation.Name = StringEx.GetString(rawBytes, nameOffset + 11, false, StringEx.EncodingType.UTF8);
            }

            //Values
            float[] values = new float[valueCount];

            for(int i = 0; i < valueCount; i++)
            {
                if(animation.FloatPrecision == ValueType.Float16)
                {
                    values[i] = Half.ToHalf(rawBytes, valueOffset + (i * 2));
                }
                else if (animation.FloatPrecision == ValueType.Float32 || animation.FloatPrecision == ValueType.Float32_2)
                {
                    values[i] = BitConverter.ToSingle(rawBytes, valueOffset + (i * 4));
                }
                else
                {
                    throw new InvalidDataException(String.Format("EMA_Animation: Unknown float type ({0}).", animation.FloatPrecision));
                }

                //Console.WriteLine(string.Format("{1}: {0}", values[i], i));
            }
            //Console.ReadLine();

            //Commands
            for(int i = 0; i < commandCount; i++)
            {
                int commandOffset = BitConverter.ToInt32(rawBytes, offset + 20 + (i * 4));

                if(commandOffset != 0)
                {
                    animation.Commands.Add(EMA_Command.Read(rawBytes, commandOffset + offset, values, emaFile, animation.EmaType));
                }

            }

            return animation;
        }

        public List<float> GetValues()
        {
            //Merges all unique keyframe values into an array and assigns the index value
            List<float> floats = new List<float>();

            List<int> dualIndex = new List<int>();

            foreach (var command in Commands)
            {
                foreach(var keyframe in command.Keyframes)
                {
                    //Sloppy code for now, refactor it later...
                    if (keyframe.Flags.HasFlag(KeyframeFlags.QuadraticBezier) && keyframe.Flags.HasFlag(KeyframeFlags.CubicBezier))
                    {
                        //Both flags are in use.
                        floats.Add(keyframe.Value);
                        keyframe.index = floats.Count - 1;
                        floats.Add(keyframe.Value2Float);
                        floats.Add(keyframe.Value3Float);
                        floats.Add(keyframe.Value4Float);
                        dualIndex.Add(keyframe.index);
                        dualIndex.Add(keyframe.index + 1);
                        dualIndex.Add(keyframe.index + 2);
                        dualIndex.Add(keyframe.index + 4);
                    }
                    else if (keyframe.Flags.HasFlag(KeyframeFlags.QuadraticBezier))
                    {
                        //Always add new dual values. Don't reuse, and dont let them be used by other keyframes.
                        floats.Add(keyframe.Value);
                        keyframe.index = floats.Count - 1;
                        floats.Add(keyframe.Value2Float);
                        dualIndex.Add(keyframe.index);
                        dualIndex.Add(keyframe.index + 1);
                    }
                    else if (keyframe.Flags.HasFlag(KeyframeFlags.CubicBezier))
                    {
                        //Always add new dual values. Don't reuse, and dont let them be used by other keyframes.
                        floats.Add(keyframe.Value);
                        keyframe.index = floats.Count - 1;
                        floats.Add(keyframe.Value3Float);
                        floats.Add(keyframe.Value4Float);
                        dualIndex.Add(keyframe.index);
                        dualIndex.Add(keyframe.index + 1);
                        dualIndex.Add(keyframe.index + 2);
                    }
                    else
                    {
                        //Value is not dual, so reuse any NON-DUAL value or add a new value
                        int idx = -1;
                        for(int i = 0; i < floats.Count; i++)
                        {
                            if(floats[i] == keyframe.Value && !dualIndex.Contains(i))
                            {
                                //Reuse this index
                                idx = i;
                                break;
                            }
                        }

                        if(idx != -1)
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

            return floats;
        }

        /// <summary>
        /// Ensures that color components (R, G, B) are always in sync (e.x: R must exist at same frame as G, and so on)
        /// </summary>
        public void SyncColorCommands()
        {
            if (EmaType != EmaType.light) throw new InvalidOperationException("EMA_Animation.SyncColorCommands: Method not valid for type = " + EmaType);

            var r_command = GetCommand("Color", "R");
            var g_command = GetCommand("Color", "G");
            var b_command = GetCommand("Color", "B");

            //There is atleast one color component on this animation
            if(r_command != null || g_command != null || b_command != null)
            {
                //Now we need to add the components that dont exist
                if(r_command == null)
                {
                    var newCommand = EMA_Command.GetNewLight();
                    newCommand.Component = "R";
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = 0, Value = 0 }); //First keyframe
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = EndFrame, Value = 0 }); //Last keyframe
                    Commands.Add(newCommand);
                }

                if (g_command == null)
                {
                    var newCommand = EMA_Command.GetNewLight();
                    newCommand.Component = "G";
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = 0, Value = 0 }); //First keyframe
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = EndFrame, Value = 0 }); //Last keyframe
                    Commands.Add(newCommand);
                }

                if (b_command == null)
                {
                    var newCommand = EMA_Command.GetNewLight();
                    newCommand.Component = "B";
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = 0, Value = 0 }); //First keyframe
                    newCommand.Keyframes.Add(new EMA_Keyframe() { Time = EndFrame, Value = 0 }); //Last keyframe
                    Commands.Add(newCommand);
                }

            }

            //Reload the commands now that they are all added.
            r_command = GetCommand("Color", "R");
            g_command = GetCommand("Color", "G");
            b_command = GetCommand("Color", "B");

            //Now sync the commands
            foreach (var anim in Commands)
            {
                foreach (var anim2 in Commands)
                {
                    if (anim.I_02 == 2 && anim2.I_02 == 2 && anim.I_03_a != 3 && anim2.I_03_a != 3)
                    {
                        anim.AddKeyframesFromCommand(anim2);
                    }
                }
            }
        }

        public EMA_Command GetCommand(string parameter, string component)
        {
            if (Commands == null) Commands = AsyncObservableCollection<EMA_Command>.Create();

            foreach(var command in Commands)
            {
                if (command.Parameter.ToLower() == parameter.ToLower() && command.Component.ToLower() == component.ToLower()) return command;
            }

            return null;
        }

        public List<RgbColor> GetUsedColors()
        {
            SyncColorCommands();

            List<RgbColor> colors = new List<RgbColor>();

            var r_command = GetCommand("Color", "R");
            var g_command = GetCommand("Color", "G");
            var b_command = GetCommand("Color", "B");

            foreach(var r in r_command.Keyframes)
            {
                var g = g_command.GetValue(r.Time);
                var b = b_command.GetValue(r.Time);

                colors.Add(new RgbColor(r.Value, g, b));
            }

            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet = false, int variance = 0)
        {
            var r_command = GetCommand("Color", "R");
            var g_command = GetCommand("Color", "G");
            var b_command = GetCommand("Color", "B");

            foreach (var r in r_command.Keyframes)
            {
                var g = g_command.GetValue(r.Time);
                var b = b_command.GetValue(r.Time);

                var hslColor = new RgbColor(r.Value, g, b).ToHsl();
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

                r_command.SetValue(r.Time, (float)convertedColor.R, undos);
                g_command.SetValue(r.Time, (float)convertedColor.G, undos);
                b_command.SetValue(r.Time, (float)convertedColor.B, undos);
            }

        }
    }

    [Serializable]
    [YAXSerializeAs("Command")]
    public class EMA_Command
    {
        private EmaType emaType = EmaType.obj;

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
        [YAXSerializeAs("Bone")]
        [YAXDontSerializeIfNull]
        public string BoneName { get; set; } //ushort

        [YAXAttributeForClass]
        [YAXSerializeAs("Parameter")]
        public string Parameter
        {
            get
            {
                return GetParameterString();
            }
            set
            {
                I_02 = GetParameterInt(value);
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("Component")]
        public string Component
        {
            get
            {
                return GetComponentString();
            }
            set
            {
                I_03_a = GetComponentInt(value);
            }
        }

        [YAXDontSerialize]
        public byte I_02 { get; set; } //Parameter
        
        [YAXDontSerialize]
        public byte I_03_a { get; set; } //uint4 (Component)
        [YAXDontSerialize]
        public bool I_03_b2_Int16ForTime = false; //Calculated on save
        [YAXDontSerialize]
        public bool I_03_b3_Int16ForValueIndex = false; //Calculated on save
        [YAXAttributeFor("Flags")]
        [YAXSerializeAs("Unk1")]
        public bool I_03_b1 { get; set; }
        [YAXAttributeFor("Flags")]
        [YAXSerializeAs("Unk4")]
        public bool I_03_b4 { get; set; }
        [YAXAttributeFor("Flags")]
        [YAXSerializeAs("Unk5")]
        public bool I_03_a4 { get; set; }
        
        public AsyncObservableCollection<EMA_Keyframe> Keyframes { get; set; }

        public static EMA_Command GetNewLight()
        {
            return new EMA_Command()
            {
                Keyframes = AsyncObservableCollection<EMA_Keyframe>.Create(),
                emaType = EmaType.light,
                I_02 = 2, //Color
            };
        }

        public static EMA_Command Read(byte[] rawBytes, int offset, float[] values, EMA_File emaFile, EmaType _emaType)
        {
            EMA_Command command = new EMA_Command();
            command.emaType = _emaType;

            if (emaFile.HasSkeleton)
            {
                command.BoneName = emaFile.GetBoneName(BitConverter.ToUInt16(rawBytes, offset + 0));
            }
            else
            {
                //This ema has no skeleton, thus no bones
                command.BoneName = null; 
            }
            
            command.I_02 = rawBytes[offset + 2];

            BitArray flags_b = new BitArray(new byte[1] { Int4Converter.ToInt4(rawBytes[offset + 3])[1] });
            BitArray flags_a = new BitArray(new byte[1] { Int4Converter.ToInt4(rawBytes[offset + 3])[0] });
            command.I_03_b1 = flags_b[0];
            command.I_03_b2_Int16ForTime = flags_b[1];
            command.I_03_b3_Int16ForValueIndex = flags_b[2];
            command.I_03_b4 = flags_b[3];
            command.I_03_a4 = flags_a[3];
            flags_a[3] = false;
            command.I_03_a = Int4Converter.GetByte(Utils.ConvertToByte(flags_a), 0);


            command.Keyframes = AsyncObservableCollection<EMA_Keyframe>.Create();

            ushort keyframeCount = BitConverter.ToUInt16(rawBytes, offset + 4);
            ushort indexOffset = BitConverter.ToUInt16(rawBytes, offset + 6);

            for(int i = 0; i < keyframeCount; i++)
            {
                ushort time;
                float value;
                string value2 = null;
                string value3 = null;
                string value4 = null;
                KeyframeFlags flags;

                if (command.I_03_b2_Int16ForTime)
                {
                    time = BitConverter.ToUInt16(rawBytes, offset + 8 + (i * 2));
                }
                else
                {
                    time = rawBytes[offset + 8 + i];
                }

                if (command.I_03_b3_Int16ForValueIndex)
                {
                    value = values[BitConverter.ToUInt16(rawBytes, offset + indexOffset + (i * 4))];
                    flags = (KeyframeFlags)BitConverter.ToUInt16(rawBytes, offset + indexOffset + 2 + (i * 4));
                    int extraOffset = 0;

                    if (flags.HasFlag(KeyframeFlags.QuadraticBezier))
                    {
                        ushort idx = (ushort)(BitConverter.ToUInt16(rawBytes, offset + indexOffset + (i * 4)) + 1);

                        //idx might be out of range due to a bug with an older version of the parser... so in that case set it to value
                        if (idx <= values.Length - 1)
                            value2 = values[idx].ToString();
                        else
                            value2 = value.ToString();

                        extraOffset++;
                    }

                    if (flags.HasFlag(KeyframeFlags.CubicBezier))
                    {
                        ushort idx = (ushort)(BitConverter.ToUInt16(rawBytes, offset + indexOffset + (i * 4)) + 1 + extraOffset);

                        //idx might be out of range due to a bug with an older version of the parser... so in that case set it to value
                        if (idx + 1 <= values.Length - 1)
                        {
                            value3 = values[idx].ToString();
                            value4 = values[idx + 1].ToString();
                        }
                        else
                        {
                            value3 = value.ToString();
                            value4 = value.ToString();
                        }

                        extraOffset++;
                    }

                }
                else
                {
                    value = values[rawBytes[offset + indexOffset + (i * 2)]];
                    flags = (KeyframeFlags)rawBytes[offset + indexOffset + 1 + (i * 2)];
                    int extraOffset = 0;

                    if (flags.HasFlag(KeyframeFlags.QuadraticBezier))
                    {
                        byte idx = (byte)(rawBytes[offset + indexOffset + (i * 2)] + 1);

                        if(idx <= values.Length -1)
                            value2 = values[idx].ToString();
                        else
                            value2 = value.ToString();
                    }

                    if (flags.HasFlag(KeyframeFlags.CubicBezier))
                    {
                        byte idx = (byte)(rawBytes[offset + indexOffset + (i * 2)] + 1 + extraOffset);

                        if (idx + 1 <= values.Length - 1)
                        {
                            value3 = values[idx].ToString();
                            value4 = values[idx + 1].ToString();
                        }
                        else
                        {
                            value3 = value.ToString();
                            value4 = value.ToString();
                        }
                    }
                }

                command.Keyframes.Add(new EMA_Keyframe()
                {
                    Time = time,
                    Value = value,
                    Flags = flags,
                    Value2 = value2,
                    CubicBezier_1 = value3,
                    CubicBezier_2 = value4
                });
            }

            return command;
        }

        public void SetFlags()
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Time > byte.MaxValue)
                    I_03_b2_Int16ForTime = true;
                if (keyframe.index > byte.MaxValue)
                    I_03_b3_Int16ForValueIndex = true;
                if ((ushort)keyframe.Flags > byte.MaxValue)
                    I_03_b3_Int16ForValueIndex = true;
            }
        }
        
        //Component range: 0 > 3 (3 bits only, remaining 5 are flags)

        private string GetParameterString()
        {
            switch (I_02)
            {
                case 0:
                    return (emaType == EmaType.light) ? I_02.ToString() : "Position";
                case 1:
                    return (emaType == EmaType.light) ? I_02.ToString() : "Rotation";
                case 2:
                    return (emaType == EmaType.light) ? "Color" : "Scale";
                case 3:
                    return (emaType == EmaType.light) ? "LightScale" : I_02.ToString();
                default:
                    return I_02.ToString();
            }
        }

        private byte GetParameterInt(string parameter)
        {
            parameter = parameter.ToLower();
            switch (parameter)
            {
                case "position":
                    return 0;
                case "rotation":
                    return 1;
                case "scale":
                case "color":
                    return 2;
                case "lightscale":
                    return 3;
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
            if(emaType != EmaType.light)
            {
                switch (I_03_a)
                {
                    case 0:
                        return "X";
                    case 1:
                        return "Y";
                    case 2:
                        return "Z";
                    default:
                        return I_03_a.ToString();
                }
            }
            else
            {
                switch (I_03_a)
                {
                    case 0:
                        return (I_02 == 2) ? "R" : I_03_a.ToString();
                    case 1:
                        return (I_02 == 2) ? "G" : I_03_a.ToString();
                    case 2:
                        return (I_02 == 2) ? "B" : I_03_a.ToString();
                    case 3:
                        return (I_02 == 2) ? "A" : I_03_a.ToString();
                    default:
                        return I_03_a.ToString();
                }
            }
        }

        private byte GetComponentInt(string component)
        {
            switch (component.ToLower())
            {
                case "x":
                case "r":
                    return 0;
                case "g":
                case "y":
                    return 1;
                case "b":
                case "z":
                    return 2;
                case "a":
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
            foreach (var keyframe in anim.Keyframes)
            {
                var existing = GetKeyframe(keyframe.Time);

                if (existing == null)
                {
                    var newKeyframe = new EMA_Keyframe() { Time = keyframe.Time, Value = GetValue(keyframe.Time) };
                    Keyframes.Add(newKeyframe);
                }
            }
        }

        public EMA_Keyframe GetKeyframe(int time)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Time == time) return keyframe;
            }

            return null;
        }

        public float GetValue(int time)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Time == time) return keyframe.Value;
            }

            return CalculateKeyframeValue(time);
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

        public float CalculateKeyframeValue(int time)
        {
            EMA_Keyframe before = GetKeyframeBefore(time);
            EMA_Keyframe after = GetKeyframeAfter(time);

            if (before == null) return 0f;

            if (after == null)
            {
                after = new EMA_Keyframe() { Time = ushort.MaxValue, Value = before.Value };
            }

            //Frame difference between previous frame and the current frame (current frame is AFTER the frame we want)
            int diff = after.Time - before.Time;

            //Keyframe value difference
            float keyframe2 = after.Value - before.Value;

            //Difference between the frame we WANT and the previous frame
            int diff2 = time - before.Time;

            //Divide keyframe value difference by the keyframe time difference, and then multiply it by diff2, then add the previous keyframe value
            return (keyframe2 / diff) * diff2 + before.Value;
        }

        /// <summary>
        /// Returns the keyframe that appears just before the specified frame
        /// </summary>
        /// <returns></returns>
        public EMA_Keyframe GetKeyframeBefore(int time)
        {
            SortKeyframes();
            EMA_Keyframe prev = null;

            foreach (var keyframe in Keyframes)
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
            SortKeyframes();
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Time > time) return keyframe;
            }

            return null;
        }

        public void SortKeyframes()
        {
            Keyframes = Sorting.SortEntries2(Keyframes);
        }

    }

    [Serializable]
    [YAXSerializeAs("Keyframe")]
    public class EMA_Keyframe : ISortable
    {
        [YAXDontSerialize]
        public int SortID { get { return Time; } }

        [YAXAttributeForClass]
        [YAXSerializeAs("Time")]
        public ushort Time { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Value")]
        [YAXFormat("0.0########")]
        public float Value { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("QuadraticBezier")]
        [YAXDontSerializeIfNull]
        public string Value2 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("CubicBezier_1")]
        [YAXDontSerializeIfNull]
        public string CubicBezier_1 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("CubicBezier_2")]
        [YAXDontSerializeIfNull]
        public string CubicBezier_2 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Flags")]
        public KeyframeFlags Flags { get; set; }

        [YAXDontSerialize]
        public int index = -1;
        [YAXDontSerialize]
        public float Value2Float
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Value2)) return Value;
                return float.Parse(Value2);
            }
            set
            {
                Value2 = value.ToString();
            }
        }
        [YAXDontSerialize]
        public float Value3Float
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CubicBezier_1)) return Value;
                return float.Parse(CubicBezier_1);
            }
            set
            {
                CubicBezier_1 = value.ToString();
            }
        }
        [YAXDontSerialize]
        public float Value4Float
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CubicBezier_2)) return Value;
                return float.Parse(CubicBezier_2);
            }
            set
            {
                CubicBezier_2 = value.ToString();
            }
        }


    }


}
