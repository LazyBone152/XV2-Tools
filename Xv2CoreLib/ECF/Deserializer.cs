using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xv2CoreLib.EMP_NEW;

namespace Xv2CoreLib.ECF
{
    public class Deserializer
    {
        ECF_File ecfFile;
        public List<byte> bytes = new List<byte>() { 35, 69, 67, 70, 254, 255, 32, 00 };


        public Deserializer(ECF_File _ecfFile)
        {
            ecfFile = _ecfFile;
            Write();
        }

        private void Write()
        {
            //offsets
            List<int> StrOffsets = new List<int>();
            List<string> StrToWrite = new List<string>();
            List<int> Type0_Offsets = new List<int>();

            bytes.AddRange(BitConverter.GetBytes((ushort)37568));
            bytes.AddRange(BitConverter.GetBytes((ushort)65535));
            bytes.AddRange(BitConverter.GetBytes(ecfFile.I_12));
            bytes.AddRange(new byte[12]);

            if (ecfFile.Nodes != null)
            {
                bytes.AddRange(BitConverter.GetBytes((short)ecfFile.Nodes.Count));
                bytes.AddRange(BitConverter.GetBytes(32));

                for (int i = 0; i < ecfFile.Nodes.Count; i++)
                {
                    if (EffectContainer.EepkToolInterlop.FullDecompile)
                    {
                        ecfFile.Nodes[i].CompileAllKeyframes();
                    }

                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].MultiColor.Constant.R));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].MultiColor.Constant.G));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].MultiColor.Constant.B));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].DiffuseColor_Transparency.Constant));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].RimColor.Constant.R));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].RimColor.Constant.G));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].RimColor.Constant.B));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].SpecularColor_Transparency.Constant));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].AmbientColor.Constant.R));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].AmbientColor.Constant.G));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].AmbientColor.Constant.B));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].AmbientColor_Transparency.Constant));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].BlendingFactor.Constant));

                    bytes.AddRange(BitConverter.GetBytes((ushort)ecfFile.Nodes[i].LoopMode));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].I_54));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].EndTime));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].I_60));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].I_62));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].I_64));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].I_72));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].I_80));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].I_88));


                    if (!string.IsNullOrWhiteSpace(ecfFile.Nodes[i].Material))
                    {
                        StrOffsets.Add(bytes.Count);
                        StrToWrite.Add(ecfFile.Nodes[i].Material);
                    }

                    bytes.AddRange(new byte[4]);

                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Nodes[i].I_96));
                    if (ecfFile.Nodes[i].KeyframedValues != null)
                    {
                        bytes.AddRange(BitConverter.GetBytes((short)ecfFile.Nodes[i].KeyframedValues.Count));
                        Type0_Offsets.Add(bytes.Count);
                        bytes.AddRange(BitConverter.GetBytes(8));
                    }
                    else
                    {
                        Type0_Offsets.Add(bytes.Count);
                        bytes.AddRange(new byte[6]);
                    }
                }


                //Writing Keyframed Values
                for (int i = 0; i < ecfFile.Nodes.Count; i++)
                {
                    if (ecfFile.Nodes[i].KeyframedValues != null)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - Type0_Offsets[i] + 4), Type0_Offsets[i]);

                        List<int> Type0EntryOffsets = new List<int>();

                        foreach (var e in ecfFile.Nodes[i].KeyframedValues)
                        {
                            int I_01_b = (e.Interpolate == true) ? 1 : 0;
                            int I_02 = (e.Loop == true) ? 1 : 0;
                            bytes.AddRange(new byte[4] { e.Parameter, Int4Converter.GetByte(e.Component, (byte)I_01_b, "Animation: Component", "Animation: Interpolated"), (byte)I_02, e.I_03 });
                            bytes.AddRange(BitConverter.GetBytes((ushort)0));
                            bytes.AddRange(BitConverter.GetBytes((short)e.Keyframes.Count));
                            Type0EntryOffsets.Add(bytes.Count);
                            bytes.AddRange(new byte[8]);

                            //Sort keyframes
                            if (e.Keyframes != null)
                            {
                                e.Keyframes = Sorting.SortEntries2(e.Keyframes);
                            }
                        }

                        for (int a = 0; a < ecfFile.Nodes[i].KeyframedValues.Count; a++)
                        {
                            int entryOffset = Type0EntryOffsets[a] - 8;
                            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - entryOffset), Type0EntryOffsets[a]);

                            int floatListOffset = WriteKeyframe(ecfFile.Nodes[i].KeyframedValues[a].Keyframes);

                            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(floatListOffset - entryOffset), Type0EntryOffsets[a] + 4);
                        }
                    }


                }

                //Writing Strings
                for (int i = 0; i < StrToWrite.Count; i++)
                {
                    int entryOffset = StrOffsets[i] - 92;
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - entryOffset), StrOffsets[i]);
                    bytes.AddRange(Encoding.ASCII.GetBytes(StrToWrite[i]));
                    bytes.Add(0);
                }

            }
            else
            {
                bytes.AddRange(new byte[8]);
            }
        }

        private int WriteKeyframe(IList<EMP_Keyframe> keyframes)
        {
            //Determines the size of the keyframe list (adds padding if its not in 32 bit blocks)
            float fCount = keyframes.Count;

            if (Math.Floor(fCount / 2) != fCount / 2)
            {
                fCount += 1f;
            }

            //Writing Keyframes
            for (int i = 0; i < (int)fCount; i++)
            {
                if (i < keyframes.Count())
                {
                    bytes.AddRange(BitConverter.GetBytes(keyframes[i].Time));
                }
                else
                {
                    bytes.AddRange(new byte[2]);
                }
            }

            //Writing Floats
            int floatListOffset = bytes.Count();
            for (int i = 0; i < keyframes.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].Value));
            }

            //Checking to make sure there are more than 1 keyframes (else, no index list)
            bool specialCase_FirstKeyFrameIsNotZero = (keyframes[0].Time == 0) ? false : true;
            if (keyframes.Count > 1)
            {
                //Writing IndexList
                float totalIndex = 0;
                for (int i = 0; i < keyframes.Count(); i++)
                {
                    int thisFrameLength = 0;
                    if (keyframes.Count() - 1 == i)
                    {
                        thisFrameLength = 1;
                    }
                    else if (specialCase_FirstKeyFrameIsNotZero == true && i == 0)
                    {
                        thisFrameLength = keyframes[0].Time;
                        thisFrameLength += keyframes[i + 1].Time - keyframes[i].Time;
                    }
                    else
                    {
                        thisFrameLength = keyframes[i + 1].Time - keyframes[i].Time;
                    }

                    for (int a = 0; a < thisFrameLength; a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes((short)i));
                        totalIndex += 1;
                    }
                }

                //Add padding if needed
                if (Math.Floor(totalIndex / 2) != totalIndex / 2)
                {
                    bytes.AddRange(new byte[2]);
                }
            }

            return floatListOffset;

        }

    }
}
