using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.IKD
{
    [YAXSerializeAs("IKD")]
    public class IKD_File
    {
        private const uint IKD_SIGNATURE = 0x444b4923;
        private const uint IKD_HEADER_SIZE = 0x10;

        public const int IKD_ENTRY_SIZE = 0x3c; // Ver 1 (original)

        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 1)]
        public int Version { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "IkdEntry")]
        public List<IKD_Entry> Entries { get; set; } = new List<IKD_Entry>();

        #region LoadSave
        public static IKD_File Parse(string path, bool writeXml)
        {
            IKD_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(IKD_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static IKD_File Parse(byte[] bytes)
        {
            IKD_File ikdFile = new IKD_File();
            int numEntries = BitConverter.ToInt32(bytes, 8);
            int entrySize = (int)((bytes.Length - IKD_HEADER_SIZE) / numEntries);
            ikdFile.Version = EntrySizeToVersion(entrySize);
            int offset = 16;

            if (bytes.Length != offset + (entrySize * numEntries))
                throw new InvalidDataException($"Error on reading ikd file: Invalid file size!");

            for(int i = 0; i < numEntries; i++)
            {
                IKD_Entry entry = new IKD_Entry();

                entry.CharaId = BitConverter.ToUInt16(bytes, offset + 0);
                entry.BoneScaleIdx = BitConverter.ToUInt16(bytes, offset + 2);
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
                entry.F_48 = BitConverter.ToSingle(bytes, offset + 48);
                entry.F_52 = BitConverter.ToSingle(bytes, offset + 52);
                entry.F_56 = BitConverter.ToSingle(bytes, offset + 56);

                offset += entrySize;
                ikdFile.Entries.Add(entry);
            }

            return ikdFile;
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .ikd file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(IKD_File), YAXSerializationOptions.DontSerializeNullObjects);
            var ikdFile = (IKD_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, ikdFile.Write());
        }

        /// <summary>
        /// Save the IKD_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            if (Entries == null) Entries = new List<IKD_Entry>();

            List<byte> bytes = new List<byte>();

            uint offset = 16;

            //Header
            bytes.AddRange(BitConverter.GetBytes(IKD_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(Entries.Count));
            bytes.AddRange(BitConverter.GetBytes(offset));

            //Entries
            foreach (var entry in Entries)
            {
                bytes.AddRange(BitConverter.GetBytes(entry.CharaId));
                bytes.AddRange(BitConverter.GetBytes(entry.BoneScaleIdx));
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
                bytes.AddRange(BitConverter.GetBytes(entry.F_48));
                bytes.AddRange(BitConverter.GetBytes(entry.F_52));
                bytes.AddRange(BitConverter.GetBytes(entry.F_56));
            }

            //validation
            if (bytes.Count != 16 + (60 * Entries.Count))
                throw new InvalidDataException($"Error on building ikd: Invalid file size!");

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
                    return IKD_ENTRY_SIZE;
                default:
                    throw new InvalidDataException($"IKD: This IKD version is not supported (Version: {version}).");
            }
        }

        public static int EntrySizeToVersion(int entrySize)
        {
            // Add more for when file ever updates
            switch (entrySize)
            {
                case IKD_ENTRY_SIZE:
                    return 1;
                default:
                    throw new InvalidDataException($"IKD: This IKD version is not supported (EntrySize: {entrySize}).");
            }
        }
        #endregion
    }

    [YAXSerializeAs("IkdEntry")]
    public class IKD_Entry : IInstallable
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
                return $"{CharaId}_{BoneScaleIdx}";
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 2)
                {
                    CharaId = ushort.Parse(split[0]);
                    BoneScaleIdx = ushort.Parse(split[1]);
                }
            }
        }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("CharaId")]
        public ushort CharaId { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("BoneScaleIdx")]
        public ushort BoneScaleIdx { get; set; }
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
    }
}
