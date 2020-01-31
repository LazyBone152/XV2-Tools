using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CNS
{
    [YAXSerializeAs("CNS")]
    public class CNS_File : ISorting
    {
        [YAXDontSerialize]
        public const int CNS_SIGNATURE = 1397637923;

        [YAXAttributeForClass]
        public ushort Version { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeForClass]
        public ushort[] UnknownValues { get; set; } //size 4

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CnsEntry")]
        public List<CNS_Entry> CnsEntries { get; set; }

        public void SortEntries()
        {
            CnsEntries.Sort((x, y) => x.SortID - y.SortID);
        }

        public static CNS_File Load(byte[] bytes)
        {
            return Read(bytes);
        }

        public static CNS_File Read(byte[] rawBytes)
        {
            List<byte> bytes = rawBytes.ToList();

            //Validation
            if (BitConverter.ToInt32(rawBytes, 0) != CNS_SIGNATURE)
            {
                throw new InvalidDataException("CNS_SIGNATURE not found at offset 0x0. Parse failed.");
            }

            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 16);

            //Parse file
            CNS_File cnsFile = new CNS_File() { CnsEntries = new List<CNS_Entry>() };
            cnsFile.Version = BitConverter.ToUInt16(rawBytes, 6);

            //Unknown values
            cnsFile.UnknownValues = BitConverter_Ex.ToUInt16Array(rawBytes, 20, 4);

            //Entries
            for (int i = 0; i < count; i++)
            {
                cnsFile.CnsEntries.Add(CNS_Entry.Read(rawBytes, bytes, offset));
                offset += 180;
            }

            return cnsFile;
        }

        public static CNS_File Read(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);

            CNS_File cnsFile = Read(rawBytes);

            //Write Xml
            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(CNS_File));
                serializer.SerializeToFile(cnsFile, path + ".xml");
            }

            return cnsFile;

        }

        public static void Write(CNS_File cnsFile, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            //Saving
            File.WriteAllBytes(path, cnsFile.SaveToBytes());
        }

        public byte[] SaveToBytes()
        {
            SortEntries();

            List<byte> bytes = new List<byte>();

            //Header
            bytes.AddRange(BitConverter.GetBytes(CNS_SIGNATURE)); //0
            bytes.AddRange(BitConverter.GetBytes((UInt16)65534)); //4
            bytes.AddRange(BitConverter.GetBytes(Version)); //6
            bytes.AddRange(BitConverter.GetBytes(CnsEntries.Count)); //8
            bytes.AddRange(BitConverter.GetBytes(20)); //12
            bytes.AddRange(BitConverter.GetBytes(28)); //16

            //Unknown values
            Assertion.AssertArraySize(UnknownValues, 4, "CNS", "UnknownValues");
            bytes.AddRange(BitConverter_Ex.GetBytes(UnknownValues));

            //Entries
            for (int i = 0; i < CnsEntries.Count; i++)
            {
                bytes.AddRange(CNS_Entry.Write(CnsEntries[i]));
            }

            return bytes.ToArray();
        }

        public static void ReadXmlAndWriteBinary(string xmlPath)
        {
            string path = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(CNS_File), YAXSerializationOptions.DontSerializeNullObjects);
            Write((CNS_File)serializer.DeserializeFromFile(xmlPath), path);
        }
        
        /// <summary>
        /// Add a new CNS_Entry or replace an existing one with the same code.
        /// </summary>
        /// <param name="code">The 4-letter code for the Dual Skill. This should be the same as the one defined in the CUS.</param>
        /// <param name="id">ID of the Dual Skill. Use -1 to automatically assign the next unused ID.</param>
        public void AddEntry(CNS_Entry cnsEntry, string code, int id = -1)
        {
            for(int i = 0; i < CnsEntries.Count; i++)
            {
                if(CnsEntries[i].Str_00 == code)
                {
                    //Entry already exists, so replace it and keep the same Dual ID.
                    cnsEntry.Index = CnsEntries[i].Index;
                    CnsEntries[i] = cnsEntry;
                    return;
                }
            }

            //Entry didn't already exist, so add it as a new entry.
            if(id == -1)
            {
                cnsEntry.Index = NextId().ToString();
            }
            else
            {
                cnsEntry.Index = id.ToString();
            }
            CnsEntries.Add(cnsEntry);

        }

        private ushort NextId()
        {
            int id = 0;
            List<int> ids = new List<int>();

            foreach(var e in CnsEntries)
            {
                ids.Add(int.Parse(e.Index));
            }

            while (true)
            {
                if (ids.Contains(id))
                {
                    id++;
                }
                else
                {
                    break;
                }
            }

            return (ushort)id;
        }
    }

    [YAXSerializeAs("CnsEntry")]
    public class CNS_Entry : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeForClass]
        [YAXSerializeAs("Skill_Code")]
        public string Str_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Dual_ID")]
        [BindingAutoId]
        public string Index { get; set; } //uint16: I_08
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_10 { get; set; } //Size 33
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_76 { get; set; } //Size 33
        [YAXAttributeFor("I_142")]
        [YAXSerializeAs("value")]
        public ushort I_142 { get; set; }
        [YAXAttributeFor("I_144")]
        [YAXSerializeAs("value")]
        public ushort I_144 { get; set; }
        [YAXAttributeFor("I_146")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_146 { get; set; } //Size 7
        [YAXAttributeFor("Owner_CMS_ID")]
        [YAXSerializeAs("value")]
        public ushort I_160 { get; set; }
        [YAXAttributeFor("I_162")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_162 { get; set; } //Size 7
        [YAXAttributeFor("Owner_CMS_ID2")]
        [YAXSerializeAs("value")]
        public ushort I_176 { get; set; }
        [YAXAttributeFor("I_178")]
        [YAXSerializeAs("value")]
        public ushort I_178 { get; set; }

        public static CNS_Entry Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new CNS_Entry()
            {
                Str_00 = Utils.GetString(bytes, offset),
                Index = BitConverter.ToUInt16(rawBytes, offset + 8).ToString(),
                I_10 = BitConverter_Ex.ToUInt16Array(rawBytes, offset + 10, 33),
                I_76 = BitConverter_Ex.ToUInt16Array(rawBytes, offset + 76, 33),
                I_142 = BitConverter.ToUInt16(rawBytes, offset + 142),
                I_144 = BitConverter.ToUInt16(rawBytes, offset + 144),
                I_146 = BitConverter_Ex.ToUInt16Array(rawBytes, offset + 146, 7),
                I_160 = BitConverter.ToUInt16(rawBytes, offset + 160),
                I_162 = BitConverter_Ex.ToUInt16Array(rawBytes, offset + 162, 7),
                I_176 = BitConverter.ToUInt16(rawBytes, offset + 176),
                I_178 = BitConverter.ToUInt16(rawBytes, offset + 178),
            };
        }

        public static List<byte> Write(CNS_Entry cnsEntry)
        {
            List<byte> bytes = new List<byte>();

            //Code
            if(cnsEntry.Str_00.Length > 8)
            {
                throw new Exception(String.Format("Dual_Code = {0} exceeds the maximum allowed length of 8. Load failed.", cnsEntry.Str_00));
            }
            bytes.AddRange(Encoding.ASCII.GetBytes(cnsEntry.Str_00));
            bytes.AddRange(new byte[8 - cnsEntry.Str_00.Length]);

            //Validate arrays
            Assertion.AssertArraySize(cnsEntry.I_10, 33, "CnsEntry", "I_10");
            Assertion.AssertArraySize(cnsEntry.I_76, 33, "CnsEntry", "I_76");
            Assertion.AssertArraySize(cnsEntry.I_146, 7, "CnsEntry", "I_146");
            Assertion.AssertArraySize(cnsEntry.I_162, 7, "CnsEntry", "I_162");

            //Remaining values
            bytes.AddRange(BitConverter.GetBytes(ushort.Parse(cnsEntry.Index)));
            bytes.AddRange(BitConverter_Ex.GetBytes(cnsEntry.I_10));
            bytes.AddRange(BitConverter_Ex.GetBytes(cnsEntry.I_76));
            bytes.AddRange(BitConverter.GetBytes(cnsEntry.I_142));
            bytes.AddRange(BitConverter.GetBytes(cnsEntry.I_144));
            bytes.AddRange(BitConverter_Ex.GetBytes(cnsEntry.I_146));
            bytes.AddRange(BitConverter.GetBytes(cnsEntry.I_160));
            bytes.AddRange(BitConverter_Ex.GetBytes(cnsEntry.I_162));
            bytes.AddRange(BitConverter.GetBytes(cnsEntry.I_176));
            bytes.AddRange(BitConverter.GetBytes(cnsEntry.I_178));

            return bytes;
        }

    }
}
