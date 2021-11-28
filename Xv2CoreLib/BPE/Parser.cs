using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BPE
{
    public class Parser
    {
        string saveLocation { get; set; }
        private BPE_File bpeFile = new BPE_File();
        byte[] rawBytes { get; set; }
        List<byte> bytes { get; set; }

        public Parser(string location, bool writeXml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);
            bytes = rawBytes.ToList();
            
            ParseBpe();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(BPE_File));
                serializer.SerializeToFile(bpeFile, saveLocation + ".xml");
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            bytes = rawBytes.ToList();
            ParseBpe();
        }

        public BPE_File GetBpeFile()
        {
            return bpeFile;
        }

        private void ParseBpe()
        {
            int count = BitConverter.ToInt16(rawBytes, 18);
            int offset = BitConverter.ToInt32(rawBytes, 20);
            int actualIndex = 0;

            if(count > 0)
            {
                bpeFile.Entries = new List<BPE_Entry>();

                for (int i = 0; i < count; i++)
                {
                    int entryOffset = BitConverter.ToInt32(rawBytes, offset);

                    if (entryOffset != 0)
                    {
                        
                        bpeFile.Entries.Add(new BPE_Entry()
                        {
                            Index = i.ToString(),
                            I_00 = BitConverter.ToInt32(rawBytes, entryOffset + 0),
                            I_04 = BitConverter.ToUInt16(rawBytes, entryOffset + 4),
                            I_06 = BitConverter.ToUInt16(rawBytes, entryOffset + 6),
                            I_08 = BitConverter.ToUInt16(rawBytes, entryOffset + 8),
                        });

                        //SubEntries
                        int subEntryCount = BitConverter.ToInt16(rawBytes, entryOffset + 10);
                        int subEntryOffset = BitConverter.ToInt32(rawBytes, entryOffset + 12) + entryOffset;

                        if(subEntryCount > 0)
                        {
                            bpeFile.Entries[actualIndex].SubEntries = new List<BPE_SubEntry>();

                            for(int a = 0; a < subEntryCount; a++)
                            {
                                bpeFile.Entries[actualIndex].SubEntries.Add(new BPE_SubEntry()
                                {
                                    BpeType = (BpeType)BitConverter.ToInt32(rawBytes, subEntryOffset + 0),
                                    I_04 = BitConverter.ToUInt16(rawBytes, subEntryOffset + 4),
                                    I_06 = BitConverter.ToUInt16(rawBytes, subEntryOffset + 6),
                                    I_08 = BitConverter.ToUInt16(rawBytes, subEntryOffset + 8),
                                    I_10 = BitConverter.ToUInt16(rawBytes, subEntryOffset + 10),
                                    F_12 = BitConverter.ToSingle(rawBytes, subEntryOffset + 12),
                                    F_16 = BitConverter.ToSingle(rawBytes, subEntryOffset + 16),
                                    F_20 = BitConverter.ToSingle(rawBytes, subEntryOffset + 20),
                                    F_24 = BitConverter.ToSingle(rawBytes, subEntryOffset + 24),
                                    F_28 = BitConverter.ToSingle(rawBytes, subEntryOffset + 28),
                                    F_32 = BitConverter.ToSingle(rawBytes, subEntryOffset + 32),
                                    I_36 = BitConverter.ToInt32(rawBytes, subEntryOffset + 36),
                                    I_40 = BitConverter.ToUInt16(rawBytes, subEntryOffset + 40),
                                });

                                //Types
                                int type = BitConverter.ToInt32(rawBytes, subEntryOffset + 0);
                                int typeCount = BitConverter.ToInt16(rawBytes, subEntryOffset + 42);
                                int typeOffset = BitConverter.ToInt32(rawBytes, subEntryOffset + 44) + subEntryOffset;

                                switch (type)
                                {
                                    case 0:
                                        bpeFile.Entries[actualIndex].SubEntries[a].Type0 = ParseType0(typeOffset, typeCount);
                                        break;
                                    case 1:
                                        bpeFile.Entries[actualIndex].SubEntries[a].Type1 = ParseType1(typeOffset, typeCount);
                                        break;
                                    case 2:
                                        bpeFile.Entries[actualIndex].SubEntries[a].Type2 = ParseType2(typeOffset, typeCount);
                                        break;
                                    case 3:
                                        bpeFile.Entries[actualIndex].SubEntries[a].Type3 = ParseType3(typeOffset, typeCount);
                                        break;
                                    case 4:
                                        bpeFile.Entries[actualIndex].SubEntries[a].Type4 = ParseType4(typeOffset, typeCount);
                                        break;
                                    case 5:
                                        bpeFile.Entries[actualIndex].SubEntries[a].Type5 = ParseType5(typeOffset, typeCount);
                                        break;
                                    case 6:
                                        bpeFile.Entries[actualIndex].SubEntries[a].Type6 = ParseType6(typeOffset, typeCount);
                                        break;
                                    case 7:
                                        bpeFile.Entries[actualIndex].SubEntries[a].Type7 = ParseType7(typeOffset, typeCount);
                                        break;
                                    case 8:
                                        bpeFile.Entries[actualIndex].SubEntries[a].Type8 = ParseType8(typeOffset, typeCount);
                                        break;
                                    case 9:
                                        bpeFile.Entries[actualIndex].SubEntries[a].Type9 = ParseType9(typeOffset, typeCount);
                                        break;
                                }

                                subEntryOffset += 48;
                            }

                        }

                        actualIndex++;
                    }

                    offset += 4;
                }
            }
            

        }

        //Type Readers
        private List<BPE_Type0> ParseType0(int offset, int count)
        {
            if(count > 0)
            {
                List<BPE_Type0> type = new List<BPE_Type0>();

                for(int i = 0; i < count; i++)
                {
                    type.Add(new BPE_Type0()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                    });
                    offset += 8;
                }

                return type;

            } else
            {
                return null;
            }

        }

        private List<BPE_Type1> ParseType1(int offset, int count)
        {
            if (count > 0)
            {
                List<BPE_Type1> type = new List<BPE_Type1>();

                for (int i = 0; i < count; i++)
                {
                    type.Add(new BPE_Type1()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                        F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                    });
                    offset += 16;
                }

                return type;

            }
            else
            {
                return null;
            }

        }

        private List<BPE_Type2> ParseType2(int offset, int count)
        {
            if (count > 0)
            {
                List<BPE_Type2> type = new List<BPE_Type2>();

                for (int i = 0; i < count; i++)
                {
                    type.Add(new BPE_Type2()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
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
                        F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                    });
                    offset += 52;
                }

                return type;

            }
            else
            {
                return null;
            }

        }

        private List<BPE_Type3> ParseType3(int offset, int count)
        {
            if (count > 0)
            {
                List<BPE_Type3> type = new List<BPE_Type3>();

                for (int i = 0; i < count; i++)
                {
                    type.Add(new BPE_Type3()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                        F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                        F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                        F_20 = BitConverter.ToSingle(rawBytes, offset + 20)
                    });
                    offset += 24;
                }

                return type;

            }
            else
            {
                return null;
            }

        }

        private List<BPE_Type4> ParseType4(int offset, int count)
        {
            if (count > 0)
            {
                List<BPE_Type4> type = new List<BPE_Type4>();

                for (int i = 0; i < count; i++)
                {
                    type.Add(new BPE_Type4()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        I_04 = BitConverter.ToInt32(rawBytes, offset + 4)
                    });
                    offset += 8;
                }

                return type;

            }
            else
            {
                return null;
            }

        }

        private List<BPE_Type5> ParseType5(int offset, int count)
        {
            if (count > 0)
            {
                List<BPE_Type5> type = new List<BPE_Type5>();

                for (int i = 0; i < count; i++)
                {
                    type.Add(new BPE_Type5()
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
                        F_44 = BitConverter.ToSingle(rawBytes, offset + 44)
                    });
                    offset += 48;
                }

                return type;

            }
            else
            {
                return null;
            }

        }

        private List<BPE_Type6> ParseType6(int offset, int count)
        {
            if (count > 0)
            {
                List<BPE_Type6> type = new List<BPE_Type6>();

                for (int i = 0; i < count; i++)
                {
                    type.Add(new BPE_Type6()
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
                        F_44 = BitConverter.ToSingle(rawBytes, offset + 44)
                    });
                    offset += 48;
                }

                return type;

            }
            else
            {
                return null;
            }

        }

        private List<BPE_Type7> ParseType7(int offset, int count)
        {
            if (count > 0)
            {
                List<BPE_Type7> type = new List<BPE_Type7>();

                for (int i = 0; i < count; i++)
                {
                    type.Add(new BPE_Type7()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        F_04 = BitConverter.ToSingle(rawBytes, offset + 4)
                    });
                    offset += 8;
                }

                return type;

            }
            else
            {
                return null;
            }

        }

        private List<BPE_Type8> ParseType8(int offset, int count)
        {
            if (count > 0)
            {
                List<BPE_Type8> type = new List<BPE_Type8>();

                for (int i = 0; i < count; i++)
                {
                    type.Add(new BPE_Type8()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                        I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                        F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                        F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                        F_24 = BitConverter.ToSingle(rawBytes, offset + 24)
                    });
                    offset += 28;
                }

                return type;

            }
            else
            {
                return null;
            }

        }

        private List<BPE_Type9> ParseType9(int offset, int count)
        {
            if (count > 0)
            {
                List<BPE_Type9> type = new List<BPE_Type9>();

                for (int i = 0; i < count; i++)
                {
                    type.Add(new BPE_Type9()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                        F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                        F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                        F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                        F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                        F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                        F_32 = BitConverter.ToSingle(rawBytes, offset + 32)
                    });
                    offset += 36;
                }

                return type;

            }
            else
            {
                return null;
            }

        }

    }
}
