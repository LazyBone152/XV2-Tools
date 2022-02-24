using System;
using System.Collections.Generic;
using System.IO;
using Xv2CoreLib.Resource;
using YAXLib;
using static Xv2CoreLib.ECF.ECF_Entry;

namespace Xv2CoreLib.ECF
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        ECF_File ecfFile = new ECF_File();

        //info
        int totalMainEntries;
        int mainEntryOffset;

        public Parser(string location, bool writeXml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            totalMainEntries = BitConverter.ToInt16(rawBytes, 26);
            mainEntryOffset = BitConverter.ToInt32(rawBytes, 28);
            Parse();
            
            if (writeXml)
            {
                WriteXmlFile();
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            totalMainEntries = BitConverter.ToInt16(rawBytes, 26);
            mainEntryOffset = BitConverter.ToInt32(rawBytes, 28);
            Parse();
        }

        public ECF_File GetEcfFile()
        {
            return ecfFile;
        }
        
        private void Parse()
        {
            ecfFile.I_12 = BitConverter.ToUInt16(rawBytes, 12);

            if (totalMainEntries > 0)
            {
                ecfFile.Entries = new List<ECF_Entry>();

                for (int i = 0; i < totalMainEntries; i++)
                {
                    ecfFile.Entries.Add(new ECF_Entry());
                    
                    ecfFile.Entries[i].I_52 = (PlayMode)BitConverter.ToInt16(rawBytes, mainEntryOffset + 52);
                    ecfFile.Entries[i].F_00 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 0);
                    ecfFile.Entries[i].F_04 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 4);
                    ecfFile.Entries[i].F_08 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 8);
                    ecfFile.Entries[i].F_12 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 12);
                    ecfFile.Entries[i].F_16 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 16);
                    ecfFile.Entries[i].F_20 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 20);
                    ecfFile.Entries[i].F_24 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 24);
                    ecfFile.Entries[i].F_28 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 28);
                    ecfFile.Entries[i].F_32 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 32);
                    ecfFile.Entries[i].F_36 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 36);
                    ecfFile.Entries[i].F_40 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 40);
                    ecfFile.Entries[i].F_44 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 44);
                    ecfFile.Entries[i].F_48 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 48);
                    ecfFile.Entries[i].I_54 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 54);
                    ecfFile.Entries[i].I_56 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 56);
                    ecfFile.Entries[i].I_58 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 58);
                    ecfFile.Entries[i].I_60 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 60);
                    ecfFile.Entries[i].I_62 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 62);
                    ecfFile.Entries[i].I_64 = new UInt16[14];

                    for (int a = 0; a < 28; a += 2)
                    {
                        ecfFile.Entries[i].I_64[a/2] = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 64 + a);
                    }

                    ecfFile.Entries[i].I_96 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 96);

                    //Type0 data
                    int Type0_Offset = BitConverter.ToInt32(rawBytes, mainEntryOffset + 100) + 96 + mainEntryOffset;
                    int Type0_Count = BitConverter.ToInt16(rawBytes, mainEntryOffset + 98);

                    if (Type0_Count > 0)
                    {
                        ecfFile.Entries[i].Animations = new List<Type0>();

                        for (int a = 0; a < Type0_Count; a++)
                        {
                            int startOffset = BitConverter.ToInt32(rawBytes, Type0_Offset + 8) + Type0_Offset;
                            int floatOffset = BitConverter.ToInt32(rawBytes, Type0_Offset + 12) + Type0_Offset;

                            ecfFile.Entries[i].Animations.Add(new Type0()
                            {
                                Parameter = (ECF.Type0.ParameterEnum)rawBytes[Type0_Offset + 0],
                                Component = Type0.GetComponent((ECF.Type0.ParameterEnum)rawBytes[Type0_Offset + 0], Int4Converter.ToInt4(rawBytes[Type0_Offset + 1])[0]),
                                Interpolated = BitConverter_Ex.ToBoolean(Int4Converter.ToInt4(rawBytes[Type0_Offset + 1])[1]),
                                Loop = (rawBytes[Type0_Offset + 2] == 0)? false : true,
                                I_03 = rawBytes[Type0_Offset + 3],
                                I_04 = BitConverter.ToUInt16(rawBytes, Type0_Offset + 4),
                                Keyframes = ParseKeyframes(BitConverter.ToInt16(rawBytes, Type0_Offset + 6), startOffset, floatOffset)
                            });
                            

                            Type0_Offset += 16;
                        }
                    }

                    //Unk_Str
                    int Str_Offset = BitConverter.ToInt32(rawBytes, mainEntryOffset + 92) + mainEntryOffset;

                    if (Str_Offset != 0)
                    {
                        ecfFile.Entries[i].MaterialLink = StringEx.GetString(rawBytes, Str_Offset);
                    }
                    else
                    {
                        ecfFile.Entries[i].MaterialLink = String.Empty;
                    }

                    mainEntryOffset += 104;

                }

            }
            


        }

        private AsyncObservableCollection<Type0_Keyframe> ParseKeyframes(int keyframeCount, int keyframeListOffset, int floatOffset)
        {
            AsyncObservableCollection<Type0_Keyframe> keyframes = AsyncObservableCollection<Type0_Keyframe>.Create();

            for (int i = 0; i < keyframeCount; i++)
            {
                keyframes.Add(new Type0_Keyframe()
                {
                    Index = BitConverter.ToUInt16(rawBytes, keyframeListOffset),
                    Float = BitConverter.ToSingle(rawBytes, floatOffset)
                });
                keyframeListOffset += 2;
                floatOffset += 4;
            }

            return keyframes;

        }
        
        private void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(ECF_File));
            serializer.SerializeToFile(ecfFile, saveLocation + ".xml");
        }


    }
}
