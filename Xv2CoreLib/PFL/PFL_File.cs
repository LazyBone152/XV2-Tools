using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.PFL
{
    public class PFL_File
    {
        public const int PFL_SIGNATURE = 1279676451;

        [YAXAttributeForClass]
        [YAXSerializeAs("Version")]
        public ushort I_06 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PFL_Entry")]
        public List<PFL_Entry> PflEntries { get; set; }

        public static PFL_File Serialize(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);
            List<byte> bytes = rawBytes.ToList();
            PFL_File pflFile = Read(rawBytes, bytes);

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(PFL_File));
                serializer.SerializeToFile(pflFile, path + ".xml");
            }

            return pflFile;
        }

        public static void Deserialize(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(PFL_File), YAXSerializationOptions.DontSerializeNullObjects);
            PFL_File pflFile = (PFL_File)serializer.DeserializeFromFile(xmlPath);

            byte[] bytes = pflFile.Write();
            File.WriteAllBytes(saveLocation, bytes);

        }


        public static PFL_File Read(byte[] rawBytes, List<byte> bytes)
        {
            PFL_File pflFile = new PFL_File();
            pflFile.PflEntries = new List<PFL_Entry>();
            pflFile.I_06 = BitConverter.ToUInt16(rawBytes, 6);
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = 12;

            for(int i = 0; i < count; i++)
            {
                pflFile.PflEntries.Add(PFL_Entry.Read(rawBytes, bytes, offset));
                offset += 48;
            }

            return pflFile;
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int count = (PflEntries != null) ? PflEntries.Count : 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(PFL_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(count));

            //Entries
            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(PflEntries[i].Write());
            }

            return bytes.ToArray();

        }
    }

    public class PFL_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("Texture")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("TP_Medals")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Unlocked")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("Str_16")]
        [YAXSerializeAs("value")]
        public string Str_16 { get; set; } //Length 32

        public static PFL_Entry Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new PFL_Entry()
            {
                I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                Str_16 = Utils.GetString(bytes, offset + 16, 32)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(Utils.GetStringBytes(Str_16, 32));

            return bytes.ToArray();
        }
    }
}
