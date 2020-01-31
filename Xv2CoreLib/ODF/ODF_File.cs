using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.ODF
{
    public class ODF_File
    {
        public const int ODF_SIGNATURE = 1178881827;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ODF_Entry")]
        public List<ODF_Entry> OdfEntries { get; set; }

        public static ODF_File Serialize(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);
            ODF_File odfFile = Read(rawBytes);

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(ODF_File));
                serializer.SerializeToFile(odfFile, path + ".xml");
            }

            return odfFile;
        }

        public static void Deserialize(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(ODF_File), YAXSerializationOptions.DontSerializeNullObjects);
            ODF_File odfFile = (ODF_File)serializer.DeserializeFromFile(xmlPath);

            byte[] bytes = odfFile.Write();
            File.WriteAllBytes(saveLocation, bytes);

        }

        public void Save(string saveLocation)
        {
            byte[] bytes = this.Write();
            File.WriteAllBytes(saveLocation, bytes);
        }


        public static ODF_File Read(byte[] rawBytes)
        {
            if (BitConverter.ToUInt16(rawBytes, 6) != 56)
            {
                throw new InvalidDataException("Unsupported ODF File version.");
            }

            ODF_File odfFile = new ODF_File();
            odfFile.OdfEntries = new List<ODF_Entry>();

            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = 16;

            for(int i = 0; i < count; i++)
            {
                int subCount = BitConverter.ToInt32(rawBytes, offset + 0);
                int subOffset = BitConverter.ToInt32(rawBytes, offset + 4) + 16;
                ODF_Entry odfEntry = new ODF_Entry();
                odfEntry.OdfSubEntries = new List<ODF_SubEntry>();

                for(int a = 0; a < subCount; a++)
                {
                    odfEntry.OdfSubEntries.Add(ODF_SubEntry.Read(rawBytes, subOffset));
                    subOffset += 56;
                }

                odfFile.OdfEntries.Add(odfEntry);
                offset += 16;
            }

            return odfFile;
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int odfEntryCount = (OdfEntries != null) ? OdfEntries.Count : 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(ODF_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)56));
            bytes.AddRange(BitConverter.GetBytes(odfEntryCount));
            bytes.AddRange(BitConverter.GetBytes(0));

            //Odf Entries
            for(int i = 0; i < odfEntryCount; i++)
            {
                int subEntryCount = (OdfEntries[i].OdfSubEntries != null) ? OdfEntries[i].OdfSubEntries.Count : 0;
                bytes.AddRange(BitConverter.GetBytes(subEntryCount));
                bytes.AddRange(BitConverter.GetBytes(0)); //Offset
                bytes.AddRange(BitConverter.GetBytes((long)0)); //Padding
            }

            //Odf Sub Entries
            for(int i = 0; i < odfEntryCount; i++)
            {
                int subEntryCount = (OdfEntries[i].OdfSubEntries != null) ? OdfEntries[i].OdfSubEntries.Count : 0;

                if(subEntryCount > 0)
                {
                    //Fill in offset
                    int offsetToReplace = (i * 16) + 16 + 4;
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - 16), offsetToReplace);
                }

                for(int a = 0; a < subEntryCount; a++)
                {
                    bytes.AddRange(OdfEntries[i].OdfSubEntries[a].Write());
                }
            }

            return bytes.ToArray();
        }
    }

    public class ODF_Entry
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ODF_SubEntry")]
        public List<ODF_SubEntry> OdfSubEntries { get; set; }


    }

    public class ODF_SubEntry
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }

        public static ODF_SubEntry Read(byte[] rawBytes, int offset)
        {
            return new ODF_SubEntry()
            {
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                I_52 = BitConverter.ToInt32(rawBytes, offset + 52)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(I_52));

            if (bytes.Count != 56) throw new InvalidDataException("ODF_SubEntry is an invalid size. Expected 56 bytes.");
            return bytes.ToArray();
        }
    }
}
