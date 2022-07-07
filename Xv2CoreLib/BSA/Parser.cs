using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BSA
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

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            ParseBsa();
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
            bsaFile.BSA_Entries = new List<BSA_Entry>();
            

            for (int i = 0; i < count; i++)
            {
                int entryOffset = BitConverter.ToInt32(rawBytes, offset);

                if (entryOffset != 0)
                {
                    bsaFile.BSA_Entries.Add(new BSA_Entry()
                    {
                        Index = i.ToString(),
                        I_00 = BitConverter.ToInt32(rawBytes, entryOffset + 0),
                        I_16_a = Int4Converter.ToInt4(rawBytes[entryOffset + 16])[0],
                        I_16_b = Int4Converter.ToInt4(rawBytes[entryOffset + 16])[1],
                        I_17 = rawBytes[entryOffset + 17],
                        I_18 = BitConverter.ToInt32(rawBytes, entryOffset + 18),
                        I_22 = BitConverter.ToUInt16(rawBytes, entryOffset + 22),
                        I_24 = BitConverter.ToUInt16(rawBytes, entryOffset + 24),
                        Expires = BitConverter.ToUInt16(rawBytes, entryOffset + 26),
                        ImpactProjectile = BitConverter.ToUInt16(rawBytes, entryOffset + 28),
                        ImpactEnemy = BitConverter.ToUInt16(rawBytes, entryOffset + 30),
                        ImpactGround = BitConverter.ToUInt16(rawBytes, entryOffset + 32),
                        I_40 = BitConverter_Ex.ToInt32Array(rawBytes, entryOffset + 40, 3)
                    });
                    int thisEntry = bsaFile.BSA_Entries.Count() - 1;
                    int Unk1Count = BitConverter.ToInt16(rawBytes, entryOffset + 4);
                    int Unk2Count = BitConverter.ToInt16(rawBytes, entryOffset + 6);

                    if (Unk1Count != 0 || Unk2Count != 0)
                    {
                        bsaFile.BSA_Entries[thisEntry].SubEntries = new BSA_SubEntries() {
                            CollisionEntries = ParseUnk1(BitConverter.ToInt16(rawBytes, entryOffset + 8) + entryOffset, BitConverter.ToInt16(rawBytes, entryOffset + 4)),
                            ExpirationEntries = ParseUnk2(BitConverter.ToInt16(rawBytes, entryOffset + 12) + entryOffset, BitConverter.ToInt16(rawBytes, entryOffset + 6))
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
                            int typeCount = BitConverter.ToInt16(rawBytes, typesOffset + 6);

                            switch (type)
                            {
                                case 0:
                                    bsaFile.BSA_Entries[thisEntry].Type0 = ParseType0(hdrOffset, dataOffset, typeCount);
                                    break;
                                case 1:
                                    bsaFile.BSA_Entries[thisEntry].Type1 = ParseType1(hdrOffset, dataOffset, typeCount);
                                    break;
                                case 2:
                                    bsaFile.BSA_Entries[thisEntry].Type2 = ParseType2(hdrOffset, dataOffset, typeCount);
                                    break;
                                case 3:
                                    bsaFile.BSA_Entries[thisEntry].Type3 = ParseType3(hdrOffset, dataOffset, typeCount);
                                    break;
                                case 4:
                                    bsaFile.BSA_Entries[thisEntry].Type4 = ParseType4(hdrOffset, dataOffset, typeCount);
                                    break;
                                case 6:
                                    bsaFile.BSA_Entries[thisEntry].Type6 = ParseType6(hdrOffset, dataOffset, typeCount);
                                    break;
                                case 7:
                                    bsaFile.BSA_Entries[thisEntry].Type7 = ParseType7(hdrOffset, dataOffset, typeCount);
                                    break;
                                case 8:
                                    bsaFile.BSA_Entries[thisEntry].Type8 = ParseType8(hdrOffset, dataOffset, typeCount);
                                    break;
                                case 12:
                                    bsaFile.BSA_Entries[thisEntry].Type12 = ParseType12(hdrOffset, dataOffset, typeCount);
                                    break;
                                case 13:
                                    bsaFile.BSA_Entries[thisEntry].Type13 = ParseType13(hdrOffset, dataOffset, typeCount);
                                    break;
                                default:
                                    //Attempt to estimate the unknown type size
                                    int estSize = (a + 1 < typesCount) ? BitConverter.ToInt16(rawBytes, typesOffset + 6 + 16) : -1;

                                    if(estSize == -1 && i + 1 < count)
                                    {
                                        //This is the final type for this BSA Entry. Seek to the next.
                                        estSize = BitConverter.ToInt32(rawBytes, offset + 4) - dataOffset;
                                    }

                                    Console.WriteLine(String.Format("Undefined BSA Type encountered: {0}, at: def offset: {1}, data offset: {2}, count: {3}, estTypeSize: {4}", type, typesOffset, dataOffset, typeCount, estSize));
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
        
        private List<BSA_Collision> ParseUnk1(int offset, int count)
        {
            if(count > 0)
            {

                List<BSA_Collision> unk1 = new List<BSA_Collision>();

                for (int i = 0; i < count; i++)
                {
                    unk1.Add(new BSA_Collision()
                    {
                        EepkType = (EepkType)BitConverter.ToUInt16(rawBytes, offset + 0),
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

        private List<BSA_Expiration> ParseUnk2(int offset, int count)
        {

            if(count > 0)
            {
                List<BSA_Expiration> unk2 = new List<BSA_Expiration>();

                for (int i = 0; i < count; i++)
                {
                    unk2.Add(new BSA_Expiration()
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
        private List<BSA_Type0> ParseType0(int hdrOffset, int offset, int count)
        {
            if(count > 0)
            {
                List<BSA_Type0> Type0 = new List<BSA_Type0>();

                for(int i = 0; i < count; i++)
                {
                    Type0.Add(new BSA_Type0()
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
                        F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                        F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                        F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                        F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                        StartTime = BitConverter.ToUInt16(rawBytes, hdrOffset + 0),
                        Duration = GetTypeDuration(BitConverter.ToUInt16(rawBytes, hdrOffset + 0), BitConverter.ToUInt16(rawBytes, hdrOffset + 2)),
                    });
                    hdrOffset += 4;
                    offset += 48;
                }

                return Type;

            }
            else
            {
                return null;
            }
        }

        private List<BSA_Type2> ParseType2(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA_Type2> Type = new List<BSA_Type2>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA_Type2()
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

        private List<BSA_Type3> ParseType3(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA_Type3> Type = new List<BSA_Type3>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA_Type3()
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

        private List<BSA_Type4> ParseType4(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA_Type4> Type = new List<BSA_Type4>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA_Type4()
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

        private List<BSA_Type6> ParseType6(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA_Type6> Type = new List<BSA_Type6>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA_Type6()
                    {
                        EepkType = (EepkType)BitConverter.ToUInt16(rawBytes, offset + 0),
                        SkillID = BitConverter.ToUInt16(rawBytes, offset + 2),
                        EffectID = BitConverter.ToUInt16(rawBytes, offset + 4),
                        I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                        I_08 = (Switch)BitConverter.ToUInt16(rawBytes, offset + 8),
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

        private List<BSA_Type7> ParseType7(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA_Type7> Type = new List<BSA_Type7>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA_Type7()
                    {
                        AcbType = (AcbType)BitConverter.ToUInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                        CueId = BitConverter.ToUInt16(rawBytes, offset + 4),
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

        private List<BSA_Type8> ParseType8(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA_Type8> Type = new List<BSA_Type8>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA_Type8()
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

        private List<BSA_Type12> ParseType12(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA_Type12> Type = new List<BSA_Type12>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA_Type12()
                    {
                        F_00 = BitConverter.ToSingle(rawBytes, offset + 0),
                        I_04 = (EepkType)BitConverter.ToInt32(rawBytes, offset + 4),
                        I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                        I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                        F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                        StartTime = BitConverter.ToUInt16(rawBytes, hdrOffset + 0),
                        Duration = GetTypeDuration(BitConverter.ToUInt16(rawBytes, hdrOffset + 0), BitConverter.ToUInt16(rawBytes, hdrOffset + 2)),
                    });
                    hdrOffset += 4;
                    offset += 20;
                }

                return Type;

            }
            else
            {
                return null;
            }
        }

        private List<BSA_Type13> ParseType13(int hdrOffset, int offset, int count)
        {
            if (count > 0)
            {
                List<BSA_Type13> Type = new List<BSA_Type13>();

                for (int i = 0; i < count; i++)
                {
                    Type.Add(new BSA_Type13()
                    {
                        F_00 = BitConverter.ToSingle(rawBytes, offset + 0),
                        F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                        F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                        I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                        F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                        I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                        I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                        I_26 = BitConverter.ToInt16(rawBytes, offset + 26),
                        I_28 = BitConverter.ToInt16(rawBytes, offset + 28),
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



        //Utility
        private ushort GetTypeDuration(ushort startTime, ushort endTime)
        {
            int duration = endTime - startTime;
            return (ushort)duration;
        }

    }
}
