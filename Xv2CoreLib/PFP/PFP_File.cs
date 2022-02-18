using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.PFP
{
    public class PFP_File
    {
        public const int PFP_SIGNATURE = 1346785315;

        [YAXAttributeForClass]
        [YAXSerializeAs("Version")]
        public ushort I_06 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PFP_Entry")]
        public List<PFP_Entry> PfpEntries { get; set; }

        public static PFP_File Serialize(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);
            PFP_File pflFile = Read(rawBytes);

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(PFP_File));
                serializer.SerializeToFile(pflFile, path + ".xml");
            }

            return pflFile;
        }

        public static void Deserialize(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(PFP_File), YAXSerializationOptions.DontSerializeNullObjects);
            PFP_File pflFile = (PFP_File)serializer.DeserializeFromFile(xmlPath);

            byte[] bytes = pflFile.Write();
            File.WriteAllBytes(saveLocation, bytes);

        }


        public static PFP_File Read(byte[] rawBytes)
        {
            PFP_File pflFile = new PFP_File();
            pflFile.PfpEntries = new List<PFP_Entry>();
            pflFile.I_06 = BitConverter.ToUInt16(rawBytes, 6);
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = 12;

            for(int i = 0; i < count; i++)
            {
                pflFile.PfpEntries.Add(PFP_Entry.Read(rawBytes, offset));
                offset += 56;
            }

            return pflFile;
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int count = (PfpEntries != null) ? PfpEntries.Count : 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(PFP_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(count));

            //Entries
            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(PfpEntries[i].Write());
            }

            return bytes.ToArray();

        }
    }

    public class PFP_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("F_04")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("Str_40")]
        [YAXSerializeAs("value")]
        public string Str_40 { get; set; } //Length 16

        public static PFP_Entry Read(byte[] rawBytes, int offset)
        {
            return new PFP_Entry()
            {
                I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                Str_40 = StringEx.GetString(rawBytes, offset + 40, maxSize: 16)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(F_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(Utils.GetStringBytes(Str_40, 16));

            return bytes.ToArray();
        }
    }
}
