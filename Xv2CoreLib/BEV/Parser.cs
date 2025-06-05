using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.BEV
{
    public class Parser
    {
        public List<int> UsedValues = new List<int>();
        string saveLocation;
        byte[] rawBytes;
        public BEV_File bevFile { get; private set; } = new BEV_File();
        
        public Parser(string location, bool writeXml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            bevFile.Entries = new List<Entry>();
            Parse();
            if (writeXml)
            {
                SaveXmlFile();
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            bevFile.Entries = new List<Entry>();
            Parse();
        }


        void Parse() 
        {
            int entryCount = BitConverter.ToInt32(rawBytes, 8);
            int entryOffset = BitConverter.ToInt32(rawBytes, 12);

            for (int i = 0; i < entryCount; i++)
            {
                bevFile.Entries.Add(new Entry()
                {
                    I_00 = BitConverter.ToInt32(rawBytes, entryOffset + 0),
                    Index = BitConverter.ToInt32(rawBytes, entryOffset + 4).ToString(),
                    I_08 = BitConverter.ToInt32(rawBytes, entryOffset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, entryOffset + 12),
                    I_24 = BitConverter.ToInt32(rawBytes, entryOffset + 24)
                });

                int count = BitConverter.ToInt32(rawBytes, entryOffset + 16);
                int offset = BitConverter.ToInt32(rawBytes, entryOffset + 20);

                //Idx
                int type0 = 0;
                int type1 = 0;
                int type2 = 0;
                int type3 = 0;
                int type4 = 0;
                int type5 = 0;
                int type6 = 0;

                if (count > 0)
                {
                    for (int a = 0; a < count; a++)
                    {
                        
                        short type = BitConverter.ToInt16(rawBytes, offset);
                        short typeCount = BitConverter.ToInt16(rawBytes, offset + 2);
                        int typeoffset = BitConverter.ToInt32(rawBytes, offset + 4);

                        switch (type)
                        {
                            case 0:
                                bevFile.Entries[i].Type0 = GetType0(typeCount, typeoffset, bevFile.Entries[i].Type0, type0);
                                type0++;
                                break;
                            case 1:
                                bevFile.Entries[i].Type1 = GetType1(typeCount, typeoffset, bevFile.Entries[i].Type1, type1);
                                type1++;
                                break;
                            case 2:
                                bevFile.Entries[i].Type2 = GetType2(typeCount, typeoffset, bevFile.Entries[i].Type2, type2);
                                type2++;
                                break;
                            case 3:
                                bevFile.Entries[i].Type3 = GetType3(typeCount, typeoffset, bevFile.Entries[i].Type3, type3);
                                type3++;
                                break;
                            case 4:
                                bevFile.Entries[i].Type4 = GetType4(typeCount, typeoffset, bevFile.Entries[i].Type4, type4);
                                type4++;
                                break;
                            case 5:
                                bevFile.Entries[i].Type5 = GetType5(typeCount, typeoffset, bevFile.Entries[i].Type5, type5);
                                type5++;
                                break;
                            case 6:
                                bevFile.Entries[i].Type6 = GetType6(typeCount, typeoffset, bevFile.Entries[i].Type6, type6);
                                type6++;
                                break;
                            case 7:
                                if(typeCount != 0 || typeoffset != 0)
                                {
                                    goto default;
                                }
                                break;
                            default:
                                Console.WriteLine(String.Format("Encountered undefined BEV_Type = {0} (offset = {1}). Unable to continue.", type, offset));
                                Utils.WaitForInputThenQuit();
                                break;

                        }
                        offset += 8;
                    }
                }

                entryOffset += 28;
            }

        }
        
        void SaveXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(BEV_File));
            serializer.SerializeToFile(bevFile, saveLocation + ".xml");
        }

        //Types below

        List<Type_0> GetType0(short count, int offset, List<Type_0> Type0, int idx)
        {
            if(Type0 == null)
            {
                Type0 = new List<Type_0>();
            }

            for (int i = 0; i < count; i++)
            {
                //if(BitConverter.ToUInt16(rawBytes, offset + 12) == 7)
                //{
                    UsedValues.Add(BitConverter.ToUInt16(rawBytes, offset + 10));
                //}

                Type0.Add(new Type_0()
                {
                    Idx = idx,
                    I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                    I_02 = (ushort)(BitConverter.ToUInt16(rawBytes, offset + 2) - BitConverter.ToUInt16(rawBytes, offset + 0)),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                    I_20 = Convert.ToBoolean(BitConverter.ToInt16(rawBytes, offset + 20)),
                    I_22 = BitConverter.ToUInt16(rawBytes, offset + 22),
                    I_24 = BitConverter.ToUInt16(rawBytes, offset + 24),
                    I_26 = BitConverter.ToUInt16(rawBytes, offset + 26),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                    F_48 = BitConverter.ToSingle(rawBytes, offset + 48)
                });
                offset += 52;
            }

            return Type0;
        }

        List<Type_1> GetType1(short count, int offset, List<Type_1> _type, int idx)
        {
            if(_type == null)
            {
                _type = new List<Type_1>();
            }

            for (int i = 0; i < count; i++)
            {
                _type.Add(new Type_1()
                {
                    Idx = idx,
                    I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                    I_02 = (ushort)(BitConverter.ToUInt16(rawBytes, offset + 2) - BitConverter.ToUInt16(rawBytes, offset + 0)),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                    F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                    F_52 = BitConverter.ToSingle(rawBytes, offset + 52),
                    F_56 = BitConverter.ToSingle(rawBytes, offset + 56),
                    F_60 = BitConverter.ToSingle(rawBytes, offset + 60),
                    F_64 = BitConverter.ToSingle(rawBytes, offset + 64),
                    F_68 = BitConverter.ToSingle(rawBytes, offset + 68),
                    F_72 = BitConverter.ToSingle(rawBytes, offset + 72),
                    F_76 = BitConverter.ToSingle(rawBytes, offset + 76),
                });

                offset += 80;

            }

            return _type;
        }

        List<Type_2> GetType2(short count, int offset, List<Type_2> Type2, int idx)
        {
            if (Type2 == null)
            {
                Type2 = new List<Type_2>();
            }

            for (int i = 0; i < count; i++)
            {
                Type2.Add(new Type_2()
                {
                    Idx = idx,
                    I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                    I_02 = (ushort)(BitConverter.ToUInt16(rawBytes, offset + 2) - BitConverter.ToUInt16(rawBytes, offset + 0)),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44)
                });

                offset += 48;
            }

            return Type2;
        }

        List<Type_3> GetType3(short count, int offset, List<Type_3> Type3, int idx)
        {
            if (Type3 == null)
            {
                Type3 = new List<Type_3>();
            }

            for (int i = 0; i < count; i++)
            {
                Type3.Add(new Type_3()
                {
                    Idx = idx,
                    I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                    I_02 = (ushort)(BitConverter.ToUInt16(rawBytes, offset + 2) - BitConverter.ToUInt16(rawBytes, offset + 0)),
                    I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                });

                offset += 12;
            }

            return Type3;
        }

        List<Type_4> GetType4(short count, int offset, List<Type_4> Type4, int idx)
        {
            if (Type4 == null)
            {
                Type4 = new List<Type_4>();
            }

            for (int i = 0; i < count; i++)
            {
                Type4.Add(new Type_4()
                {
                    Idx = idx,
                    I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                    I_02 = (ushort)(BitConverter.ToUInt16(rawBytes, offset + 2) - BitConverter.ToUInt16(rawBytes, offset + 0)),
                    I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                });
                

                offset += 20;
            }

            return Type4;
        }

        List<Type_5> GetType5(short count, int offset, List<Type_5> Type5, int idx)
        {
            if (Type5 == null)
            {
                Type5 = new List<Type_5>();
            }

            for (int i = 0; i < count; i++)
            {
                Type5.Add(new Type_5()
                {
                    Idx = idx,
                    I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                    I_02 = (ushort)(BitConverter.ToUInt16(rawBytes, offset + 2) - BitConverter.ToUInt16(rawBytes, offset + 0)),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter_Ex.ToInt32Array(rawBytes, offset + 16, 12)
                });

                offset += 64;
            }

            return Type5;
        }

        List<Type_6> GetType6(short count, int offset, List<Type_6> _type, int idx)
        {
            if (_type == null)
            {
                _type = new List<Type_6>();
            }

            for (int i = 0; i < count; i++)
            {
                _type.Add(new Type_6()
                {
                    Idx = idx,
                    I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                    I_02 = (ushort)(BitConverter.ToUInt16(rawBytes, offset + 2) - BitConverter.ToUInt16(rawBytes, offset + 0)),
                    I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                    F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                    F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                    I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                });

                offset += 48;

            }

            return _type;
        }


    }
}
