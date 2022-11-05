using System;
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

                    ecfFile.Nodes[i].LoopMode = (PlayMode)BitConverter.ToInt16(rawBytes, NodeOffset + 52);
                    ecfFile.Nodes[i].DiffuseColor.Constant.R = BitConverter.ToSingle(rawBytes, NodeOffset + 0);
                    ecfFile.Nodes[i].DiffuseColor.Constant.G = BitConverter.ToSingle(rawBytes, NodeOffset + 4);
                    ecfFile.Nodes[i].DiffuseColor.Constant.B = BitConverter.ToSingle(rawBytes, NodeOffset + 8);
                    ecfFile.Nodes[i].DiffuseColor_Transparency.Constant = BitConverter.ToSingle(rawBytes, NodeOffset + 12);
                    ecfFile.Nodes[i].SpecularColor.Constant.R = BitConverter.ToSingle(rawBytes, NodeOffset + 16);
                    ecfFile.Nodes[i].SpecularColor.Constant.G = BitConverter.ToSingle(rawBytes, NodeOffset + 20);
                    ecfFile.Nodes[i].SpecularColor.Constant.B = BitConverter.ToSingle(rawBytes, NodeOffset + 24);
                    ecfFile.Nodes[i].SpecularColor_Transparency.Constant = BitConverter.ToSingle(rawBytes, NodeOffset + 28);
                    ecfFile.Nodes[i].AmbientColor.Constant.R = BitConverter.ToSingle(rawBytes, NodeOffset + 32);
                    ecfFile.Nodes[i].AmbientColor.Constant.G = BitConverter.ToSingle(rawBytes, NodeOffset + 36);
                    ecfFile.Nodes[i].AmbientColor.Constant.B = BitConverter.ToSingle(rawBytes, NodeOffset + 40);
                    ecfFile.Nodes[i].AmbientColor_Transparency.Constant = BitConverter.ToSingle(rawBytes, NodeOffset + 44);
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

                    //Type0 data
                    int keyframedValuesOffset = BitConverter.ToInt32(rawBytes, NodeOffset + 100) + 96 + NodeOffset;
                    int keyframedValuesCount = BitConverter.ToInt16(rawBytes, NodeOffset + 98);

                    if (keyframedValuesCount > 0)
                    {
                        for (int a = 0; a < keyframedValuesCount; a++)
                        {
                            int startOffset = BitConverter.ToInt32(rawBytes, keyframedValuesOffset + 8) + keyframedValuesOffset;
                            int floatOffset = BitConverter.ToInt32(rawBytes, keyframedValuesOffset + 12) + keyframedValuesOffset;

                            ecfFile.Nodes[i].KeyframedValues.Add(new EMP_KeyframedValue()
                            {
                                Parameter = rawBytes[keyframedValuesOffset + 0],
                                Component = Int4Converter.ToInt4(rawBytes[keyframedValuesOffset + 1])[0],
                                Interpolate = BitConverter_Ex.ToBoolean(Int4Converter.ToInt4(rawBytes[keyframedValuesOffset + 1])[1]),
                                Loop = (rawBytes[keyframedValuesOffset + 2] == 0) ? false : true,
                                I_03 = rawBytes[keyframedValuesOffset + 3],
                                Keyframes = ParseKeyframes(BitConverter.ToInt16(rawBytes, keyframedValuesOffset + 6), startOffset, floatOffset)
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
                        ecfFile.Nodes[i].DiffuseColor.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(0, 0, 1, 2));
                        ecfFile.Nodes[i].SpecularColor.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(1, 0, 1, 2));
                        ecfFile.Nodes[i].AmbientColor.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(2, 0, 1, 2));
                        ecfFile.Nodes[i].DiffuseColor_Transparency.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(0, 3));
                        ecfFile.Nodes[i].SpecularColor_Transparency.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(1, 3));
                        ecfFile.Nodes[i].AmbientColor_Transparency.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(2, 3));
                        ecfFile.Nodes[i].BlendingFactor.DecompileKeyframes(ecfFile.Nodes[i].GetKeyframedValues(3, 0));
                    }

                    NodeOffset += 104;
                }

            }
        }

        private AsyncObservableCollection<EMP_Keyframe> ParseKeyframes(int keyframeCount, int keyframeListOffset, int floatOffset)
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

            return keyframes;
        }

    }
}
