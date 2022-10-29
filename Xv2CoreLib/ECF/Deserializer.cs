using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.ECF
{
    public class Deserializer
    {
        string saveLocation;
        ECF_File ecfFile;
        public List<byte> bytes = new List<byte>() { 35, 69, 67, 70, 254, 255, 32, 00 };
        bool writeToDisk = true;

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(ECF_File), YAXSerializationOptions.DontSerializeNullObjects);
            ecfFile = (ECF_File)serializer.DeserializeFromFile(location);
            Write();
        }

        public Deserializer(ECF_File _ecfFile)
        {
            writeToDisk = false;
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

            if (ecfFile.Entries != null)
            {
                bytes.AddRange(BitConverter.GetBytes((short)ecfFile.Entries.Count));
                bytes.AddRange(BitConverter.GetBytes(32));


                for (int i = 0; i < ecfFile.Entries.Count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_00));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_04));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_08));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_20));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_24));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_28));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_32));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_36));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_40));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_44));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].F_48));
                    bytes.AddRange(BitConverter.GetBytes((ushort)ecfFile.Entries[i].I_52));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].I_54));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].I_56));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].I_58));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].I_60));
                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].I_62));

                    foreach (ushort value in ecfFile.Entries[i].I_64)
                    {
                        bytes.AddRange(BitConverter.GetBytes(value));
                    }

                    if (!string.IsNullOrWhiteSpace(ecfFile.Entries[i].MaterialLink))
                    {
                        StrOffsets.Add(bytes.Count);
                        StrToWrite.Add(ecfFile.Entries[i].MaterialLink);
                    }

                    bytes.AddRange(new byte[4]);

                    bytes.AddRange(BitConverter.GetBytes(ecfFile.Entries[i].I_96));
                    if (ecfFile.Entries[i].Animations != null)
                    {
                        bytes.AddRange(BitConverter.GetBytes((short)ecfFile.Entries[i].Animations.Count));
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
                for (int i = 0; i < ecfFile.Entries.Count; i++)
                {
                    if (ecfFile.Entries[i].Animations != null)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - Type0_Offsets[i] + 4), Type0_Offsets[i]);

                        List<int> Type0EntryOffsets = new List<int>();

                        foreach (var e in ecfFile.Entries[i].Animations)
                        {
                            int I_01_b = (e.Interpolated == true) ? 1 : 0;
                            int I_02 = (e.Loop == true) ? 1 : 0;
                            bytes.AddRange(new byte[4] { (byte)e.Parameter, Int4Converter.GetByte((byte)e.GetComponent(), (byte)I_01_b, "Animation: Component", "Animation: Interpolated"), (byte)I_02, e.I_03 });
                            bytes.AddRange(BitConverter.GetBytes(e.I_04));
                            bytes.AddRange(BitConverter.GetBytes((short)e.Keyframes.Count()));
                            Type0EntryOffsets.Add(bytes.Count);
                            bytes.AddRange(new byte[8]);

                            //Sort keyframes
                            if (e.Keyframes != null)
                            {
                                e.Keyframes = Sorting.SortEntries2(e.Keyframes);
                            }
                        }

                        for (int a = 0; a < ecfFile.Entries[i].Animations.Count; a++)
                        {
                            int entryOffset = Type0EntryOffsets[a] - 8;
                            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - entryOffset), Type0EntryOffsets[a]);

                            int floatListOffset = WriteKeyframe(ecfFile.Entries[i].Animations[a].Keyframes);

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

            if (writeToDisk)
            {
                File.WriteAllBytes(saveLocation, bytes.ToArray());
            }
        }

        private int WriteKeyframe(IList<Type0_Keyframe> keyframes)
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
                    bytes.AddRange(BitConverter.GetBytes(keyframes[i].Index));
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
                bytes.AddRange(BitConverter.GetBytes(keyframes[i].Float));
            }

            //Checking to make sure there are more than 1 keyframes (else, no index list)
            bool specialCase_FirstKeyFrameIsNotZero = (keyframes[0].Index == 0) ? false : true;
            if (keyframes.Count() > 1)
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
                        thisFrameLength = keyframes[0].Index;
                        thisFrameLength += keyframes[i + 1].Index - keyframes[i].Index;
                    }
                    else
                    {
                        thisFrameLength = keyframes[i + 1].Index - keyframes[i].Index;
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
