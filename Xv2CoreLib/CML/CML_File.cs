using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CML
{
    [YAXSerializeAs("CML")]
    public class CML_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CmlEntry")]
        public List<CML_Entry> Entries { get; set; } = new List<CML_Entry>();

        #region LoadSave
        public static CML_File Parse(string path, bool writeXml)
        {
            CML_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(CML_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static CML_File Parse(byte[] bytes)
        {
            CML_File cml = new CML_File();
            int numEntries = BitConverter.ToInt32(bytes, 0);
            int offset = 4;

            if (bytes.Length != 4 + (112 * numEntries))
                throw new InvalidDataException($"Error on reading cml file: Invalid file size!");

            for(int i = 0; i < numEntries; i++)
            {
                CML_Entry entry = new CML_Entry();

                entry.CharaId = BitConverter.ToUInt16(bytes, offset + 0);
                entry.Costume = BitConverter.ToUInt16(bytes, offset + 2);
                entry.I_04 = BitConverter.ToInt32(bytes, offset + 4);
                entry.F_08 = BitConverter.ToSingle(bytes, offset + 8);
                entry.F_12 = BitConverter.ToSingle(bytes, offset + 12);
                entry.F_16 = BitConverter.ToSingle(bytes, offset + 16);
                entry.F_20 = BitConverter.ToSingle(bytes, offset + 20);
                entry.F_24 = BitConverter.ToSingle(bytes, offset + 24);
                entry.F_28 = BitConverter.ToSingle(bytes, offset + 28);
                entry.F_32 = BitConverter.ToSingle(bytes, offset + 32);
                entry.F_36 = BitConverter.ToSingle(bytes, offset + 36);
                entry.F_40 = BitConverter.ToSingle(bytes, offset + 40);
                entry.F_44 = BitConverter.ToSingle(bytes, offset + 44);
                entry.F_48 = BitConverter.ToSingle(bytes, offset + 48);
                entry.F_52 = BitConverter.ToSingle(bytes, offset + 52);
                entry.F_56 = BitConverter.ToSingle(bytes, offset + 56);
                entry.F_60 = BitConverter.ToSingle(bytes, offset + 60);
                entry.F_64 = BitConverter.ToSingle(bytes, offset + 64);
                entry.F_68 = BitConverter.ToSingle(bytes, offset + 68);
                entry.F_72 = BitConverter.ToSingle(bytes, offset + 72);
                entry.F_76 = BitConverter.ToSingle(bytes, offset + 76);
                entry.F_80 = BitConverter.ToSingle(bytes, offset + 80);
                entry.F_84 = BitConverter.ToSingle(bytes, offset + 84);
                entry.F_88 = BitConverter.ToSingle(bytes, offset + 88);
                entry.F_92 = BitConverter.ToSingle(bytes, offset + 92);
                entry.F_96 = BitConverter.ToSingle(bytes, offset + 96);
                entry.F_100 = BitConverter.ToSingle(bytes, offset + 100);
                entry.F_104 = BitConverter.ToSingle(bytes, offset + 104);
                entry.F_108 = BitConverter.ToSingle(bytes, offset + 108);

                offset += 112;
                cml.Entries.Add(entry);
            }

            return cml;
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .cml file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(CML_File), YAXSerializationOptions.DontSerializeNullObjects);
            var oblFile = (CML_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, oblFile.Write());
        }

        /// <summary>
        /// Save the CML_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            if (Entries == null) Entries = new List<CML_Entry>();

            List<byte> bytes = new List<byte>();

            //Header
            bytes.AddRange(BitConverter.GetBytes(Entries.Count));

            //Entries
            foreach(var entry in Entries)
            {
                bytes.AddRange(BitConverter.GetBytes(entry.CharaId));
                bytes.AddRange(BitConverter.GetBytes(entry.Costume));
                bytes.AddRange(BitConverter.GetBytes(entry.I_04));
                bytes.AddRange(BitConverter.GetBytes(entry.F_08));
                bytes.AddRange(BitConverter.GetBytes(entry.F_12));
                bytes.AddRange(BitConverter.GetBytes(entry.F_16));
                bytes.AddRange(BitConverter.GetBytes(entry.F_20));
                bytes.AddRange(BitConverter.GetBytes(entry.F_24));
                bytes.AddRange(BitConverter.GetBytes(entry.F_28));
                bytes.AddRange(BitConverter.GetBytes(entry.F_32));
                bytes.AddRange(BitConverter.GetBytes(entry.F_36));
                bytes.AddRange(BitConverter.GetBytes(entry.F_40));
                bytes.AddRange(BitConverter.GetBytes(entry.F_44));
                bytes.AddRange(BitConverter.GetBytes(entry.F_48));
                bytes.AddRange(BitConverter.GetBytes(entry.F_52));
                bytes.AddRange(BitConverter.GetBytes(entry.F_56));
                bytes.AddRange(BitConverter.GetBytes(entry.F_60));
                bytes.AddRange(BitConverter.GetBytes(entry.F_64));
                bytes.AddRange(BitConverter.GetBytes(entry.F_68));
                bytes.AddRange(BitConverter.GetBytes(entry.F_72));
                bytes.AddRange(BitConverter.GetBytes(entry.F_76));
                bytes.AddRange(BitConverter.GetBytes(entry.F_80));
                bytes.AddRange(BitConverter.GetBytes(entry.F_84));
                bytes.AddRange(BitConverter.GetBytes(entry.F_88));
                bytes.AddRange(BitConverter.GetBytes(entry.F_92));
                bytes.AddRange(BitConverter.GetBytes(entry.F_96));
                bytes.AddRange(BitConverter.GetBytes(entry.F_100));
                bytes.AddRange(BitConverter.GetBytes(entry.F_104));
                bytes.AddRange(BitConverter.GetBytes(entry.F_108));
            }

            //validation
            if (bytes.Count != 4 + (112 * Entries.Count))
                throw new InvalidDataException($"Error on building cml: Invalid file size!");

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }

        #endregion
    }

    [YAXSerializeAs("CmlEntry")]
    public class CML_Entry : IInstallable
    {
        #region NonSerialized

        //interface
        [YAXDontSerialize]
        public int SortID { get { return CharaId; } }
        [YAXDontSerialize]
        public string Index 
        { 
            get
            { 
                return $"{CharaId}_{Costume}";
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 2)
                {
                    CharaId = ushort.Parse(split[0]);
                    Costume = ushort.Parse(split[1]);
                }
            }
        }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("CharaId")]
        public ushort CharaId { get; set; } //0
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public ushort Costume { get; set; } //2
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; } //4
        [YAXAttributeFor("CSS_POS_Z")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_08 { get; set; } //8
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_12 { get; set; } //12
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("F_48")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("F_52")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("F_56")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("F_60")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("F_64")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("F_68")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("F_72")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_72 { get; set; }
        [YAXAttributeFor("F_76")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_76 { get; set; }
        [YAXAttributeFor("CSS_ROT_X")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_80 { get; set; }
        [YAXAttributeFor("CSS_ROT_Y")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_84 { get; set; }
        [YAXAttributeFor("CSS_POS_Y")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_88 { get; set; }
        [YAXAttributeFor("CSS_POS_X")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_92 { get; set; }
        [YAXAttributeFor("F_96")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_96 { get; set; }
        [YAXAttributeFor("F_100")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_100 { get; set; }
        [YAXAttributeFor("F_104")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_104 { get; set; }
        [YAXAttributeFor("F_108")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_108 { get; set; }

    }
}
