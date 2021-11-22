using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BDM
{
    public class Deserializer
    {
        string saveLocation;
        BDM_File bdm_File;
        public List<byte> bytes { get; private set; } = new List<byte>() { 35, 66, 68, 77, 254, 255, 0, 0 };
        private BDM_Type type;
        int count = 0;

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            ReadXmlFile(location);
            type = GetBdmType();
            bdm_File.SortEntries();
            Validation();
            WriteFile();
            SaveBinaryFile();
        }

        public Deserializer(BDM_File _bdmFile, string location)
        {
            saveLocation = location;
            bdm_File = _bdmFile;
            bdm_File.SortEntries();
            type = GetBdmType();
            Validation();
            WriteFile();
            SaveBinaryFile();
        }

        public Deserializer(BDM_File _bdmFile)
        {
            bdm_File = _bdmFile;
            bdm_File.SortEntries();
            type = GetBdmType();
            Validation();
            WriteFile();
        }

        void Validation()
        {
            switch (type)
            {
                case BDM_Type.XV2_0:
                    ValidateType0_Xv2();
                    break;
                case BDM_Type.XV2_1:
                    ValidateType1_Xv2();
                    break;
                case BDM_Type.XV1:
                    ValidateType1_Xv1();
                    break;
                default:
                    Console.WriteLine("Unknown BDM_Type.\nDeserialization failed.");
                    Console.ReadLine();
                    Environment.Exit(0);
                    break;
            }
        }

        void ValidateType0_Xv2()
        {
            int count = 0;

            for (int i = 0; i < bdm_File.BDM_Entries.Count(); i++)
            {
                if (bdm_File.BDM_Entries[i].Type0Entries != null)
                {
                    count++;
                }
            }

            if (count != bdm_File.BDM_Entries.Count())
            {
                Console.WriteLine("The BDM type is declared as of Type XV2_0, but the data does not represent that of a Type XV2_0 BDM.\nDeserialization failed.");
                Console.ReadLine();
                Environment.Exit(0);
            }

        }

        void ValidateType1_Xv2()
        {
            int count = 0;

            for (int i = 0; i < bdm_File.BDM_Entries.Count(); i++)
            {
                if (bdm_File.BDM_Entries[i].Type1Entries != null)
                {
                    count++;
                }
            }

            if (count != bdm_File.BDM_Entries.Count())
            {
                Console.WriteLine("The BDM type is declared as of Type XV2_1, but the data does not represent that of a Type XV2_1 BDM.\nDeserialization failed.");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        void ValidateType1_Xv1()
        {
            int count = 0;
            int prevID = -1;

            for (int i = 0; i < bdm_File.BDM_Entries.Count(); i++)
            {
                if(prevID + 1 != bdm_File.BDM_Entries[i].I_00 )
                {
                    Console.WriteLine(String.Format("BDM_Entry ID {0} is out of range. For Type XV1, the ID parameter must be consecutive.", bdm_File.BDM_Entries[i].I_00));
                    Utils.WaitForInputThenQuit();
                }
                if (bdm_File.BDM_Entries[i].Type1Entries != null)
                {
                    count++;
                }
                prevID++;
            }

            if (count != bdm_File.BDM_Entries.Count())
            {
                Console.WriteLine("The BDM type is declared as of Type XV1 , but the data does not represent that of a Type XV1.\nDeserialization failed.");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }


        //validation end

        void WriteFile() {
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(16));

            for (int i = 0; i < count; i++)
            {
                switch (type)
                {
                    case BDM_Type.XV2_0:
                        bytes.AddRange(BitConverter.GetBytes(bdm_File.BDM_Entries[i].I_00));
                        WriteType0_Xv2(bdm_File.BDM_Entries[i].Type0Entries, bdm_File.BDM_Entries[i].I_00);
                        break;
                    case BDM_Type.XV2_1:
                        bytes.AddRange(BitConverter.GetBytes(bdm_File.BDM_Entries[i].I_00));
                        WriteType1_Xv2(bdm_File.BDM_Entries[i].Type1Entries, bdm_File.BDM_Entries[i].I_00);
                        break;
                    case BDM_Type.XV1:
                        WriteType1_Xv1(bdm_File.BDM_Entries[i].Type1Entries, bdm_File.BDM_Entries[i].I_00);
                        break;
                }
            }

        }

        void ReadXmlFile(string path)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(BDM_File), YAXSerializationOptions.DontSerializeNullObjects);
            bdm_File = (BDM_File)serializer.DeserializeFromFile(path);
        }

        void SaveBinaryFile()
        {
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        private BDM_Type GetBdmType()
        {

            switch (bdm_File.BDM_Type)
            {
                case BDM_Type.XV2_0:
                    if (bdm_File.BDM_Entries != null) { count = bdm_File.BDM_Entries.Count(); }
                    return bdm_File.BDM_Type;
                case BDM_Type.XV2_1:
                    if(bdm_File.BDM_Entries != null) { count = bdm_File.BDM_Entries.Count(); }
                    return bdm_File.BDM_Type;
                case BDM_Type.XV1:
                    if (bdm_File.BDM_Entries != null) { count = bdm_File.BDM_Entries.Count(); }
                    return bdm_File.BDM_Type;
                default:
                    return bdm_File.BDM_Type;
            }

        }


        //Writers

        void WriteType0_Xv2(List<Type0SubEntry> type0, int bdmId) {
            //Validation
            if (type0.Count() > 10)
            {
                Console.WriteLine(String.Format("Invalid amount of SubEntries. (BDM ID: {0}).\nDeserialization failed.", bdmId));
                Console.ReadLine();
                Environment.Exit(0);
            }
            while (type0.Count() < 10)
            {
                int idx = type0.Count();
                type0.Add(type0[0].Clone());
                type0[idx].Index = idx;
            }

            int[] properOrder = new int[10];
            List<int> passedIndex = new List<int>();
            for (int a = 0; a < 10; a++)
            {
                properOrder[a] = type0[a].Index;
            }

            for (int i = 0; i < 10; i++) {
                if (properOrder[i] > 9 || properOrder[i] < 0)
                {
                    Console.WriteLine(String.Format("{0} is not a valid index for a BDM SubEntry (BDM ID: {1}).\nDeserialization failed.", properOrder[i], bdmId));
                    Console.ReadLine();
                    Environment.Exit(0);
                }
                if (passedIndex.IndexOf(properOrder[i]) != -1)
                {
                    Console.WriteLine(String.Format("Index {0} is already defined for this SubEntry (BDM ID: {1}).\nDeserialization failed.", properOrder[i], bdmId));
                    Console.ReadLine();
                    Environment.Exit(0);
                }

                passedIndex.Add(properOrder[i]);
                
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_00));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_02));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_04));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_06));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type0[properOrder[i]].F_08)));
                bytes.AddRange(BitConverter.GetBytes((ushort)type0[properOrder[i]].I_12));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_14));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_16));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].Effect1_SkillID));
                bytes.AddRange(BitConverter.GetBytes((ushort)type0[properOrder[i]].I_20));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_22));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_24));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].Effect2_SkillID));
                bytes.AddRange(BitConverter.GetBytes((ushort)type0[properOrder[i]].I_28));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_30));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_32));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].Effect3_SkillID));
                bytes.AddRange(BitConverter.GetBytes((ushort)type0[properOrder[i]].I_36));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_38));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type0[properOrder[i]].F_40)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type0[properOrder[i]].F_44)));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_48));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_50));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_52));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_54));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_56));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_58));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type0[properOrder[i]].F_60)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type0[properOrder[i]].F_64)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type0[properOrder[i]].F_68)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type0[properOrder[i]].F_72)));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_76));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_78));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_80));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_82));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_84));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_86));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_88[0]));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_88[1]));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_88[2]));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_94));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_96[0]));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_96[1]));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_100));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_102));
                bytes.Add((Byte)type0[properOrder[i]].I_104);
                bytes.Add(0);
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_106));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_108));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_110));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_112));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_114));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_116));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].I_118));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].F_120));
                bytes.AddRange(BitConverter.GetBytes(type0[properOrder[i]].F_124));
                Assertion.AssertArraySize(type0[properOrder[i]].I_88, 3, "BDM_Entry", "I_88");
                Assertion.AssertArraySize(type0[properOrder[i]].I_96, 2, "BDM_Entry", "I_96");

            }
            

        }

        void WriteType1_Xv2(List<Type1SubEntry> type, int bdmId)
        {
            //Validation

            if (type.Count() > 10)
            {
                Console.WriteLine(String.Format("Invalid amount of SubEntries. (BDM ID: {0}).\nDeserialization failed.", bdmId));
                Console.ReadLine();
                Environment.Exit(0);
            }
            while (type.Count() < 10)
            {
                int idx = type.Count();
                type.Add(type[0].Clone());
                type[idx].Index = idx;
            }

            int[] properOrder = new int[10];
            List<int> passedIndex = new List<int>();
            for (int a = 0; a < 10; a++)
            {
                properOrder[a] = type[a].Index;
            }

            for (int i = 0; i < 10; i++)
            {
                if (properOrder[i] > 9 || properOrder[i] < 0)
                {
                    Console.WriteLine(String.Format("{0} is not a valid index for a BDM SubEntry (BDM ID: {1}).\nDeserialization failed.", properOrder[i], bdmId));
                    Console.ReadLine();
                    Environment.Exit(0);
                }
                if (passedIndex.IndexOf(properOrder[i]) != -1)
                {
                    Console.WriteLine(String.Format("Index {0} is already defined for this SubEntry (BDM ID: {1}).\nDeserialization failed.", properOrder[i], bdmId));
                    Console.ReadLine();
                    Environment.Exit(0);
                }

                passedIndex.Add(properOrder[i]);

                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_02));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_04));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_06));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_08)));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_12));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_14));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_16));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_18));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_20));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_22));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_24));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_26));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_28));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_30));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_32)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_36)));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_40));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_42));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_44));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_46));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_48));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_50));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_52)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_56)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_60)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_64)));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_68));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_70));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_72));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_74));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_76));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_78));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_80[0]));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_80[1]));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_80[2]));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_86));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_88[0]));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_88[1]));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_92));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_94));
                bytes.Add((Byte)type[properOrder[i]].I_96);
                bytes.Add(0);
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_98));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_100));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_102));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_104));
                Assertion.AssertArraySize(type[properOrder[i]].I_80, 3, "BDM_Entry", "I_80");
                Assertion.AssertArraySize(type[properOrder[i]].I_88, 2, "BDM_Entry", "I_88");
            }
        }

        void WriteType1_Xv1(List<Type1SubEntry> type, int bdmId)
        {
            //Validation

            if (type.Count() > 7)
            {
                Console.WriteLine(String.Format("Invalid amount of SubEntries. (BDM ID: {0}).\nDeserialization failed.", bdmId));
                Console.ReadLine();
                Environment.Exit(0);
            }
            while(type.Count() < 7)
            {
                int idx = type.Count();
                type.Add(type[0].Clone());
                type[idx].Index = idx;
            }

            int[] properOrder = new int[7];
            List<int> passedIndex = new List<int>();
            for (int a = 0; a < 7; a++)
            {
                properOrder[a] = type[a].Index;
            }

            for (int i = 0; i < 7; i++)
            {
                if (properOrder[i] > 6 || properOrder[i] < 0)
                {
                    Console.WriteLine(String.Format("{0} is not a valid index for a BDM SubEntry (BDM ID: {1}).\nDeserialization failed.", properOrder[i], bdmId));
                    Console.ReadLine();
                    Environment.Exit(0);
                }
                if (passedIndex.IndexOf(properOrder[i]) != -1)
                {
                    Console.WriteLine(String.Format("Index {0} is already defined for this SubEntry (BDM ID: {1}).\nDeserialization failed.", properOrder[i], bdmId));
                    Console.ReadLine();
                    Environment.Exit(0);
                }

                passedIndex.Add(properOrder[i]);

                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_02));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_04));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_06));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_08)));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_12));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_14));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_16));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_18));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_20));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_22));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_24));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_26));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_28));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_30));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_32)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_36)));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_40));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_42));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_44));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_46));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_48));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_50));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_52)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_56)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_60)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToSingle(type[properOrder[i]].F_64)));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_68));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_70));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_72));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_74));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_76));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_78));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_80[0]));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_80[1]));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_80[2]));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_86));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_88[0]));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_88[1]));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_92));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_94));
                bytes.Add((Byte)type[properOrder[i]].I_96);
                bytes.Add(0);
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_98));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_100));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_102));
                bytes.AddRange(BitConverter.GetBytes(type[properOrder[i]].I_104));

                Assertion.AssertArraySize(type[properOrder[i]].I_80, 3, "BDM_Entry", "I_80");
                Assertion.AssertArraySize(type[properOrder[i]].I_88, 2, "BDM_Entry", "I_88");
            }


        }

    }
}
