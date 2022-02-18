using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.PSL
{
    public class PSL_File
    {
        public const int PSL_SIGNATURE = 1280528419;

        [YAXAttributeForClass]
        [YAXSerializeAs("Version")]
        public ushort I_06 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PSL_Entry")]
        public List<PSL_Entry> PslEntries { get; set; }

        public static PSL_File Serialize(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);
            PSL_File pslFile = Read(rawBytes);

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(PSL_File));
                serializer.SerializeToFile(pslFile, path + ".xml");
            }

            return pslFile;
        }

        public static void Deserialize(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(PSL_File), YAXSerializationOptions.DontSerializeNullObjects);
            PSL_File pflFile = (PSL_File)serializer.DeserializeFromFile(xmlPath);

            byte[] bytes = pflFile.Write();
            File.WriteAllBytes(saveLocation, bytes);

        }


        public static PSL_File Read(byte[] rawBytes)
        {
            PSL_File pflFile = new PSL_File();
            pflFile.PslEntries = new List<PSL_Entry>();
            pflFile.I_06 = BitConverter.ToUInt16(rawBytes, 6);
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = 12;

            for(int i = 0; i < count; i++)
            {
                pflFile.PslEntries.Add(PSL_Entry.Read(rawBytes, offset));
                offset += 64;
            }

            return pflFile;
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int count = (PslEntries != null) ? PslEntries.Count : 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(PSL_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(count));

            //Entries
            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(PslEntries[i].Write());
            }

            return bytes.ToArray();

        }
    }

    public class PSL_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public ushort I_28 { get; set; }
        [YAXAttributeFor("I_30")]
        [YAXSerializeAs("value")]
        public ushort I_30 { get; set; }
        [YAXAttributeFor("Str_32")]
        [YAXSerializeAs("value")]
        public string Str_32 { get; set; } //Length 32

        public static PSL_Entry Read(byte[] rawBytes, int offset)
        {
            return new PSL_Entry()
            {
                I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                I_28 = BitConverter.ToUInt16(rawBytes, offset + 28),
                I_30 = BitConverter.ToUInt16(rawBytes, offset + 30),
                Str_32 = StringEx.GetString(rawBytes, offset + 32, maxSize: 32)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(I_30));
            bytes.AddRange(Utils.GetStringBytes(Str_32, 32));

            return bytes.ToArray();
        }
    }
}
