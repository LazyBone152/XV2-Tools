using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            //Init
            DML_File dmlFile = new DML_File() { DML_Entries = new List<DML_Entry>() };
            byte[] rawBytes = File.ReadAllBytes(path);
            List<byte> bytes = rawBytes.ToList();

            //Header
            dmlFile.I_06 = BitConverter.ToUInt16(rawBytes, 6);
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 12);
            
            //Entries
            for(int i = 0; i < count; i++)
            {
                dmlFile.DML_Entries.Add(new DML_Entry()
                {
                    I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                    I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    Str_16 = Utils.GetString(bytes, offset + 16, 32),
                    Str_48 = Utils.GetString(bytes, offset + 48, 32),
                    Str_80 = Utils.GetString(bytes, offset + 80, 32),
                    Str_112 = Utils.GetString(bytes, offset + 112, 32)
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
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_02));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_06));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_08));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_10));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_12));
                bytes.AddRange(BitConverter.GetBytes(DML_Entries[i].I_14));
                bytes.AddRange(Utils.GetStringBytes(DML_Entries[i].Str_16, 32));
                bytes.AddRange(Utils.GetStringBytes(DML_Entries[i].Str_48, 32));
                bytes.AddRange(Utils.GetStringBytes(DML_Entries[i].Str_80, 32));
                bytes.AddRange(Utils.GetStringBytes(DML_Entries[i].Str_112, 32));
            }

            return bytes.ToArray();
        }
    }

    public class DML_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
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
        [YAXAttributeFor("Str_16")]
        [YAXSerializeAs("value")]
        public string Str_16 { get; set; }
        [YAXAttributeFor("Str_48")]
        [YAXSerializeAs("value")]
        public string Str_48 { get; set; }
        [YAXAttributeFor("Str_80")]
        [YAXSerializeAs("value")]
        public string Str_80 { get; set; }
        [YAXAttributeFor("Str_112")]
        [YAXSerializeAs("value")]
        public string Str_112 { get; set; }
    }
}
