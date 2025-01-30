using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.ESK;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.EAN
{
    public class Deserializer
    {

        string saveLocation;
        EAN_File eanFile;
        public List<byte> bytes = new List<byte>() { 35, 69, 65, 78, 254, 255, 32, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(EAN_File), YAXSerializationOptions.DontSerializeNullObjects);
            eanFile = (EAN_File)serializer.DeserializeFromFile(location);
            eanFile.Skeleton.CreateNonRecursiveBoneList();
            eanFile.ValidateAnimationIndexes();
            eanFile.SortEntries();
            ValidateAnimationBones();
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EAN_File _eanFile, string location)
        {
            saveLocation = location;
            eanFile = _eanFile;
            eanFile.Skeleton.CreateNonRecursiveBoneList();
            eanFile.ValidateAnimationIndexes();
            eanFile.SortEntries();
            ValidateAnimationBones();
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EAN_File _eanFile)
        {
            eanFile = _eanFile;
            eanFile.Skeleton.CreateNonRecursiveBoneList();
            eanFile.ValidateAnimationIndexes();
            eanFile.SortEntries();
           
            ValidateAnimationBones();
           
            Write();
        }

        private void ValidateAnimationBones()
        {
            //Removes any bone from any animation if it doesn't exist in the internal ESK.
            //This is a safe-guard method.

            if(eanFile.Animations == null)
            {
                return;
            }

            for(int i = 0; i < eanFile.Animations.Count; i++)
            {
                if(eanFile.Animations[i].Nodes != null)
                {
                    for (int a = eanFile.Animations[i].Nodes.Count - 1; a >= 0; a--)
                    {
                        if(!eanFile.Skeleton.NonRecursiveBones.Any(x => x.Name == eanFile.Animations[i].Nodes[a].BoneName))
                        {
                            eanFile.Animations[i].Nodes.RemoveAt(a);
                        }
                    }
                }
            }
      
        }
        
        private void Write()
        {
            int AnimationCount = eanFile.GetAnimationIndexCount();

            //Header
            int IsCamera = (eanFile.IsCamera == true) ? 1 : 0;
            bytes.AddRange(BitConverter.GetBytes(eanFile.I_08));
            bytes.AddRange(new byte[4]);
            bytes.Add((byte)IsCamera);
            bytes.Add(eanFile.I_17);
            bytes.AddRange(BitConverter.GetBytes((short)AnimationCount));
            bytes.AddRange(BitConverter.GetBytes((int)32)); //Skeleton offset
            bytes.AddRange(new byte[8]);

            //Skeleton
            bytes.AddRange(eanFile.Skeleton.Write());

            //Animation
            if(AnimationCount > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 24);
                List<int> AnimationTable = new List<int>();

                for(int i = 0; i < AnimationCount; i++)
                {
                    AnimationTable.Add(bytes.Count);
                    bytes.AddRange(new byte[4]);
                }

                for(int i = 0; i < AnimationCount; i++)
                {
                    int _idxOfAnimation = eanFile.IndexOf(i);
                    if (_idxOfAnimation != -1)
                    {
                        StartNewLine();
                        WriteAnimation(eanFile.Animations[_idxOfAnimation], AnimationTable[i]);
                    }
                }
            }

            //Animation Names
            if(AnimationCount > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 28);
                List<int> NameTable = new List<int>();

                //Name Table
                for(int i = 0; i < AnimationCount; i++)
                {
                    NameTable.Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                }

                //Name Strings
                for(int i = 0; i < AnimationCount; i++)
                {
                    int _idxOfAnimation = eanFile.IndexOf(i);
                    if(_idxOfAnimation != -1)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), NameTable[i]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(eanFile.Animations[_idxOfAnimation].Name));
                        bytes.Add(0);
                    }
                }
            }
        }

        private void WriteAnimation(EAN_Animation animation, int offsetToReplace)
        {
            StartNewLine();

            if (animation.GetFrameCount(true) <= 0)
                throw new InvalidDataException("EAN Save: FrameCount cannot be 0 or less!");

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), offsetToReplace);
            int startOffset = bytes.Count;
            int nodeCount = (animation.Nodes != null) ? animation.Nodes.Count : 0;

            if(nodeCount > 0)
            {
                nodeCount = 0;
                for(int i = 0; i < animation.Nodes.Count; i++)
                {
                    if(eanFile.Skeleton.Exists(animation.Nodes[i].BoneName))
                    {
                        nodeCount++;
                    }
                }
            }

            int frameCount = animation.GetFrameCount();

            bytes.AddRange(new byte[2]);
            bytes.Add((byte)animation.IndexSize);
            bytes.Add((byte)animation.FloatSize);
            bytes.AddRange(BitConverter.GetBytes(frameCount));
            bytes.AddRange(BitConverter.GetBytes(nodeCount));
            bytes.AddRange(new byte[4]);

            //Nodes
            if(nodeCount > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - startOffset), startOffset + 12);
                List<int> NodeTable = new List<int>();
                for (int i = 0; i < nodeCount; i++)
                {
                    NodeTable.Add(bytes.Count);
                    bytes.AddRange(new byte[4]);
                }

                for (int i = 0; i < nodeCount; i++)
                {
                    if(eanFile.Skeleton.Exists(animation.Nodes[i].BoneName))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - startOffset), NodeTable[i]);
                        int NodeOffset = bytes.Count;
                        List<int> AnimationComponentTable = new List<int>();

                        int AnimationComponentCount = (animation.Nodes[i].AnimationComponents != null) ? animation.Nodes[i].AnimationComponents.Count : 0;
                        bytes.AddRange(BitConverter.GetBytes(GetBoneIndex(animation.Nodes[i].BoneName, animation.Name)));
                        bytes.AddRange(BitConverter.GetBytes((short)AnimationComponentCount));
                        bytes.AddRange(BitConverter.GetBytes(8));

                        //Get ESK data for this bone as it will be needed for calculating default keyframes
                        ESK_RelativeTransform eskRelativeTransform = eanFile.Skeleton.GetBone(animation.Nodes[i].BoneName)?.RelativeTransform;

                        if (eskRelativeTransform == null)
                            throw new ArgumentNullException($"EAN Save: Could not find the RelativeTransform for bone {animation.Nodes[i].BoneName}");

                        //Table
                        for (int a = 0; a < AnimationComponentCount; a++)
                        {
                            AnimationComponentTable.Add(bytes.Count());
                            bytes.AddRange(new byte[4]);
                        }

                        //Data
                        for (int a = 0; a < AnimationComponentCount; a++)
                        {
                            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - NodeOffset), AnimationComponentTable[a]);

                            int KeyframeOffset = bytes.Count;

                            //Can happen if loaded from XML and no keyframes were declared.
                            if (animation.Nodes[i].AnimationComponents[a].Keyframes == null)
                                animation.Nodes[i].AnimationComponents[a].Keyframes = new AsyncObservableCollection<EAN_Keyframe>();

                            //Handle first and last keyframe
                            bool hasFirstKeyframe = animation.Nodes[i].AnimationComponents[a].Keyframes.Any(x => x.FrameIndex == 0);
                            bool hasFinalKeyframe = animation.Nodes[i].AnimationComponents[a].Keyframes.Any(x => x.FrameIndex == animation.GetFrameCount() - 1);
                            int KeyframeCount = animation.Nodes[i].AnimationComponents[a].Keyframes.Count;

                            //If no first or final keyframes were found, increment count to include them
                            if (!hasFinalKeyframe) KeyframeCount++;
                            if (!hasFirstKeyframe) KeyframeCount++;

                            //Select default keyframe
                            EAN_Keyframe defaultKeyframe = null;

                            switch (animation.Nodes[i].AnimationComponents[a].Type)
                            {
                                case EAN_AnimationComponent.ComponentType.Position:
                                    defaultKeyframe = eskRelativeTransform.ToEanPosKeyframe();
                                    break;
                                case EAN_AnimationComponent.ComponentType.Rotation:
                                    defaultKeyframe = eskRelativeTransform.ToEanRotKeyframe();
                                    break;
                                case EAN_AnimationComponent.ComponentType.Scale:
                                    defaultKeyframe = eskRelativeTransform.ToEanScaleKeyframe();
                                    break;
                            }

                            //Write
                            bytes.Add((byte)animation.Nodes[i].AnimationComponents[a].Type);
                            bytes.Add(animation.Nodes[i].AnimationComponents[a].I_01);
                            bytes.AddRange(BitConverter.GetBytes(animation.Nodes[i].AnimationComponents[a].I_02));
                            bytes.AddRange(BitConverter.GetBytes(KeyframeCount));
                            bytes.AddRange(new byte[8]);

                            //Not needed now, right? Declared FrameCount is read-only and always calculated based on keyframes.
                            //Validate
                            //if(animation.Nodes[i].AnimationComponents[a].Keyframes.Any(x => x.FrameIndex > (animation.GetFrameCount() - 1)))
                            //    throw new Exception($"EAN Save: Keyframe FrameIndex must not exceed the animation duration. (Name: {animation.Name}, ID: {animation.ID_UShort}");

                            //Write Keyframes
                            if (KeyframeCount > 0)
                            {
                                Sorting.SortEntries2(animation.Nodes[i].AnimationComponents[a].Keyframes);

                                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - KeyframeOffset), KeyframeOffset + 8);
                                switch (animation.IndexSize)
                                {
                                    case EAN_Animation.IntPrecision._8Bit:
                                        WriteKeyframeIndex_Int8(animation.Nodes[i].AnimationComponents[a].Keyframes, hasFirstKeyframe, hasFinalKeyframe, animation.GetFrameCount() - 1);
                                        break;
                                    case EAN_Animation.IntPrecision._16Bit:
                                        WriteKeyframeIndex_Int16(animation.Nodes[i].AnimationComponents[a].Keyframes, hasFirstKeyframe, hasFinalKeyframe, animation.GetFrameCount() - 1);
                                        break;
                                }

                                StartNewLine();
                                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - KeyframeOffset), KeyframeOffset + 12);
                                switch (animation.FloatSize)
                                {
                                    case EAN_Animation.FloatPrecision._16Bit:
                                        WriteKeyframeFloats_Float16(animation.Nodes[i].AnimationComponents[a].Keyframes, hasFirstKeyframe, hasFinalKeyframe, defaultKeyframe);
                                        break;
                                    case EAN_Animation.FloatPrecision._32Bit:
                                        WriteKeyframeFloats_Float32(animation.Nodes[i].AnimationComponents[a].Keyframes, hasFirstKeyframe, hasFinalKeyframe, defaultKeyframe);
                                        break;
                                }

                            }

                        }
                    }
                }

            }

            bytes.AddRange(new byte[12]);

        }

        private void WriteKeyframeIndex_Int8(IList<EAN_Keyframe> keyframes, bool hasFirstKeyframe, bool hasFinalKeyframe, int endFrame)
        {
            if (!hasFirstKeyframe)
            {
                bytes.Add(Convert.ToByte(0));
            }

            for(int i = 0; i < keyframes.Count; i++)
            {
                if(keyframes[i].FrameIndex > 255 || keyframes[i].FrameIndex < 0)
                {
                    throw new Exception("Failed. An animation with IndexSize == _UInt8 cannot have a keyframe index exceeding 255, or less than 0!");
                }
                bytes.Add(Convert.ToByte(keyframes[i].FrameIndex));
            }

            if (!hasFinalKeyframe)
            {
                bytes.Add(Convert.ToByte((byte)endFrame));
            }
        }

        private void WriteKeyframeIndex_Int16(IList<EAN_Keyframe> keyframes, bool hasFirstKeyframe, bool hasFinalKeyframe, int endFrame)
        {
            if (!hasFirstKeyframe)
            {
                bytes.AddRange(BitConverter.GetBytes((ushort)0));
            }

            for (int i = 0; i < keyframes.Count; i++)
            {
                if (keyframes[i].FrameIndex > 65535 || keyframes[i].FrameIndex < 0)
                {
                    throw new Exception("Failed. An animation with IndexSize == _UInt16 cannot have a keyframe index exceeding 65535, or less than 0!");
                }
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].FrameIndex));
            }

            if (!hasFinalKeyframe)
            {
                bytes.AddRange(BitConverter.GetBytes((ushort)endFrame));
            }
        }

        private void WriteKeyframeFloats_Float16(IList<EAN_Keyframe> keyframes, bool hasFirstKeyframe, bool hasFinalKeyframe, EAN_Keyframe defaultKeyframe)
        {
            if (!hasFirstKeyframe)
            {
                EAN_Keyframe defaultFirstKeyframe = (keyframes.Count == 0) ? defaultKeyframe : keyframes[0];
                bytes.AddRange(Half.GetBytes((Half)defaultFirstKeyframe.X));
                bytes.AddRange(Half.GetBytes((Half)defaultFirstKeyframe.Y));
                bytes.AddRange(Half.GetBytes((Half)defaultFirstKeyframe.Z));
                bytes.AddRange(Half.GetBytes((Half)defaultFirstKeyframe.W));
            }

            for(int i = 0; i < keyframes.Count; i++)
            {
                bytes.AddRange(Half.GetBytes((Half)keyframes[i].X));
                bytes.AddRange(Half.GetBytes((Half)keyframes[i].Y));
                bytes.AddRange(Half.GetBytes((Half)keyframes[i].Z));
                bytes.AddRange(Half.GetBytes((Half)keyframes[i].W));
            }

            if (!hasFinalKeyframe)
            {
                EAN_Keyframe defaultFinalKeyframe = (keyframes.Count == 0) ? defaultKeyframe : keyframes[keyframes.Count - 1];
                bytes.AddRange(Half.GetBytes((Half)defaultFinalKeyframe.X));
                bytes.AddRange(Half.GetBytes((Half)defaultFinalKeyframe.Y));
                bytes.AddRange(Half.GetBytes((Half)defaultFinalKeyframe.Z));
                bytes.AddRange(Half.GetBytes((Half)defaultFinalKeyframe.W));
            }
        }

        private void WriteKeyframeFloats_Float32(IList<EAN_Keyframe> keyframes, bool hasFirstKeyframe, bool hasFinalKeyframe, EAN_Keyframe defaultKeyframe)
        {
            if (!hasFirstKeyframe)
            {
                EAN_Keyframe defaultFirstKeyframe = (keyframes.Count == 0) ? defaultKeyframe : keyframes[0];
                bytes.AddRange(BitConverter.GetBytes(defaultFirstKeyframe.X));
                bytes.AddRange(BitConverter.GetBytes(defaultFirstKeyframe.Y));
                bytes.AddRange(BitConverter.GetBytes(defaultFirstKeyframe.Z));
                bytes.AddRange(BitConverter.GetBytes(defaultFirstKeyframe.W));
            }

            for (int i = 0; i < keyframes.Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].X));
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].Y));
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].Z));
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].W));
            }

            if (!hasFinalKeyframe)
            {
                EAN_Keyframe defaultFinalKeyframe = (keyframes.Count == 0) ? defaultKeyframe : keyframes[keyframes.Count - 1];
                bytes.AddRange(BitConverter.GetBytes(defaultFinalKeyframe.X));
                bytes.AddRange(BitConverter.GetBytes(defaultFinalKeyframe.Y));
                bytes.AddRange(BitConverter.GetBytes(defaultFinalKeyframe.Z));
                bytes.AddRange(BitConverter.GetBytes(defaultFinalKeyframe.W));
            }
        }

        //Utility

        private void StartNewLine()
        {
            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);
        }

        private short GetBoneIndex(string name, string animationName)
        {
            for(int i = 0; i < eanFile.Skeleton.NonRecursiveBones.Count; i++)
            {
                if(eanFile.Skeleton.NonRecursiveBones[i].Name == name)
                {
                    return (short)i;
                }
            }
            
            throw new Exception(String.Format("Could not find the bone \"{0}\", which is declared in the animation \"{1}\" in the skeleton!\nRebuild failed.", name, animationName));
            
        }

        

    }


}
