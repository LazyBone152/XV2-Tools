using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.CBS
{
    [YAXSerializeAs("CBS")]
    public class CBS_File
    {
        private const uint CBS_SIGNATURE = 0x53424323;
        private const uint CBS_HEADER_SIZE = 0x10;

        public const int CBS_ENTRY_SIZE = 0x30; // Ver 1 (original)

        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 65534)]
        public ushort I_00 { get; set; }

        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 1)]
        public ushort Version { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Entry")]
        public List<CBS_Entry> Entries { get; set; } = new List<CBS_Entry>();

        #region LoadSave
        public static CBS_File Parse(string path, bool writeXml)
        {
            CBS_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(CBS_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static CBS_File Parse(byte[] bytes)
        {
            CBS_File cbsFile = new CBS_File();
            int numEntries = BitConverter.ToInt32(bytes, 8);
            int entrySize = (int)((bytes.Length - CBS_HEADER_SIZE) / numEntries);
            cbsFile.I_00 = BitConverter.ToUInt16(bytes, 4);
            cbsFile.Version = EntrySizeToVersion(entrySize);
            int offset = 16;

            if (bytes.Length != offset + (entrySize * numEntries))
                throw new InvalidDataException($"Error on reading cbs file: Invalid file size!");

            for(int i = 0; i < numEntries; i++)
            {
                CBS_Entry entry = new CBS_Entry();

                entry.CharaId = BitConverter.ToUInt16(bytes, offset + 0);
                entry.BodyId = BitConverter.ToUInt16(bytes, offset + 2);
                entry.F_04 = BitConverter.ToSingle(bytes, offset + 4);
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

                offset += entrySize;
                cbsFile.Entries.Add(entry);
            }

            return cbsFile;
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .cbs file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(CBS_File), YAXSerializationOptions.DontSerializeNullObjects);
            var cbsFile = (CBS_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, cbsFile.Write());
        }

        /// <summary>
        /// Save the CBS_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            if (Entries == null) Entries = new List<CBS_Entry>();

            List<byte> bytes = new List<byte>();

            uint offset = 16;

            //Header
            bytes.AddRange(BitConverter.GetBytes(CBS_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes((ushort)16));
            bytes.AddRange(BitConverter.GetBytes(Entries.Count));
            bytes.AddRange(BitConverter.GetBytes(offset));

            //Entries
            foreach (var entry in Entries)
            {
                bytes.AddRange(BitConverter.GetBytes(entry.CharaId));
                bytes.AddRange(BitConverter.GetBytes(entry.BodyId));
                bytes.AddRange(BitConverter.GetBytes(entry.F_04));
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
            }

            //validation
            if (bytes.Count != 16 + (VersionToEntrySize(Version) * Entries.Count))
                throw new InvalidDataException($"Error on building cbs: Invalid file size!");

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }

        public static int VersionToEntrySize(ushort version)
        {
            switch (version)
            {
                case 1:
                    return CBS_ENTRY_SIZE;
                default:
                    throw new InvalidDataException($"CBS: This CBS version is not supported (Version: {version}).");
            }
        }

        public static ushort EntrySizeToVersion(int entrySize)
        {
            // Add more for when file ever updates
            switch (entrySize)
            {
                case CBS_ENTRY_SIZE:
                    return 1;
                default:
                    throw new InvalidDataException($"CBS: This CBS version is not supported (EntrySize: {entrySize}).");
            }
        }
        #endregion
    }

    [YAXSerializeAs("CbsEntry")]
    public class CBS_Entry : IInstallable
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
                return $"{CharaId}_{BodyId}";
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 2)
                {
                    CharaId = ushort.Parse(split[0]);
                    BodyId = ushort.Parse(split[1]);
                }
            }
        }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("CharaId")]
        public ushort CharaId { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("BodyId")]
        public ushort BodyId { get; set; }
        [YAXAttributeFor("F_04")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_12 { get; set; }
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
    }
}
