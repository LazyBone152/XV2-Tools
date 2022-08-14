using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.DML
{
    [YAXSerializeAs("DML")]
    public class DML_File
    {
        public const int DML_SIGNATURE = 1280132131;
        [YAXAttributeForClass]
        public ushort I_06 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "DML_Entry")]
        public List<DML_Entry> DML_Entries { get; set; }

        public static void ParseFile(string filePath)
        {
            var dmlFile = Load(filePath);
            YAXSerializer serializer = new YAXSerializer(typeof(DML_File));
            serializer.SerializeToFile(dmlFile, filePath + ".xml");
        }

        public static void LoadXmlAndSave(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(DML_File), YAXSerializationOptions.DontSerializeNullObjects);
            DML_File dmlFile = (DML_File)serializer.DeserializeFromFile(xmlPath);
            byte[] bytes = dmlFile.GetBytes();
            File.WriteAllBytes(saveLocation, bytes);
        }

        public static DML_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static DML_File Load(byte[] rawBytes)
        {
            //Init
            DML_File dmlFile = new DML_File() { DML_Entries = new List<DML_Entry>() };

            //Header
            dmlFile.I_06 = BitConverter.ToUInt16(rawBytes, 6);
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 12);
            
            //Entries
            for(int i = 0; i < count; i++)
            {
                dmlFile.DML_Entries.Add(new DML_Entry()
                {
                    Index = BitConverter.ToUInt16(rawBytes, offset + 0).ToString(),
                    Chapter_ID = BitConverter.ToUInt16(rawBytes, offset + 2),
                    StageRimlightID = BitConverter.ToUInt16(rawBytes, offset + 4),
                    I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    DemoNameMsgID = StringEx.GetString(rawBytes, offset + 16, false, StringEx.EncodingType.ASCII, 32),
                    DemoID = StringEx.GetString(rawBytes, offset + 48, false, StringEx.EncodingType.ASCII, 32),
                    Str_80 = StringEx.GetString(rawBytes, offset + 80, false, StringEx.EncodingType.ASCII, 32),
                    QuestID = StringEx.GetString(rawBytes, offset + 112, false, StringEx.EncodingType.ASCII, 32)
                });

                offset += 144;
            }

            return dmlFile;
            
        }

        public byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            int count = (DML_Entries != null) ? DML_Entries.Count : 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(DML_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(16));

            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(ushort.Parse(DML_Entries[i].Index)));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].Chapter_ID));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].StageRimlightID));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_06));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_08));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_10));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_12));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_14));
                bytes.AddRange(Utils.GetStringBytes(DML_Entries[i].DemoNameMsgID, 32));
                bytes.AddRange(Utils.GetStringBytes(DML_Entries[i].DemoID, 32));
                bytes.AddRange(Utils.GetStringBytes(DML_Entries[i].Str_80, 32));
                bytes.AddRange(Utils.GetStringBytes(DML_Entries[i].QuestID, 32));
            }

            return bytes.ToArray();
        }
    
        public byte[] SaveToBytes()
        {
            return GetBytes();
        }
    }

    public class DML_Entry : IInstallable
    {
        #region Install
        [YAXDontSerialize]
        public int SortID
        {
            get => Utils.TryParseInt(Index);
            set => Index = value.ToString();
        }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } //Ushort
        [YAXAttributeForClass]
        [YAXSerializeAs("Chapter_ID")]
        public ushort Chapter_ID { get; set; }
        [YAXAttributeFor("StageRimlightID")]
        [YAXSerializeAs("value")]
        public ushort StageRimlightID { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("DemoNameMsgId")]
        [YAXSerializeAs("value")]
        public string DemoNameMsgID { get; set; }
        [YAXAttributeFor("DemoID")]
        [YAXSerializeAs("value")]
        public string DemoID { get; set; }
        [YAXAttributeFor("Str_80")]
        [YAXSerializeAs("value")]
        public string Str_80 { get; set; }
        [YAXAttributeFor("QuestID")]
        [YAXSerializeAs("value")]
        public string QuestID { get; set; }
    }
}
