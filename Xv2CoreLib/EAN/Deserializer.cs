using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.EAN
{
    public class Deserializer
    {

        string saveLocation;
        EAN_File eanFile;
        public List<byte> bytes = new List<byte>() { 35, 69, 65, 78, 254, 255, 32, 0 };
        private bool writeXmlMode { get; set; }
        List<ESK_BoneNonHierarchal> nonHierarchalBones = null;

        public Deserializer(string location)
        {
            writeXmlMode = true;
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(EAN_File), YAXSerializationOptions.DontSerializeNullObjects);
            eanFile = (EAN_File)serializer.DeserializeFromFile(location);
            
            eanFile.ValidateAnimationIndexes();
            eanFile.SortEntries();

            nonHierarchalBones = eanFile.Skeleton.GetNonHierarchalBoneList();
            ValidateAnimationBones();
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EAN_File _eanFile, string location)
        {
            writeXmlMode = false;
            saveLocation = location;
            eanFile = _eanFile;
            eanFile.ValidateAnimationIndexes();
            eanFile.SortEntries();
            nonHierarchalBones = eanFile.Skeleton.GetNonHierarchalBoneList();
            ValidateAnimationBones();
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EAN_File _eanFile)
        {
            writeXmlMode = false;
            eanFile = _eanFile;
            eanFile.ValidateAnimationIndexes();
            eanFile.SortEntries();
            nonHierarchalBones = eanFile.Skeleton.GetNonHierarchalBoneList();
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
                    for (int a = eanFile.Animations[i].Nodes.Count - 1; i >= 0; i--)
                    {
                        if(!nonHierarchalBones.Any(x => x.Name == eanFile.Animations[i].Nodes[a].BoneName))
                        {
                            eanFile.Animations[i].Nodes.RemoveAt(a);
                        }
                    }
                }
            }

        }
        
        private void Write()
        {
            int AnimationCount = eanFile.AnimationCount();

            //Header
            int IsCamera = (eanFile.IsCamera == true) ? 1 : 0;
            bytes.AddRange(BitConverter.GetBytes(eanFile.I_08));
            bytes.AddRange(new byte[4]);
            bytes.Add((byte)IsCamera);
            bytes.Add(eanFile.I_17);
            bytes.AddRange(BitConverter.GetBytes((short)AnimationCount));
            bytes.AddRange(new byte[12]);

            //Skeleton
            WriteSkeleton(eanFile.Skeleton, 20);

            //Animation
            if(AnimationCount > 0)
            {
                bytes = Xv2CoreLib.Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 24);
                List<int> AnimationTable = new List<int>();

                for(int i = 0; i < AnimationCount; i++)
                {
                    AnimationTable.Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                }

                for(int i = 0; i < AnimationCount; i++)
                {
                    int _idxOfAnimation = eanFile.IndexOfAnimation(i);
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
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 28);
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
                    int _idxOfAnimation = eanFile.IndexOfAnimation(i);
                    if(_idxOfAnimation != -1)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), NameTable[i]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(eanFile.Animations[_idxOfAnimation].Name));
                        bytes.Add(0);
                    }
                }
            }
        }

        private void WriteSkeleton(ESK_Skeleton skeleton, int offsetToReplace)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), offsetToReplace);

            int startOffset = bytes.Count();
            int count = (nonHierarchalBones != null) ? nonHierarchalBones.Count() : 0;

            bytes.AddRange(BitConverter.GetBytes((short)count));
            bytes.AddRange(BitConverter.GetBytes(skeleton.I_02));
            bytes.AddRange(new byte[24]);
            bytes.AddRange(BitConverter_Ex.GetBytes(skeleton.I_28));

            if (count > 0)
            {
                //Writing Index List
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 4);

                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].Index1));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].Index2));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].Index3));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].Index4));
                }

                //Writing Name Table and List
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 8);
                List<Xv2CoreLib.StringWriter.StringInfo> stringInfo = new List<Xv2CoreLib.StringWriter.StringInfo>();

                for (int i = 0; i < count; i++)
                {
                    stringInfo.Add(new Xv2CoreLib.StringWriter.StringInfo()
                    {
                        StringToWrite = nonHierarchalBones[i].Name,
                        Offset = bytes.Count(),
                        RelativeOffset = startOffset
                    });
                    bytes.AddRange(new byte[4]);
                }

                for (int i = 0; i < count; i++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - stringInfo[i].RelativeOffset), stringInfo[i].Offset);
                    bytes.AddRange(Encoding.ASCII.GetBytes(stringInfo[i].StringToWrite));
                    bytes.Add(0);
                }

                //Writing RelativeTransform
                StartNewLine();
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 12);

                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_00));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_04));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_08));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_12));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_16));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_20));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_24));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_28));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_32));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_36));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_40));
                    bytes.AddRange(BitConverter.GetBytes(nonHierarchalBones[i].RelativeTransform.F_44));
                }

                //Writing AbsoluteTransform (esk only)
                //StartNewLine();
                //bytes = CommonOperations.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 16);

               // for (int i = 0; i < count; i++)
               // {
               //     bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_00));
                //    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_04));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_08));
                //    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_12));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_16));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_20));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_24));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_28));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_32));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_36));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_40));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_44));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_48));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_52));
                 //   bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_56));
                //    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_60));
                //}

                //Writing Unk1
                if (skeleton.Unk1 != null)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 20);
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_00));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_04));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_08));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_12));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_16));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_20));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_24));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_28));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_32));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_36));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_40));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_44));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_48));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_52));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_56));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_60));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_64));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_68));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_72));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_76));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_80));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_84));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_88));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_92));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_96));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_100));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_104));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_108));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_112));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_116));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_120));
                }

                //Writing Unk2
                if (skeleton.UseUnk2 == true && count > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 24);

                    for (int i = 0; i < count; i++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(281470681743360));
                    }
                }

            }


        }

        private void WriteAnimation(EAN_Animation animation, int offsetToReplace)
        {
            StartNewLine();

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), offsetToReplace);
            int startOffset = bytes.Count();
            int nodeCount = (animation.Nodes != null) ? animation.Nodes.Count() : 0;

            if(nodeCount > 0)
            {
                nodeCount = 0;
                for(int i = 0; i < animation.Nodes.Count(); i++)
                {
                    if(eanFile.Skeleton.Exists(animation.Nodes[i].BoneName))
                    {
                        nodeCount++;
                    }
                }
            }

            bytes.AddRange(new byte[2]);
            bytes.Add((byte)animation.I_02);
            bytes.Add((byte)animation.I_03);
            bytes.AddRange(BitConverter.GetBytes(animation.I_04));
            bytes.AddRange(BitConverter.GetBytes(nodeCount));
            bytes.AddRange(new byte[4]);

            //Nodes
            if(nodeCount > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 12);
                List<int> NodeTable = new List<int>();
                for (int i = 0; i < nodeCount; i++)
                {
                    NodeTable.Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                }

                for (int i = 0; i < nodeCount; i++)
                {
                    if(eanFile.Skeleton.Exists(animation.Nodes[i].BoneName))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), NodeTable[i]);
                        int NodeOffset = bytes.Count();
                        List<int> AnimationComponentTable = new List<int>();

                        int AnimationComponentCount = (animation.Nodes[i].AnimationComponents != null) ? animation.Nodes[i].AnimationComponents.Count() : 0;
                        bytes.AddRange(BitConverter.GetBytes(GetBoneIndex(animation.Nodes[i].BoneName, animation.Name)));
                        bytes.AddRange(BitConverter.GetBytes((short)AnimationComponentCount));
                        bytes.AddRange(BitConverter.GetBytes(8));

                        //Table
                        for (int a = 0; a < AnimationComponentCount; a++)
                        {
                            AnimationComponentTable.Add(bytes.Count());
                            bytes.AddRange(new byte[4]);
                        }

                        //Data
                        for (int a = 0; a < AnimationComponentCount; a++)
                        {
                            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - NodeOffset), AnimationComponentTable[a]);

                            int KeyframeOffset = bytes.Count();
                            int KeyframeCount = (animation.Nodes[i].AnimationComponents[a].Keyframes != null) ? animation.Nodes[i].AnimationComponents[a].Keyframes.Count() : 0;

                            bytes.Add((byte)animation.Nodes[i].AnimationComponents[a].I_00);
                            bytes.Add(animation.Nodes[i].AnimationComponents[a].I_01);
                            bytes.AddRange(BitConverter.GetBytes(animation.Nodes[i].AnimationComponents[a].I_02));
                            bytes.AddRange(BitConverter.GetBytes(KeyframeCount));
                            bytes.AddRange(new byte[8]);

                            //Sort Keyframes
                            if (KeyframeCount > 0)
                            {
                                var sortedList = animation.Nodes[i].AnimationComponents[a].Keyframes.ToList();
                                sortedList.Sort((x, y) => x.FrameIndex - y.FrameIndex);
                                animation.Nodes[i].AnimationComponents[a].Keyframes = new ObservableCollection<EAN_Keyframe>(sortedList);
                            }

                            if (KeyframeCount > 0)
                            {
                                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - KeyframeOffset), KeyframeOffset + 8);
                                switch (animation.I_02)
                                {
                                    case EAN_Animation.IntPrecision._8Bit:
                                        WriteKeyframeIndex_Int8(animation.Nodes[i].AnimationComponents[a].Keyframes);
                                        break;
                                    case EAN_Animation.IntPrecision._16Bit:
                                        WriteKeyframeIndex_Int16(animation.Nodes[i].AnimationComponents[a].Keyframes);
                                        break;
                                }

                                StartNewLine();
                                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - KeyframeOffset), KeyframeOffset + 12);
                                switch (animation.I_03)
                                {
                                    case EAN_Animation.FloatPrecision._16Bit:
                                        WriteKeyframeFloats_Float16(animation.Nodes[i].AnimationComponents[a].Keyframes);
                                        break;
                                    case EAN_Animation.FloatPrecision._32Bit:
                                        WriteKeyframeFloats_Float32(animation.Nodes[i].AnimationComponents[a].Keyframes);
                                        break;
                                }
                            }


                        }
                    }
                }

            }

            bytes.AddRange(new byte[12]);

        }

        private void WriteKeyframeIndex_Int8(ObservableCollection<EAN_Keyframe> keyframes)
        {
            for(int i = 0; i < keyframes.Count(); i++)
            {
                if(keyframes[i].FrameIndex > 255 || keyframes[i].FrameIndex < 0)
                {
                    throw new Exception("Failed. An animation with IndexSize == _UInt8 cannot have a keyframe index exceeding 255, or less than 0!");
                }
                bytes.Add(Convert.ToByte(keyframes[i].FrameIndex));
            }
        }

        private void WriteKeyframeIndex_Int16(ObservableCollection<EAN_Keyframe> keyframes)
        {
            for (int i = 0; i < keyframes.Count(); i++)
            {
                if (keyframes[i].FrameIndex > 65535 || keyframes[i].FrameIndex < 0)
                {
                    throw new Exception("Failed. An animation with IndexSize == _UInt16 cannot have a keyframe index exceeding 65535, or less than 0!");
                }
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].FrameIndex));
            }
        }

        private void WriteKeyframeFloats_Float16(ObservableCollection<EAN_Keyframe> keyframes)
        {
            for(int i = 0; i < keyframes.Count(); i++)
            {
                int size = bytes.Count();
                bytes.AddRange(Half.GetBytes((Half)keyframes[i].X));
                bytes.AddRange(Half.GetBytes((Half)keyframes[i].Y));
                bytes.AddRange(Half.GetBytes((Half)keyframes[i].Z));
                bytes.AddRange(Half.GetBytes((Half)keyframes[i].W));
                if (size + 8 != bytes.Count())
                {
                    throw new Exception(String.Format("Assert Fail: Float16 struct is wrong size! (expected 8, was {0}).\nSave failed.", bytes.Count() - size));
                }
            }
        }

        private void WriteKeyframeFloats_Float32(ObservableCollection<EAN_Keyframe> keyframes)
        {
            for (int i = 0; i < keyframes.Count(); i++)
            {
                int size = bytes.Count();

                bytes.AddRange(BitConverter.GetBytes(keyframes[i].X));
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].Y));
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].Z));
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].W));
                if (size + 16 != bytes.Count())
                {
                    throw new Exception(String.Format("Assert Fail: Float32 struct is wrong size! (expected 16, was {0}).\nSave failed.", bytes.Count() - size));
                }
            }
        }

        //Utility

        private void StartNewLine()
        {
            while(Convert.ToSingle(bytes.Count()) / 16 != Math.Floor(Convert.ToSingle(bytes.Count()) / 16))
            {
                bytes.Add(0);
            }
        }

        private short GetBoneIndex(string name, string animationName)
        {
            for(int i = 0; i < nonHierarchalBones.Count(); i++)
            {
                if(nonHierarchalBones[i].Name == name)
                {
                    return (short)i;
                }
            }
            
            throw new Exception(String.Format("Could not find the bone \"{0}\", which is declared in the animation \"{1}\" in the skeleton!\nRebuild failed.", name, animationName));
            
        }

        private List<EAN_Keyframe> ModifyKeyframes(float x, float y, float z, float w, List<EAN_Keyframe> keyframes)
        {
            for(int i = 0; i < keyframes.Count(); i++)
            {
                float _x = Convert.ToSingle(keyframes[i].X) + x;
                float _y = Convert.ToSingle(keyframes[i].Y) + y;
                float _z = Convert.ToSingle(keyframes[i].Z) + z;
                float _w = Convert.ToSingle(keyframes[i].W) + w;

                keyframes[i].X = _x;
                keyframes[i].Y = _y;
                keyframes[i].Z = _z;
                keyframes[i].W = _w;
            }
            return keyframes;
        }

        private List<EAN_Keyframe> ZeroKeyframes(List<EAN_Keyframe> keyframes)
        {
            for (int i = 0; i < keyframes.Count(); i++)
            {
                keyframes[i].X = 0f;
                keyframes[i].Y = 0f;
                keyframes[i].Z = 0f;
                keyframes[i].W = 0f;
            }
            return keyframes;
        }

        

    }


}
