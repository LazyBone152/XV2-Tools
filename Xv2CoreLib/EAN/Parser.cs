using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using Xv2CoreLib;

namespace Xv2CoreLib.EAN
{
    public class Parser
    {
        string saveLocation { get; set; }
        public EAN_File eanFile { private set; get; }
        byte[] rawBytes { get; set; }
        List<byte> bytes { get; set; }
        private int boneCount = 0;


        public Parser(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
            bytes = rawBytes.ToList();
            eanFile = new EAN_File();
            Parse();
        }

        public Parser(string location, bool writeXml = false)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);
            bytes = rawBytes.ToList();
            eanFile = new EAN_File();
            Parse();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EAN_File));
                serializer.SerializeToFile(eanFile, saveLocation + ".xml");
            }
        }

        private void Parse()
        {
            int AnimationCount = BitConverter.ToInt16(rawBytes, 18);
            int SkeletonOffset = BitConverter.ToInt32(rawBytes, 20);
            int AnimationOffset = BitConverter.ToInt32(rawBytes, 24);
            int AnimationNamesOffset = BitConverter.ToInt32(rawBytes, 28);

            //Header
            eanFile.I_08 = BitConverter.ToInt32(rawBytes, 8);
            eanFile.IsCamera = (rawBytes[16] == 0) ? false : true;
            eanFile.I_17 = rawBytes[17];

            //Skeleton
            ParseSkeleton(SkeletonOffset);

            //Animations
            eanFile.Animations = new ObservableCollection<EAN_Animation>();
            if (AnimationCount > 0)
            {
                for(int i = 0; i < AnimationCount; i++)
                {
                    if(BitConverter.ToInt32(rawBytes, AnimationOffset) != 0)
                    {
                        eanFile.Animations.Add(ParseAnimation(BitConverter.ToInt32(rawBytes, AnimationOffset), BitConverter.ToInt32(rawBytes, AnimationNamesOffset), i));
                    }
                    AnimationOffset += 4;
                    AnimationNamesOffset += 4;
                }

            }
        }

        //Animations
        private EAN_Animation ParseAnimation(int offset, int nameOffset, int animIndex)
        {
            List<ESK_BoneNonHierarchal> bonesNonHierachal = eanFile.Skeleton.GetNonHierarchalBoneList();

            EAN_Animation animation = new EAN_Animation();

            byte indexSize = rawBytes[offset + 2];
            byte floatSize = rawBytes[offset + 3];
            int nodeCount = BitConverter.ToInt32(rawBytes, offset + 8);
            int nodeOffset = BitConverter.ToInt32(rawBytes, offset + 12) + offset;

            //animation header data
            ValidateFloatAndIntPrecision(indexSize, floatSize);
            animation.I_02 = (EAN_Animation.IntPrecision)indexSize;
            animation.I_03 = (EAN_Animation.FloatPrecision)floatSize;
            animation.I_04 = BitConverter.ToInt32(rawBytes, offset + 4);

            if(nodeCount > 0)
            {
                animation.Nodes = new ObservableCollection<EAN_Node>();
                for(int i = 0; i < nodeCount; i++)
                {
                    int thisNodeOffset = BitConverter.ToInt32(rawBytes, nodeOffset) + offset;
                    animation.Nodes.Add(new EAN_Node() { BoneName = GetBoneName(BitConverter.ToInt16(rawBytes, thisNodeOffset), animIndex, bonesNonHierachal) });
                    int keyframedAnimationsCount = BitConverter.ToInt16(rawBytes, thisNodeOffset + 2);
                    int keyframedAnimationsOffset = BitConverter.ToInt32(rawBytes, thisNodeOffset + 4) + thisNodeOffset;

                    if(keyframedAnimationsCount > 0)
                    {
                        animation.Nodes[i].AnimationComponents = new ObservableCollection<EAN_AnimationComponent>();
                        for(int a = 0; a < keyframedAnimationsCount; a++)
                        {
                            int thisKeyframedAnimationsOffset = BitConverter.ToInt32(rawBytes, keyframedAnimationsOffset) + thisNodeOffset;
                            animation.Nodes[i].AnimationComponents.Add(new EAN_AnimationComponent());
                            animation.Nodes[i].AnimationComponents[a].I_00 = (EAN_AnimationComponent.ComponentType)rawBytes[thisKeyframedAnimationsOffset + 0];
                            animation.Nodes[i].AnimationComponents[a].I_01 = rawBytes[thisKeyframedAnimationsOffset + 1];
                            animation.Nodes[i].AnimationComponents[a].I_02 = BitConverter.ToInt16(rawBytes, thisKeyframedAnimationsOffset + 2);

                            //Offsets/Count
                            int keyframeCount = BitConverter.ToInt32(rawBytes, thisKeyframedAnimationsOffset + 4);
                            int IndexListOffset = BitConverter.ToInt32(rawBytes, thisKeyframedAnimationsOffset + 8) + thisKeyframedAnimationsOffset;
                            int MatrixOffset = BitConverter.ToInt32(rawBytes, thisKeyframedAnimationsOffset + 12) + thisKeyframedAnimationsOffset;

                            animation.Nodes[i].AnimationComponents[a].Keyframes = ParseKeyframes(IndexListOffset, MatrixOffset, keyframeCount, indexSize, floatSize);

                            keyframedAnimationsOffset += 4;
                        }
                    }

                    nodeOffset += 4;
                    
                }
            }

            animation.Name = Utils.GetString(bytes, nameOffset);
            animation.IndexNumeric = animIndex;

            return animation;
        }

        private void ValidateFloatAndIntPrecision(byte indexSize, byte floatSize)
        {
            if(!Enum.IsDefined(typeof(EAN_Animation.IntPrecision), (int)indexSize))
            {
                throw new Exception(String.Format("Parse failed. indexSize = {0} has not been defined.", indexSize));
            }

            if (!Enum.IsDefined(typeof(EAN_Animation.FloatPrecision), (int)floatSize))
            {
                throw new Exception(String.Format("Parse failed. floatSize = {0} has not been defined.", floatSize));
            }
        }

        private ObservableCollection<EAN_Keyframe> ParseKeyframes(int indexOffset, int floatOffset, int count, int indexSize, int floatSize)
        {
            ObservableCollection<EAN_Keyframe> keyframes = new ObservableCollection<EAN_Keyframe>();

            for(int i = 0; i < count; i++)
            {
                keyframes.Add(new EAN_Keyframe());

                //Index
                if(indexSize == 0)
                {
                    keyframes[i].FrameIndex = rawBytes[indexOffset];
                    indexOffset += 1;
                }
                else if(indexSize == 1)
                {
                    keyframes[i].FrameIndex = BitConverter.ToUInt16(rawBytes, indexOffset);
                    indexOffset += 2;
                }

                //Floats
                if (floatSize == 1)
                {
                    keyframes[i].X = Half.ToHalf(rawBytes, floatOffset + 0);
                    keyframes[i].Y = Half.ToHalf(rawBytes, floatOffset + 2);
                    keyframes[i].Z = Half.ToHalf(rawBytes, floatOffset + 4);
                    keyframes[i].W = Half.ToHalf(rawBytes, floatOffset + 6);
                    floatOffset += 8;
                }
                else if (floatSize == 2)
                {
                    keyframes[i].X = BitConverter.ToSingle(rawBytes, floatOffset + 0);
                    keyframes[i].Y = BitConverter.ToSingle(rawBytes, floatOffset + 4);
                    keyframes[i].Z = BitConverter.ToSingle(rawBytes, floatOffset + 8);
                    keyframes[i].W = BitConverter.ToSingle(rawBytes, floatOffset + 12);
                    floatOffset += 16;
                }

            }

            return keyframes;
        }

        private string GetBoneName(short boneIndex, int animIndex, List<ESK_BoneNonHierarchal> bonesNonHierarchal)
        {
            try
            {
                return bonesNonHierarchal[boneIndex].Name;
            }
            catch
            {
                throw new Exception(String.Format("Parse failed. The internal skeleton does not have a bone at index {0}, which the animation at index {1} is referencing!", boneIndex, animIndex));
            }
        }

        //Skeleton

        private void ParseSkeleton(int offset)
        {
            //Init
            boneCount = BitConverter.ToInt16(rawBytes, offset);
            int unk1Offset = BitConverter.ToInt32(rawBytes, offset + 20) + offset;
            int unk2Offset = BitConverter.ToInt32(rawBytes, offset + 24) + offset;

            //Skeleton init
            eanFile.Skeleton = new ESK_Skeleton()
            {
                I_02 = BitConverter.ToInt16(rawBytes, offset + 2),
                I_28 = BitConverter_Ex.ToInt32Array(rawBytes, offset + 28, 2),
                Unk1 = ESK_Unk1.Read(rawBytes, unk1Offset),
                UseUnk2 = (unk2Offset != 0) ? true : false,
                ESKBones = new ObservableCollection<ESK_Bone>()
            };

            //Setting the offsets for the initial loop to use
            int[] offsets = GetBoneOffset(0, offset);
            int boneIndexOffset = offsets[0];
            int nameOffset = offsets[1];
            int skinningMatrixOffset = offsets[2];
            int transformMatrixOffset = offsets[3];

            while (true)
            {
                int idx = eanFile.Skeleton.ESKBones.Count;
                eanFile.Skeleton.ESKBones.Add(ESK_Bone.Read(bytes, rawBytes, offsets));
                if (BitConverter.ToInt16(rawBytes, boneIndexOffset + 2) != -1)
                {
                    eanFile.Skeleton.ESKBones[idx].ESK_Bones = ParseChildrenBones(BitConverter.ToInt16(rawBytes, boneIndexOffset + 2), offset);
                }

                //Loop management
                if (BitConverter.ToInt16(rawBytes, boneIndexOffset + 4) != -1)
                {
                    //There is a sibling
                    offsets = GetBoneOffset(BitConverter.ToInt16(rawBytes, boneIndexOffset + 4), offset);
                    boneIndexOffset = offsets[0];
                    nameOffset = offsets[1];
                    skinningMatrixOffset = offsets[2];
                    //transformMatrixOffset = offsets[3];
                }
                else
                {
                    //There is no sibling. End loop.
                    break;
                }
            }

        }


        private ObservableCollection<ESK_Bone> ParseChildrenBones(int indexOfFirstSibling, int offset)
        {
            ObservableCollection<ESK_Bone> newBones = new ObservableCollection<ESK_Bone>();

            int[] offsets = GetBoneOffset(indexOfFirstSibling, offset);
            int boneIndexOffset = offsets[0];
            int nameOffset = offsets[1];
            int skinningMatrixOffset = offsets[2];
            int transformMatrixOffset = offsets[3];

            while (true)
            {
                int idx = newBones.Count;
                newBones.Add(ESK_Bone.Read(bytes, rawBytes, offsets));
                if (BitConverter.ToInt16(rawBytes, boneIndexOffset + 2) != -1)
                {
                    newBones[idx].ESK_Bones = ParseChildrenBones(BitConverter.ToInt16(rawBytes, boneIndexOffset + 2), offset);
                }

                //Loop management
                if (BitConverter.ToInt16(rawBytes, boneIndexOffset + 4) != -1)
                {
                    //There is a sibling
                    offsets = GetBoneOffset(BitConverter.ToInt16(rawBytes, boneIndexOffset + 4), offset);
                    boneIndexOffset = offsets[0];
                    nameOffset = offsets[1];
                    skinningMatrixOffset = offsets[2];
                    //transformMatrixOffset = offsets[3];
                }
                else
                {
                    //There is no sibling. End loop.
                    break;
                }
            }

            return newBones;
        }

        //Helper methods below

        /// <summary>
        /// Returns the offsets for the bone indexes[0], name[1], Skinning Matrix[2], and Transform Matrix[3], in that order.
        /// </summary>
        private int[] GetBoneOffset(int index, int SkeletonOffset)
        {
            if (BitConverter.ToInt16(rawBytes, SkeletonOffset + 0) - 1 < index)
            {
                throw new Exception("BoneIndex is greater than BoneCount.");
            }

            int boneIndexTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 4) + SkeletonOffset;
            int nameTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 8) + SkeletonOffset;
            int skinningMatrixTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 12) + SkeletonOffset;
            int transformMatrixTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 16) + SkeletonOffset;

            //Calc offsets
            int boneIndex = (8 * index) + boneIndexTableOffset;
            int nameTable = BitConverter.ToInt32(rawBytes, (4 * index) + nameTableOffset) + SkeletonOffset; //Points to the actual string, not the table
            int skinningMatrix = (48 * index) + skinningMatrixTableOffset;
            int transformMatrix = (64 * index) + transformMatrixTableOffset;

            return new int[4] { boneIndex, nameTable, skinningMatrix, transformMatrix };
        }


        private void ValidateSkeletonUnk2(int offset, int count)
        {

            for (int i = 0; i < count; i++)
            {
                if (BitConverter.ToInt64(rawBytes, offset) != 281470681743360)
                {
                    throw new Exception("Parse failed. Skeleton Unk2 Mismatch!");
                }
                offset += 8;
            }
        }


    }

}
