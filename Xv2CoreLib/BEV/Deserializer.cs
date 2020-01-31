using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BEV
{
    public class Deserializer
    {
        string saveLocation;
        BEV_File bevFile;
        public List<byte> bytes = new List<byte>() { 35, 65, 69, 86, 254, 255, 0, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            ReadXmlFile(location);
            bevFile.SortEntries();
            WriteFile();
            SaveBinaryFile();
        }

        public Deserializer(BEV_File _bevFile, string location)
        {
            saveLocation = location;
            bevFile = _bevFile;
            bevFile.SortEntries();
            WriteFile();
            SaveBinaryFile();
        }

        public Deserializer(BEV_File _bevFile)
        {
            bevFile = _bevFile;
            bevFile.SortEntries();
            WriteFile();
        }

        void ReadXmlFile(string path)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(BEV_File), YAXSerializationOptions.DontSerializeNullObjects);
            bevFile = (BEV_File)serializer.DeserializeFromFile(path);
        }

        void WriteFile()
        {
            //offsets
            List<int> subEntryPointers = new List<int>(); //pointer in Main Entry to SubEntry (stored here so it can be set later)
            List<int> dataPointers = new List<int>(); //use a self-controlled varible to access this list. Write all pointers offsets to it, and then fill those offsets in. 

            bytes.AddRange(BitConverter.GetBytes(bevFile.Entries.Count()));
            bytes.AddRange(new List<byte> { 32, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            for (int i = 0; i < bevFile.Entries.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(bevFile.Entries[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(bevFile.Entries[i].Index)));
                bytes.AddRange(BitConverter.GetBytes(bevFile.Entries[i].I_08));
                bytes.AddRange(BitConverter.GetBytes(HexConverter.ToInt32(bevFile.Entries[i].I_12)));
                bytes.AddRange(BitConverter.GetBytes(TotalTypeCount(bevFile.Entries[i])));

                subEntryPointers.Add(bytes.Count());
                bytes.AddRange(new byte[4]);

                bytes.AddRange(BitConverter.GetBytes(bevFile.Entries[i].I_24));

            }

            for (int i = 0; i < bevFile.Entries.Count(); i++)
            {

                var _subEntries = GetTypes(bevFile.Entries[i]);

                if(_subEntries.Count > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), subEntryPointers[i]);

                    foreach (var e in _subEntries)
                    {
                        int size = bytes.Count();

                        int typeCount = 0;
                        switch (e._Type)
                        {
                            case 0:
                                typeCount = bevFile.Entries[i].Type0.Count(entry => entry.Idx == e._Idx);
                                break;
                            case 1:
                                typeCount = bevFile.Entries[i].Type1.Count(entry => entry.Idx == e._Idx);
                                break;
                            case 2:
                                typeCount = bevFile.Entries[i].Type2.Count(entry => entry.Idx == e._Idx);
                                break;
                            case 3:
                                typeCount = bevFile.Entries[i].Type3.Count(entry => entry.Idx == e._Idx);
                                break;
                            case 4:
                                typeCount = bevFile.Entries[i].Type4.Count(entry => entry.Idx == e._Idx);
                                break;
                            case 5:
                                typeCount = bevFile.Entries[i].Type5.Count(entry => entry.Idx == e._Idx);
                                break;
                            case 6:
                                typeCount = bevFile.Entries[i].Type6.Count(entry => entry.Idx == e._Idx);
                                break;
                        }

                        bytes.AddRange(BitConverter.GetBytes((short)e._Type));
                        bytes.AddRange(BitConverter.GetBytes((short)typeCount));

                        if (typeCount > 0)
                        {
                            dataPointers.Add(bytes.Count());
                        }
                        else
                        {
                            dataPointers.Add(-1);
                        }
                        bytes.AddRange(new byte[4]);

                        //Entry size validation
                        if(bytes.Count() - size != 8)
                        {
                            Console.WriteLine(String.Format("SubEntry size mismatch!\nExpected = 8\nWas = {0}", bytes.Count() - size));
                            Console.ReadLine();
                        }
                    }
                }
                
            }

            int access = 0;
            for (int i = 0; i < bevFile.Entries.Count(); i++)
            {

                var _subEntries = GetTypes(bevFile.Entries[i]);

                if (_subEntries.Count > 0)
                {
                    foreach(var e in _subEntries)
                    {
                        if (dataPointers[access] != -1)
                        {
                            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), dataPointers[access]);
                        }
                        int size = bytes.Count();

                        switch (e._Type)
                        {
                            case 0:
                                WriteType0(bevFile.Entries[i].Type0, e._Idx);
                                SizeCheck(size, bytes.Count(), 52, 0, bevFile.Entries[i].Type0.Count(entry => entry.Idx == e._Idx));
                                break;
                            case 1:
                                WriteType1(bevFile.Entries[i].Type1, e._Idx);
                                SizeCheck(size, bytes.Count(), 80, 1, bevFile.Entries[i].Type1.Count(entry => entry.Idx == e._Idx));
                                break;
                            case 2:
                                WriteType2(bevFile.Entries[i].Type2, e._Idx);
                                SizeCheck(size, bytes.Count(), 48, 2, bevFile.Entries[i].Type2.Count(entry => entry.Idx == e._Idx));
                                break;
                            case 3:
                                WriteType3(bevFile.Entries[i].Type3, e._Idx);
                                SizeCheck(size, bytes.Count(), 12, 3, bevFile.Entries[i].Type3.Count(entry => entry.Idx == e._Idx));
                                break;
                            case 4:
                                WriteType4(bevFile.Entries[i].Type4, e._Idx);
                                SizeCheck(size, bytes.Count(), 20, 4, bevFile.Entries[i].Type4.Count(entry => entry.Idx == e._Idx));
                                break;
                            case 5:
                                WriteType5(bevFile.Entries[i].Type5, e._Idx);
                                SizeCheck(size, bytes.Count(), 64, 5, bevFile.Entries[i].Type5.Count(entry => entry.Idx == e._Idx));
                                break;
                            case 6:
                                WriteType6(bevFile.Entries[i].Type6, e._Idx);
                                SizeCheck(size, bytes.Count(), 48, 5, bevFile.Entries[i].Type6.Count(entry => entry.Idx == e._Idx));
                                break;
                        }

                        access++;
                    }
                }
            }

        }

        void SaveBinaryFile()
        {
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }


        //Count Methods
        private struct TypeEntry
        {
            public int _Type { get; set; }
            public int _Idx { get; set; }
        }

        private List<TypeEntry> GetTypes(Entry entry)
        {
            List<TypeEntry> _types = new List<TypeEntry>();

            //Type0
            List<int> _type0 = TotalCount(entry.Type0);
            foreach(int e in _type0)
            {
                _types.Add(new TypeEntry()
                {
                    _Idx = e,
                    _Type = 0
                });
            }

            //Type1
            List<int> _type1 = TotalCount(entry.Type1);
            foreach (int e in _type1)
            {
                _types.Add(new TypeEntry()
                {
                    _Idx = e,
                    _Type = 1
                });
            }

            //Type2
            List<int> _type2 = TotalCount(entry.Type2);
            foreach (int e in _type2)
            {
                _types.Add(new TypeEntry()
                {
                    _Idx = e,
                    _Type = 2
                });
            }

            //Type3
            List<int> _type3 = TotalCount(entry.Type3);
            foreach (int e in _type3)
            {
                _types.Add(new TypeEntry()
                {
                    _Idx = e,
                    _Type = 3
                });
            }

            //Type4
            List<int> _type4 = TotalCount(entry.Type4);
            foreach (int e in _type4)
            {
                _types.Add(new TypeEntry()
                {
                    _Idx = e,
                    _Type = 4
                });
            }

            //Type5
            List<int> _type5 = TotalCount(entry.Type5);
            foreach (int e in _type5)
            {
                _types.Add(new TypeEntry()
                {
                    _Idx = e,
                    _Type = 5
                });
            }

            //Type6
            List<int> _type6 = TotalCount(entry.Type6);
            foreach (int e in _type6)
            {
                _types.Add(new TypeEntry()
                {
                    _Idx = e,
                    _Type = 6
                });
            }

            return _types;
        }

        private int TotalTypeCount(Entry entry)
        {
            int count = 0;

            count += TotalCount(entry.Type0).Count;
            count += TotalCount(entry.Type1).Count;
            count += TotalCount(entry.Type2).Count;
            count += TotalCount(entry.Type3).Count;
            count += TotalCount(entry.Type4).Count;
            count += TotalCount(entry.Type5).Count;
            count += TotalCount(entry.Type6).Count;

            return count;
        }

        private List<int> TotalCount(List<Type_0> types)
        {
            List<int> count = new List<int>();
            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    if (count.IndexOf(types[i].Idx) == -1)
                    {
                        count.Add(types[i].Idx);
                    }
                }
            }
            return count;
        }

        private List<int> TotalCount(List<Type_1> types)
        {
            List<int> count = new List<int>();
            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    if (count.IndexOf(types[i].Idx) == -1)
                    {
                        count.Add(types[i].Idx);
                    }
                }
            }
            return count;
        }

        private List<int> TotalCount(List<Type_2> types)
        {
            List<int> count = new List<int>();
            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    if (count.IndexOf(types[i].Idx) == -1)
                    {
                        count.Add(types[i].Idx);
                    }
                }
            }
            return count;
        }

        private List<int> TotalCount(List<Type_3> types)
        {
            List<int> count = new List<int>();
            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    if (count.IndexOf(types[i].Idx) == -1)
                    {
                        count.Add(types[i].Idx);
                    }
                }
            }
            return count;
        }

        private List<int> TotalCount(List<Type_4> types)
        {
            List<int> count = new List<int>();
            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    if (count.IndexOf(types[i].Idx) == -1)
                    {
                        count.Add(types[i].Idx);
                    }
                }
            }
            return count;
        }

        private List<int> TotalCount(List<Type_5> types)
        {
            List<int> count = new List<int>();
            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    if (count.IndexOf(types[i].Idx) == -1)
                    {
                        count.Add(types[i].Idx);
                    }
                }
            }
            return count;
        }

        private List<int> TotalCount(List<Type_6> types)
        {
            List<int> count = new List<int>();
            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    if (count.IndexOf(types[i].Idx) == -1)
                    {
                        count.Add(types[i].Idx);
                    }
                }
            }
            return count;
        }



        //Old

        static void SizeCheck(int prevSize, int nowSize, int actualSize, int type, int count) {
            int sizeDifference = nowSize - prevSize;

            actualSize = actualSize * count;

            if (nowSize != prevSize + actualSize) {
                Console.WriteLine("Type " + type + " is wrong size. Supposed to be " + actualSize + " but is " + sizeDifference);
                Console.ReadLine();
            }
        }

        //Type Writers

        void WriteType0(List<Type_0> type, int _idx) {

            for (int i = 0; i < type.Count(); i++)
            {
                if(type[i].Idx == _idx)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes((ushort)(type[i].I_00 + type[i].I_02)));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_06));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_10));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_14));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_18));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToInt16(type[i].I_20)));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_22));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_24));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_26));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_28)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_32)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_36)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_40)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_44)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_48)));
                }
            }

        }

        void WriteType1(List<Type_1> type, int _idx)
        {

            for (int i = 0; i < type.Count(); i++)
            {
                if(type[i].Idx == _idx)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes((ushort)(type[i].I_00 + type[i].I_02)));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_06));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_16)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_20)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_24)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_28)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_32)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_36)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_40)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_44)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_48)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_52)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_56)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_60)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_64)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_68)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_72)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_76)));
                }
            }

        }

        void WriteType2(List<Type_2> type, int _idx)
        {

            for (int i = 0; i < type.Count(); i++)
            {
                if(type[i].Idx == _idx)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes((ushort)(type[i].I_00 + type[i].I_02)));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_06));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_10));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_14));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_16)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_20)));
                    bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[i].F_24)));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_28));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_32));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_36));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_40));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_44));
                }
            }

        }

        void WriteType3(List<Type_3> type, int _idx)
        {
            for (int i = 0; i < type.Count(); i++)
            {
                if(type[i].Idx == _idx)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes((ushort)(type[i].I_00 + type[i].I_02)));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_08));
                }
            }

        }

        void WriteType4(List<Type_4> type, int _idx)
        {
            for (int i = 0; i < type.Count(); i++)
            {
                if(type[i].Idx == _idx)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes((ushort)(type[i].I_00 + type[i].I_02)));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_14));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_18));
                }
            }

        }

        void WriteType5(List<Type_5> type, int _idx)
        {

            for (int i = 0; i < type.Count(); i++)
            {
                if(type[i].Idx == _idx)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes((ushort)(type[i].I_00 + type[i].I_02)));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_06));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_10));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_14));

                    Assertion.AssertArraySize(type[i].I_16, 12, "BEV_Type5", "I_16");
                    for (int a = 0; a < 12; a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(type[i].I_16[a]));
                    }
                }
            }

        }

        void WriteType6(List<Type_6> type, int _idx)
        {

            for (int i = 0; i < type.Count(); i++)
            {
                if (type[i].Idx == _idx)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes((ushort)(type[i].I_00 + type[i].I_02)));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_24));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_28));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_32));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_36));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_40));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_44));
                }
            }

        }

    }
}
