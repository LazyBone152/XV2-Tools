using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CAT
{
    [YAXSerializeAs("CAT")]
    public class CAT_File
    {
        private const uint CAT_SIGNATURE = 0x54414323;
        private const uint CAT_HEADER_SIZE = 0xC;

        public const int CAT_ENTRY_SIZE = 0x18; // Ver 1 (original)

        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 1)]
        public int Version { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CATEntry")]
        public List<CAT_Entry> Entries { get; set; } = new List<CAT_Entry>();

        #region LoadSave
        public static CAT_File Parse(string path, bool writeXml)
        {
            CAT_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(CAT_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static CAT_File Parse(byte[] bytes)
        {
            CAT_File CATFile = new CAT_File();
            int numEntries = BitConverter.ToInt16(bytes, 6);
            int entrySize = (int)((bytes.Length - CAT_HEADER_SIZE) / numEntries);
            CATFile.Version = EntrySizeToVersion(entrySize);
            int offset = BitConverter.ToInt16(bytes, 8);

            if (bytes.Length != offset + (entrySize * numEntries))
                throw new InvalidDataException($"Error on reading CAT file: Invalid file size!");

            for(int i = 0; i < numEntries; i++)
            {
                CAT_Entry entry = new CAT_Entry();

                entry.CharaId = BitConverter.ToUInt16(bytes, offset + 0);
                entry.Costume = BitConverter.ToUInt16(bytes, offset + 2);
                entry.I_04 = BitConverter.ToUInt16(bytes, offset + 4);
                entry.I_06 = BitConverter.ToUInt16(bytes, offset + 6);
                entry.Str_08 = StringEx.GetString(bytes, offset + 8, false, StringEx.EncodingType.ASCII, 8);
                entry.I_12 = BitConverter.ToInt32(bytes, offset + 12);
                entry.I_16 = BitConverter.ToInt32(bytes, offset + 16);
                entry.I_20 = BitConverter.ToInt32(bytes, offset + 20);

                offset += entrySize;
                CATFile.Entries.Add(entry);
            }

            return CATFile;
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .CAT file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(CAT_File), YAXSerializationOptions.DontSerializeNullObjects);
            var CATFile = (CAT_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, CATFile.Write());
        }

        /// <summary>
        /// Save the CAT_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            if (Entries == null) Entries = new List<CAT_Entry>();

            List<byte> bytes = new List<byte>();

            uint offset = (int)CAT_HEADER_SIZE;

            //Header
            bytes.AddRange(BitConverter.GetBytes(CAT_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)Entries.Count));
            bytes.AddRange(BitConverter.GetBytes(offset));

            //Entries
            foreach (var entry in Entries)
            {
                if (entry.Str_08.Length > 3)
                    throw new Exception($"ShortName \"{entry.Str_08}\" exceeds 3 characters!");

                bytes.AddRange(BitConverter.GetBytes(entry.CharaId));
                bytes.AddRange(BitConverter.GetBytes(entry.Costume));
                bytes.AddRange(BitConverter.GetBytes(entry.I_04));
                bytes.AddRange(BitConverter.GetBytes(entry.I_06));
                bytes.AddRange(Encoding.ASCII.GetBytes(entry.Str_08));
                bytes.AddRange(new byte[4 - entry.Str_08.Length]);
                bytes.AddRange(BitConverter.GetBytes(entry.I_12));
                bytes.AddRange(BitConverter.GetBytes(entry.I_16));
                bytes.AddRange(BitConverter.GetBytes(entry.I_20));
            }

            //validation
            if (bytes.Count != CAT_HEADER_SIZE + ((int)CAT_ENTRY_SIZE * Entries.Count))
                throw new InvalidDataException($"Error on building CAT: Invalid file size!");

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }

        public static int VersionToEntrySize(int version)
        {
            switch (version)
            {
                case 1:
                    return CAT_ENTRY_SIZE;
                default:
                    throw new InvalidDataException($"CAT: This CAT version is not supported (Version: {version}).");
            }
        }

        public static int EntrySizeToVersion(int entrySize)
        {
            // Add more for when file ever updates
            switch (entrySize)
            {
                case CAT_ENTRY_SIZE:
                    return 1;
                default:
                    throw new InvalidDataException($"CAT: This CAT version is not supported (EntrySize: {entrySize}).");
            }
        }
        #endregion
    }

    [YAXSerializeAs("CATEntry")]
    public class CAT_Entry : IInstallable
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
        public ushort CharaId { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public ushort Costume { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("Awoken_Skill_ID2")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("Chara")]
        [YAXSerializeAs("value")]
        public string Str_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
    }
}
