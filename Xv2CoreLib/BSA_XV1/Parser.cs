using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BSA_XV1
{
    public class Parser
    {
        string saveLocation { get; set; }
        private BSA_File bsaFile = new BSA_File();
        byte[] rawBytes { get; set; }
        

        public Parser(string location, bool writeXml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);
            
            ParseBsa();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(BSA_File));
                serializer.SerializeToFile(bsaFile, saveLocation + ".xml");
            }
        }

        public BSA_File GetBsaFile()
        {
            return bsaFile;
        }

        private void ParseBsa()
        {
            int count = BitConverter.ToInt16(rawBytes, 18);
            int offset = BitConverter.ToInt32(rawBytes, 20);

            bsaFile.I_08 = BitConverter.ToInt64(rawBytes, 8);
            bsaFile.I_16 = BitConverter.ToInt16(rawBytes, 16);

            if(count > 0)
            {
                bsaFile.BSA_Entries = new List<BSA_Entry>();
            }

            for (int i = 0; i < count; i++)
            {
                int entryOffset = BitConverter.ToInt32(rawBytes, offset);

                if (entryOffset != 0)
                {
                    bsaFile.BSA_Entries.Add(new BSA_Entry()
                    {
                        Index = i,
                        I_00 = BitConverter.ToInt32(rawBytes, entryOffset + 0),
                        I_16_a = Int4Converter.ToInt4(rawBytes[entryOffset + 16])[0],
                        I_16_b = Int4Converter.ToInt4(rawBytes[entryOffset + 16])[1],
                        I_17 = rawBytes[entryOffset + 17],
                        I_18 = BitConverter.ToInt32(rawBytes, entryOffset + 18),
                        I_22 = BitConverter.ToUInt16(rawBytes, entryOffset + 22),
                        I_24 = BitConverter.ToUInt16(rawBytes, entryOffset + 24),
                        I_26 = BitConverter.ToInt16(rawBytes, entryOffset + 26),
                        I_28 = BitConverter.ToInt16(rawBytes, entryOffset + 28),
                        I_30 = BitConverter.ToInt16(rawBytes, entryOffset + 30),
                        I_32 = BitConverter.ToInt16(rawBytes, entryOffset + 32)
                    });
                    int thisEntry = bsaFile.BSA_Entries.Count() - 1;
                    int Unk1Count = BitConverter.ToInt16(rawBytes, entryOffset + 4);
                    int Unk2Count = BitConverter.ToInt16(rawBytes, entryOffset + 6);

                    if (Unk1Count != 0 || Unk2Count != 0)
                    {
                        bsaFile.BSA_Entries[thisEntry].SubEntries = new BSA_SubEntries() {
                            Unk1 = ParseUnk1(BitConverter.ToInt16(rawBytes, entryOffset + 8) + entryOffset, BitConverter.ToInt16(rawBytes, entryOffset + 4)),
                            Unk2 = ParseUnk2(BitConverter.ToInt16(rawBytes, entryOffset + 12) + entryOffset, BitConverter.ToInt16(rawBytes, entryOffset + 6))
                        };

                        

                    }

                    //Types
                    int typesOffset = BitConverter.ToInt16(rawBytes, entryOffset + 36) + entryOffset;
                    int typesCount = BitConverter.ToInt16(rawBytes, entryOffset + 34);

                    if (typesCount > 0)
                    {

                        for (int a = 0; a < typesCount; a++)
                        {
                            int type = BitConverter.ToInt16(rawBytes, typesOffset + 0);
                            int hdrOffset = BitConverter.ToInt32(rawBytes, typesOffset + 8) + typesOffset;
                            int dataOffset = BitConverter.ToInt32(rawBytes, typesOffset + 12) + typesOffset;

                            switch (type)
                            {
                                case 0:
                                    bsaFile.BSA_Entries[thisEntry].Type0 = ParseType0(hdrOffset, dataOffset, BitConverter.ToInt16(rawBytes, typesOffset + 6));
                                    break;
                                case 1:
                                    bsaFile.BSA_Entries[thisEntry].Type1 = ParseType1(hdrOffset, dataOffset, BitConverter.ToInt16(rawBytes, typesOffset + 6));
                                    break;
                                case 2:
                                    bsaFile.BSA_Entries[thisEntry].Type2 = ParseType2(hdrOffset, dataOffset, BitConverter.ToInt16(rawBytes, typesOffset + 6));
                                    break;
                                case 3:
                                    bsaFile.BSA_Entries[thisEntry].Type3 = ParseType3(hdrOffset, dataOffset, BitConverter.ToInt16(rawBytes, typesOffset + 6));
                                    break;
                                case 4:
                                    bsaFile.BSA_Entries[thisEntry].Type4 = ParseType4(hdrOffset, dataOffset, BitConverter.ToInt16(rawBytes, typesOffset + 6));
                                    break;
                                case 6:
                                    bsaFile.BSA_Entries[thisEntry].Type6 = ParseType6(hdrOffset, dataOffset, BitConverter.ToInt16(rawBytes, typesOffset + 6));
                                    break;
                                case 7:
                                    bsaFile.BSA_Entries[thisEntry].Type7 = ParseType7(hdrOffset, dataOffset, BitConverter.ToInt16(rawBytes, typesOffset + 6));
                                    break;
                                case 8:
                                    bsaFile.BSA_Entries[thisEntry].Type8 = ParseType8(hdrOffset, dataOffset, BitConverter.ToInt16(rawBytes, typesOffset + 6));
                                    break;
                                default:
                                    Console.WriteLine(String.Format("Undefined BSA Type encountered: {0}, at offset: {1}", type, typesOffset));
                                    Console.ReadLine();
                                    break;
                            }

                            typesOffset += 16;
                        }

                    }

                }
                offset += 4;
            }

        }
        
        private List<BSA.BSA_Collision> ParseUnk1(int offset, int count)
        {
            if(count > 0)
            {

                List<BSA.BSA_Collision> unk1 = new List<BSA.BSA_Collision>();

                for (int i = 0; i < count; i++)
                {
                    unk1.Add(new BSA.BSA_Collision()
                    {
                        I_00 = (BSA.EepkType)BitConverter.ToUInt16(rawBytes, offset + 0),
                        SkillID = BitConverter.ToUInt16(rawBytes, offset + 2),
                        EffectID = BitConverter.ToUInt16(rawBytes, offset + 4),
                        I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                        I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                        I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                        I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                        I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                    });
                    offset += 24;
                }

                return unk1;
            } else
            {
                return null;
            }
        }

        private List<BSA.BSA_Expiration> ParseUnk2(int offset, int count)
        {

            if(count > 0)
            {
                List<BSA.BSA_Expiration> unk2 = new List<BSA.BSA_Expiration>();

                for (int i = 0; i < count; i++)
                {
                    unk2.Add(new BSA.BSA_Expiration()
                    {
                        I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                        I_06 = BitConverter.ToUInt16(rawBytes, offset + 6)
                    });
                    offset += 8;
                }

                return unk2;
            } else
            {
                return null;
            }
            
        }

        

        //Type Parsers
        private List<BSA.BSA_Type0> ParseType0(int hdrOffset, int offset, int count)
        {
            if(count > 0)
            {
                List<BSA.BSA_Type0> Type0 = new List<BSA.BSA_Type0>();

                for(int i = 0; i < count; i++)
                {
                    Type0.Add(new BSA.BSA_Type0()
                    {
                        I_00 = BitConverter.ToInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                        BSA_EntryID = BitConverter.ToUInt16(rawBytes, offset + 4),
                        I_06 = BitConverter.ToInt16(rawBytes, offset + 6),
                        F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                        StartTime = BitConverter.ToUInt16(rawBytes, hdrOffset + 0),
                        Duration = GetTypeDuration(BitConverter.ToUInt16(rawBytes, hdrOffset + 0), BitConverter.ToUInt16(rawBytes, hdrOffset + 2)),
                    });
                    hdrOffset += 4;
                    offset += 16;
                }

                return Type0;

            } else
            {
                return null;
            }
        }

        private List<BSA_Type1> ParseType1(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA_Type1> Type = new List<BSA_Type1>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA_Type1()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                        F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                        F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                        F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                        F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                        F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                        StartTime = BitConverter.ToUInt16(rawBytes, hdrOffset + 0),
                        Duration = GetTypeDuration(BitConverter.ToUInt16(rawBytes, hdrOffset + 0), BitConverter.ToUInt16(rawBytes, hdrOffset + 2)),
                    });
                    hdrOffset += 4;
                    offset += 32;
                }

                return Type;

            }
            else
            {
                return null;
            }
        }

        private List<BSA.BSA_Type2> ParseType2(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA.BSA_Type2> Type = new List<BSA.BSA_Type2>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA.BSA_Type2()
                    {
                        I_00 = BitConverter.ToInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                        I_06 = BitConverter.ToInt16(rawBytes, offset + 6),
                        StartTime = BitConverter.ToUInt16(rawBytes, hdrOffset + 0),
                        Duration = GetTypeDuration(BitConverter.ToUInt16(rawBytes, hdrOffset + 0), BitConverter.ToUInt16(rawBytes, hdrOffset + 2)),
                    });
                    hdrOffset += 4;
                    offset += 8;
                }

                return Type;

            }
            else
            {
                return null;
            }
        }

        private List<BSA.BSA_Type3> ParseType3(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA.BSA_Type3> Type = new List<BSA.BSA_Type3>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA.BSA_Type3()
                    {
                        I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                        I_06_a = Int4Converter.ToInt4(rawBytes[offset + 6])[0],
                        I_06_b = Int4Converter.ToInt4(rawBytes[offset + 6])[1],
                        I_06_c = Int4Converter.ToInt4(rawBytes[offset + 7])[0],
                        I_06_d = Int4Converter.ToInt4(rawBytes[offset + 7])[1],
                        F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                        F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                        F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                        F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                        F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                        F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                        F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                        F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                        F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                        I_48 = BitConverter.ToUInt16(rawBytes, offset + 48),
                        I_50 = BitConverter.ToUInt16(rawBytes, offset + 50),
                        I_52 = BitConverter.ToUInt16(rawBytes, offset + 52),
                        I_54 = BitConverter.ToUInt16(rawBytes, offset + 54),
                        I_56 = BitConverter.ToUInt16(rawBytes, offset + 56),
                        FirstHit = BitConverter.ToUInt16(rawBytes, offset + 58),
                        MultipleHits = BitConverter.ToUInt16(rawBytes, offset + 60),
                        LastHit = BitConverter.ToUInt16(rawBytes, offset + 62),
                        StartTime = BitConverter.ToUInt16(rawBytes, hdrOffset + 0),
                        Duration = GetTypeDuration(BitConverter.ToUInt16(rawBytes, hdrOffset + 0), BitConverter.ToUInt16(rawBytes, hdrOffset + 2)),
                    });
                    hdrOffset += 4;
                    offset += 64;
                }

                return Type;

            }
            else
            {
                return null;
            }
        }

        private List<BSA.BSA_Type4> ParseType4(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA.BSA_Type4> Type = new List<BSA.BSA_Type4>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA.BSA_Type4()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                        I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                        F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                        F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                        I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                        I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                        I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                        I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                        I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                        I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                        I_48 = BitConverter.ToUInt16(rawBytes, offset + 48),
                        I_50 = BitConverter.ToUInt16(rawBytes, offset + 50),
                        I_52 = BitConverter.ToUInt16(rawBytes, offset + 52),
                        I_54 = BitConverter.ToUInt16(rawBytes, offset + 54),
                        StartTime = BitConverter.ToUInt16(rawBytes, hdrOffset + 0),
                        Duration = GetTypeDuration(BitConverter.ToUInt16(rawBytes, hdrOffset + 0), BitConverter.ToUInt16(rawBytes, hdrOffset + 2)),
                    });
                    hdrOffset += 4;
                    offset += 56;
                }

                return Type;

            }
            else
            {
                return null;
            }
        }

        private List<BSA.BSA_Type6> ParseType6(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA.BSA_Type6> Type = new List<BSA.BSA_Type6>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA.BSA_Type6()
                    {
                        I_00 = (BSA.EepkType)BitConverter.ToUInt16(rawBytes, offset + 0),
                        SkillID = BitConverter.ToUInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                        I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                        I_08 = (BSA.Switch)BitConverter.ToUInt16(rawBytes, offset + 8),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                        F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                        F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                        StartTime = BitConverter.ToUInt16(rawBytes, hdrOffset + 0),
                        Duration = GetTypeDuration(BitConverter.ToUInt16(rawBytes, hdrOffset + 0), BitConverter.ToUInt16(rawBytes, hdrOffset + 2)),
                    });
                    hdrOffset += 4;
                    offset += 24;
                }

                return Type;

            }
            else
            {
                return null;
            }
        }

        private List<BSA.BSA_Type7> ParseType7(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA.BSA_Type7> Type = new List<BSA.BSA_Type7>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA.BSA_Type7()
                    {
                        I_00 = (BSA.AcbType)BitConverter.ToUInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                        I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                        StartTime = BitConverter.ToUInt16(rawBytes, hdrOffset + 0),
                        Duration = GetTypeDuration(BitConverter.ToUInt16(rawBytes, hdrOffset + 0), BitConverter.ToUInt16(rawBytes, hdrOffset + 2)),
                    });
                    hdrOffset += 4;
                    offset += 8;
                }

                return Type;

            }
            else
            {
                return null;
            }
        }

        private List<BSA.BSA_Type8> ParseType8(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA.BSA_Type8> Type = new List<BSA.BSA_Type8>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA.BSA_Type8()
                    {
                        I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                        I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                        I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                        I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                        I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                        StartTime = BitConverter.ToUInt16(rawBytes, hdrOffset + 0),
                        Duration = GetTypeDuration(BitConverter.ToUInt16(rawBytes, hdrOffset + 0), BitConverter.ToUInt16(rawBytes, hdrOffset + 2)),
                    });
                    hdrOffset += 4;
                    offset += 24;
                }

                return Type;

            }
            else
            {
                return null;
            }
        }

        //Utility
        private ushort GetTypeDuration(ushort startTime, ushort endTime)
        {
            int duration = endTime - startTime;
            return (ushort)duration;
        }

    }
}
