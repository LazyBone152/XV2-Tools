using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CNC
{
    [YAXSerializeAs("CNC")]
    public class CNC_File : ISorting
    {
        [YAXDontSerialize]
        public const int CNC_SIGNATURE = 1129202467;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CncEntry")]
        public List<CNC_Entry> CncEntries { get; set; }
        
        public static CNC_File Load(byte[] bytes)
        {
            return Read(bytes);
        }

        public void SortEntries()
        {
            CncEntries.Sort((x, y) => x.SortID - y.SortID);
        }

        public static CNC_File Read(byte[] rawBytes)
        {
            //Validation
            if (BitConverter.ToInt32(rawBytes, 0) != CNC_SIGNATURE)
            {
                throw new InvalidDataException("CNC_SIGNATURE not found at offset 0x0. Parse failed.");
            }

            int count = BitConverter.ToUInt16(rawBytes, 6);
            int offset = BitConverter.ToInt32(rawBytes, 8);

            //Parse file
            CNC_File cncFile = new CNC_File() { CncEntries = new List<CNC_Entry>() };

            for (int i = 0; i < count; i++)
            {
                cncFile.CncEntries.Add(CNC_Entry.Read(rawBytes, offset));
                offset += 12;
            }

            return cncFile;
        }

        public static CNC_File Read(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);

            CNC_File cncFile = Read(rawBytes);

            //Write Xml
            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(CNC_File));
                serializer.SerializeToFile(cncFile, path + ".xml");
            }

            return cncFile;
        }

        public static void Write(CNC_File cncFile, string path)
        {
            byte[] bytes = cncFile.SaveToBytes();

            //Saving
            File.WriteAllBytes(path, bytes.ToArray());
        }

        public byte[] SaveToBytes()
        {
            List<byte> bytes = new List<byte>();

            //Header
            bytes.AddRange(BitConverter.GetBytes(CNC_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((UInt16)65534));
            bytes.AddRange(BitConverter.GetBytes((UInt16)CncEntries.Count));
            bytes.AddRange(BitConverter.GetBytes((UInt32)12));

            //Entries
            for (int i = 0; i < CncEntries.Count; i++)
            {
                bytes.AddRange(CNC_Entry.Write(CncEntries[i]));
            }

            return bytes.ToArray();
        }

        public static void ReadXmlAndWriteBinary(string xmlPath)
        {
            string path = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(CNC_File), YAXSerializationOptions.DontSerializeNullObjects);
            Write((CNC_File)serializer.DeserializeFromFile(xmlPath), path);
        }
        
        /// <summary>
        /// Adds a CncEntry to the file. If an entry with the same IDs exist then it will overwrite that one.
        /// </summary>
        /// <param name="cncEntry"></param>
        public void AddEntry(CNC_Entry cncEntry)
        {
            int charaId = int.Parse(cncEntry.I_00);
            int costume = cncEntry.I_02;
            int modelPreset = cncEntry.I_04;

            for (int i = 0; i < CncEntries.Count; i++)
            {
                if (int.Parse(CncEntries[i].I_00) == charaId && CncEntries[i].I_02 == costume && CncEntries[i].I_04 == modelPreset)
                {
                    CncEntries[i] = cncEntry;
                    return;
                }
            }

            //Entry didn't already exist, so add it as a new entry.
            CncEntries.Add(cncEntry);
        }


    }

    [YAXSerializeAs("CncEntry")]
    public class CNC_Entry : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(I_00); } }
        [YAXDontSerialize]
        public string Index
        {
            get
            {
                return string.Format("{0}_{1}_{2}", I_00, I_02, I_04);
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 3)
                {
                    I_00 = split[0];
                    I_02 = ushort.Parse(split[1]);
                    I_04 = ushort.Parse(split[2]);
                }
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Character_ID")]
        public string I_00 { get; set; } //ushort
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Model_Preset")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("Dual_Skill_1")]
        [YAXSerializeAs("Dual_ID")]
        public string I_06 { get; set; } //ushort
        [YAXAttributeFor("Dual_Skill_2")]
        [YAXSerializeAs("Dual_ID")]
        public string I_08 { get; set; } //ushort
        [YAXAttributeFor("Dual_Skill_3")]
        [YAXSerializeAs("Dual_ID")]
        public string I_10 { get; set; } //ushort

        public static CNC_Entry Read(byte[] rawBytes, int offset)
        {
            return new CNC_Entry()
            {
                I_00 = BitConverter.ToUInt16(rawBytes, offset + 0).ToString(),
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                I_06 = BitConverter.ToUInt16(rawBytes, offset + 6).ToString(),
                I_08 = BitConverter.ToUInt16(rawBytes, offset + 8).ToString(),
                I_10 = BitConverter.ToUInt16(rawBytes, offset + 10).ToString()
            };
        }

        public static List<byte> Write(CNC_Entry cncEntry)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(ushort.Parse(cncEntry.I_00)));
            bytes.AddRange(BitConverter.GetBytes(cncEntry.I_02));
            bytes.AddRange(BitConverter.GetBytes(cncEntry.I_04));
            bytes.AddRange(BitConverter.GetBytes(ushort.Parse(cncEntry.I_06)));
            bytes.AddRange(BitConverter.GetBytes(ushort.Parse(cncEntry.I_08)));
            bytes.AddRange(BitConverter.GetBytes(ushort.Parse(cncEntry.I_10)));
            return bytes;
        }

    }



}
