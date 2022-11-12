using System;
using System.Linq;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;
using static Xv2CoreLib.ECF.ECF_Node;

namespace Xv2CoreLib.ECF
{
    public class Parser
    {
        byte[] rawBytes;
        ECF_File ecfFile = new ECF_File();

        //info
        int NodeCount;
        int NodeOffset;


        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            Parse();
        }

        public ECF_File GetEcfFile()
        {
            return ecfFile;
        }

        private void Parse()
        {
            NodeCount = BitConverter.ToInt16(rawBytes, 26);
            NodeOffset = BitConverter.ToInt32(rawBytes, 28);
            ecfFile.I_12 = BitConverter.ToUInt16(rawBytes, 12);

            if (NodeCount > 0)
            {
                for (int i = 0; i < NodeCount; i++)
                {
                    ecfFile.Nodes.Add(new ECF_Node());

                    NodeTypeEnum nodeType = (NodeTypeEnum)BitConverter.ToInt16(rawBytes, NodeOffset + 52);
                    ecfFile.Nodes[i].Loop = nodeType == NodeTypeEnum.AllMaterials_Loop || nodeType == NodeTypeEnum.UseMaterialName_Loop;
                    ecfFile.Nodes[i].UseMaterial = nodeType == NodeTypeEnum.UseMaterialName_Loop || nodeType == NodeTypeEnum.UseMaterialName_NoLoop;

                    ecfFile.Nodes[i].MultiColor.Constant.R = BitConverter.ToSingle(rawBytes, NodeOffset + 0);
                    ecfFile.Nodes[i].MultiColor.Constant.G = BitConverter.ToSingle(rawBytes, NodeOffset + 4);
                    ecfFile.Nodes[i].MultiColor.Constant.B = BitConverter.ToSingle(rawBytes, NodeOffset + 8);
                    ecfFile.Nodes[i].MultiColor_Transparency.Constant = BitConverter.ToSingle(rawBytes, NodeOffset + 12);
                    ecfFile.Nodes[i].RimColor.Constant.R = BitConverter.ToSingle(rawBytes, NodeOffset + 16);
                    ecfFile.Nodes[i].RimColor.Constant.G = BitConverter.ToSingle(rawBytes, NodeOffset + 20);
                    ecfFile.Nodes[i].RimColor.Constant.B = BitConverter.ToSingle(rawBytes, NodeOffset + 24);
                    ecfFile.Nodes[i].RimColor_Transparency.Constant = BitConverter.ToSingle(rawBytes, NodeOffset + 28);
                    ecfFile.Nodes[i].AddColor.Constant.R = BitConverter.ToSingle(rawBytes, NodeOffset + 32);
                    ecfFile.Nodes[i].AddColor.Constant.G = BitConverter.ToSingle(rawBytes, NodeOffset + 36);
                    ecfFile.Nodes[i].AddColor.Constant.B = BitConverter.ToSingle(rawBytes, NodeOffset + 40);
                    ecfFile.Nodes[i].AddColor_Transparency.Constant = BitConverter.ToSingle(rawBytes, NodeOffset + 44);
                    ecfFile.Nodes[i].BlendingFactor.Constant = BitConverter.ToSingle(rawBytes, NodeOffset + 48);
                    ecfFile.Nodes[i].I_54 = BitConverter.ToUInt16(rawBytes, NodeOffset + 54);
                    ecfFile.Nodes[i].StartTime = BitConverter.ToUInt16(rawBytes, NodeOffset + 56);
                    ecfFile.Nodes[i].EndTime = BitConverter.ToUInt16(rawBytes, NodeOffset + 58);
                    ecfFile.Nodes[i].I_60 = BitConverter.ToUInt16(rawBytes, NodeOffset + 60);
                    ecfFile.Nodes[i].I_62 = BitConverter.ToUInt16(rawBytes, NodeOffset + 62);
                    ecfFile.Nodes[i].I_64 = BitConverter.ToUInt64(rawBytes, NodeOffset + 64);
                    ecfFile.Nodes[i].I_72 = BitConverter.ToUInt64(rawBytes, NodeOffset + 72);
                    ecfFile.Nodes[i].I_80 = BitConverter.ToUInt64(rawBytes, NodeOffset + 80);
                    ecfFile.Nodes[i].I_88 = BitConverter.ToInt32(rawBytes, NodeOffset + 88);
                    ecfFile.Nodes[i].I_96 = BitConverter.ToUInt16(rawBytes, NodeOffset + 96);

                    //Animations
                    int keyframedValuesOffset = BitConverter.ToInt32(rawBytes, NodeOffset + 100) + 96 + NodeOffset;
                    int keyframedValuesCount = BitConverter.ToInt16(rawBytes, NodeOffset + 98);

                    if (keyframedValuesCount > 0)
                    {
                        for (int a = 0; a < keyframedValuesCount; a++)
                        {
                            int startOffset = BitConverter.ToInt32(rawBytes, keyframedValuesOffset + 8) + keyframedValuesOffset;
                            int floatOffset = BitConverter.ToInt32(rawBytes, keyframedValuesOffset + 12) + keyframedValuesOffset;
                            bool loop = (rawBytes[keyframedValuesOffset + 3] == 0) ? false : true;
                            //bool interpolate = BitConverter_Ex.ToBoolean(Int4Converter.ToInt4(rawBytes[keyframedValuesOffset + 1])[1]);
                            bool interpolate = (rawBytes[keyframedValuesOffset + 2] == 0) ? false : true;
                            ushort duration = BitConverter.ToUInt16(rawBytes, keyframedValuesOffset + 4);

                            ecfFile.Nodes[i].KeyframedValues.Add(new EMP_KeyframedValue()
                            {
                                Parameter = rawBytes[keyframedValuesOffset + 0],
                                Component = Int4Converter.ToInt4(rawBytes[keyframedValuesOffset + 1])[0],
                                Interpolate = interpolate,
                                Loop = loop,
                                I_03 = Int4Converter.ToInt4(rawBytes[keyframedValuesOffset + 1])[1],
                                Keyframes = ParseKeyframes(BitConverter.ToInt16(rawBytes, keyframedValuesOffset + 6), startOffset, floatOffset, duration, interpolate, loop)
                            });

                            keyframedValuesOffset += 16;
                        }
                    }

                    //Material
                    int materialNameOffset = BitConverter.ToInt32(rawBytes, NodeOffset + 92);

                    //There is only one ECF file in the game with more than 1 entry (currently) and it has a werid offset for the second material name. A negative number that adds up to 0 when adding the current entry position, supposed to point to a string just after the entry.
                    if(materialNameOffset < 0)
                    {
                        materialNameOffset = 104;
                    } 

                    if (materialNameOffset != 0)
                    {
                        ecfFile.Nodes[i].Material = StringEx.GetString(rawBytes, materialNameOffset + NodeOffset, false);
                    }
                    else
                    {
                        ecfFile.Nodes[i].Material = string.Empty;
                    }

                    if (EffectContainer.EepkToolInterlop.FullDecompile)
                    {
                        ecfFile.Nodes[i].MultiColor.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(0, 0, 1, 2));
                        ecfFile.Nodes[i].RimColor.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(1, 0, 1, 2));
                        ecfFile.Nodes[i].AddColor.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(2, 0, 1, 2));
                        ecfFile.Nodes[i].MultiColor_Transparency.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(0, 3));
                        ecfFile.Nodes[i].RimColor_Transparency.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(1, 3));
                        ecfFile.Nodes[i].AddColor_Transparency.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(2, 3));
                        ecfFile.Nodes[i].BlendingFactor.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(3, 0));
                    }

                    NodeOffset += 104;
                }

            }
        }

        private AsyncObservableCollection<EMP_Keyframe> ParseKeyframes(int keyframeCount, int keyframeListOffset, int floatOffset, ushort duration, bool interpolate, bool loop)
        {
            AsyncObservableCollection<EMP_Keyframe> keyframes = new AsyncObservableCollection<EMP_Keyframe>();

            for (int i = 0; i < keyframeCount; i++)
            {
                keyframes.Add(new EMP_Keyframe()
                {
                    Time = BitConverter.ToUInt16(rawBytes, keyframeListOffset),
                    Value = BitConverter.ToSingle(rawBytes, floatOffset)
                });
                keyframeListOffset += 2;
                floatOffset += 4;
            }

            if (duration == 0) return keyframes;

            if (EffectContainer.EepkToolInterlop.FullDecompile && loop && duration != 0)
            {
                //Ensure that a keyframe exists at the end frame. If there is none, an interpolated one will be added
                EMP_Keyframe endKeyframe = keyframes.FirstOrDefault(x => x.Time == duration - 1);

                if (endKeyframe == null)
                {
                    keyframes.Add(new EMP_Keyframe()
                    {
                        Time = (ushort)(duration - 1),
                        Value = EMP_Keyframe.GetInterpolatedKeyframe(keyframes, duration - 1, interpolate)
                    });
                }

                //Delete all keyframes beyond the declared duration
                for (int i = keyframes.Count - 1; i >= 0; i--)
                {
                    if (keyframes[i].Time >= duration)
                    {
                        if (loop && duration < 101 && keyframes[i].Time == duration - 1)
                        {
                            //Special case: A non-looped animation with a duration less than a particles life will default to the last keyframe once the animation finishes, not the last keyframe within the duration.
                            //To account for this, this last keyframe should be kept on the animation and inserted on the time directly after the previous keyframe (of the delcared duration).
                            keyframes[i].Time = duration;
                        }
                        else
                        {
                            keyframes.RemoveAt(i);
                        }
                    }
                }
            }

            return keyframes;
        }

    }
}
