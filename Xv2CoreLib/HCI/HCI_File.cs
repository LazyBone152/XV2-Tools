using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.HCI
{
    [YAXSerializeAs("HCI")]
    public class HCI_File
    {
        private const uint HCI_SIGNATURE = 0x49484323;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "HciEntry")]
        public List<HCI_Entry> Entries { get; set; } = new List<HCI_Entry>();

        #region LoadSave
        public static HCI_File Parse(string path, bool writeXml)
        {
            HCI_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(HCI_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static HCI_File Parse(byte[] bytes)
        {
            if (BitConverter.ToUInt32(bytes, 0) != HCI_SIGNATURE) throw new InvalidDataException("HCI signature not found!");

            HCI_File hci = new HCI_File();

            int numEntries = BitConverter.ToInt32(bytes, 8);
            int offset = BitConverter.ToInt32(bytes, 12);

            for(int i = 0; i < numEntries; i++)
            {
                HCI_Entry entry = new HCI_Entry();

                entry.CharId = BitConverter.ToUInt16(bytes, offset + 0);
                entry.Costume = BitConverter.ToUInt16(bytes, offset + 2);
                entry.State1 = BitConverter.ToUInt16(bytes, offset + 4);
                entry.State2 = BitConverter.ToUInt16(bytes, offset + 6);
                entry.EmbIndex = BitConverter.ToInt32(bytes, offset + 8);
                entry.I_12 = BitConverter.ToInt32(bytes, offset + 12);

                offset += 16;
                hci.Entries.Add(entry);
            }

            return hci;
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .hci file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(HCI_File), YAXSerializationOptions.DontSerializeNullObjects);
            var oblFile = (HCI_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, oblFile.Write());
        }

        /// <summary>
        /// Save the HCI_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            if (Entries == null) Entries = new List<HCI_Entry>();

            List<byte> bytes = new List<byte>();

            //Header
            bytes.AddRange(BitConverter.GetBytes(HCI_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534)); //endianness
            bytes.AddRange(BitConverter.GetBytes((ushort)0)); //Pad
            bytes.AddRange(BitConverter.GetBytes(Entries.Count)); 
            bytes.AddRange(BitConverter.GetBytes(16)); //Offset to data, so always 0x10.

            //Entries
            foreach(var entry in Entries)
            {
                bytes.AddRange(BitConverter.GetBytes(entry.CharId));
                bytes.AddRange(BitConverter.GetBytes(entry.Costume));
                bytes.AddRange(BitConverter.GetBytes(entry.State1));
                bytes.AddRange(BitConverter.GetBytes(entry.State2));
                bytes.AddRange(BitConverter.GetBytes(entry.EmbIndex));
                bytes.AddRange(BitConverter.GetBytes(entry.I_12));
            }

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }

        #endregion

    }

    [YAXSerializeAs("HciEntry")]
    public class HCI_Entry : IInstallable
    {
        #region NonSerialized
        [YAXDontSerialize]
        public ushort CharId { get { return ushort.Parse(I_00); } set { I_00 = value.ToString(); } }
        [YAXDontSerialize]
        public int EmbIndex { get { return int.Parse(I_08); } set { I_08 = value.ToString(); } }

        //interface
        [YAXDontSerialize]
        public int SortID { get { return CharId; } }
        [YAXDontSerialize]
        public string Index 
        { 
            get 
            { 
                return $"{I_00}_{Costume}_{State1}_{State2}";
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 4)
                {
                    I_00 = split[0];
                    Costume = ushort.Parse(split[1]);
                    State1 = ushort.Parse(split[2]);
                    State2 = ushort.Parse(split[3]);
                }
            }
        }

        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("CharId")]
        public string I_00 { get; set; } //0
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public ushort Costume { get; set; } //2
        [YAXAttributeForClass]
        [YAXSerializeAs("State1")]
        public ushort State1 { get; set; } //4
        [YAXAttributeForClass]
        [YAXSerializeAs("State2")]
        public ushort State2 { get; set; } //6
        [YAXAttributeForClass]
        [YAXSerializeAs("EmbIndex")]
        public string I_08 { get; set; } //8
        [YAXAttributeForClass]
        [YAXSerializeAs("I_12")]
        public int I_12 { get; set; } //12
    }
}
