using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;
using Xv2CoreLib.Resource;
using Xv2CoreLib.ESK;

namespace Xv2CoreLib.EAN
{
    public class Parser
    {
        string saveLocation { get; set; }
        public EAN_File eanFile { private set; get; }
        byte[] rawBytes { get; set; }
        private int boneCount = 0;
        private bool linkEsk = false;


        public Parser(byte[] _rawBytes, bool linkEsk = true)
        {
            rawBytes = _rawBytes;
            eanFile = new EAN_File();
            this.linkEsk = linkEsk;
            Parse();
        }

        public Parser(string location, bool writeXml = false, bool linkEsk = true)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);
            eanFile = new EAN_File();
            this.linkEsk = linkEsk;
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
            eanFile.Skeleton = ESK_Skeleton.Read(rawBytes, SkeletonOffset, false);

            //Animations
            eanFile.Animations = new AsyncObservableCollection<EAN_Animation>();
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

            if(linkEsk)
                eanFile.LinkEskData();
        }

        //Animations
        private EAN_Animation ParseAnimation(int offset, int nameOffset, int animIndex)
        {
            EAN_Animation animation = new EAN_Animation();

            byte indexSize = rawBytes[offset + 2];
            byte floatSize = rawBytes[offset + 3];
            int nodeCount = BitConverter.ToInt32(rawBytes, offset + 8);
            int nodeOffset = BitConverter.ToInt32(rawBytes, offset + 12) + offset;

            //animation header data
            ValidateFloatAndIntPrecision(indexSize, floatSize);
            //var indexValueType = (EAN_Animation.IntPrecision)indexSize;
            animation.FloatSize = (EAN_Animation.FloatPrecision)floatSize;

            if(nodeCount > 0)
            {
                animation.Nodes = AsyncObservableCollection<EAN_Node>.Create();
                for(int i = 0; i < nodeCount; i++)
                {
                    int thisNodeOffset = BitConverter.ToInt32(rawBytes, nodeOffset) + offset;
                    animation.Nodes.Add(new EAN_Node() { BoneName = GetBoneName(BitConverter.ToInt16(rawBytes, thisNodeOffset), animIndex, eanFile.Skeleton.NonRecursiveBones) });
                    int keyframedAnimationsCount = BitConverter.ToInt16(rawBytes, thisNodeOffset + 2);
                    int keyframedAnimationsOffset = BitConverter.ToInt32(rawBytes, thisNodeOffset + 4) + thisNodeOffset;

                    if(keyframedAnimationsCount > 0)
                    {
                        animation.Nodes[i].AnimationComponents = AsyncObservableCollection<EAN_AnimationComponent>.Create();
                        for(int a = 0; a < keyframedAnimationsCount; a++)
                        {
                            int thisKeyframedAnimationsOffset = BitConverter.ToInt32(rawBytes, keyframedAnimationsOffset) + thisNodeOffset;
                            animation.Nodes[i].AnimationComponents.Add(new EAN_AnimationComponent());
                            animation.Nodes[i].AnimationComponents[a].Type = (EAN_AnimationComponent.ComponentType)rawBytes[thisKeyframedAnimationsOffset + 0];
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

            animation.Name = StringEx.GetString(rawBytes, nameOffset);
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

        private AsyncObservableCollection<EAN_Keyframe> ParseKeyframes(int indexOffset, int floatOffset, int count, int indexSize, int floatSize)
        {
            AsyncObservableCollection<EAN_Keyframe> keyframes = AsyncObservableCollection<EAN_Keyframe>.Create();

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

        private string GetBoneName(short boneIndex, int animIndex, List<ESK_Bone> bonesNonHierarchal)
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

    }

}
