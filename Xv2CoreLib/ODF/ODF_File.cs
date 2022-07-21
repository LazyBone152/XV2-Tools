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

        [YAXAttributeForClass]
        public ushort Version { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Header")]
        public List<ODF_SubHeader> SubHeader { get; set; }

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
            ushort version = BitConverter.ToUInt16(rawBytes, 6);

            if (version != 56 && version != 60)
            {
                throw new InvalidDataException("Unsupported ODF File version.");
            }

            ODF_File odfFile = new ODF_File();
            odfFile.Version = version;
            odfFile.SubHeader = new List<ODF_SubHeader>();

            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = 16;

            for(int i = 0; i < count; i++)
            {
                int subCount = BitConverter.ToInt32(rawBytes, offset + 0);
                int subOffset = BitConverter.ToInt32(rawBytes, offset + 4) + 16;
                ODF_SubHeader odfEntry = new ODF_SubHeader();
                odfEntry.Entries = new List<ODF_Entry>();

                for(int a = 0; a < subCount; a++)
                {
                    odfEntry.Entries.Add(ODF_Entry.Read(rawBytes, subOffset, version));

                    if (version == 56)
                        subOffset += 56;
                    else if (version == 60)
                        subOffset += 64;
                }

                odfFile.SubHeader.Add(odfEntry);
                offset += 16;
            }

            return odfFile;
        }

        public byte[] Write()
        {
            if (Version != 56 && Version != 60)
                throw new ArgumentException("ODF: Invalid Version. Only 56 and 60 are supported.");

            List<byte> bytes = new List<byte>();

            int odfEntryCount = (SubHeader != null) ? SubHeader.Count : 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(ODF_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(odfEntryCount));
            bytes.AddRange(BitConverter.GetBytes(0));

            //Odf Entries
            for(int i = 0; i < odfEntryCount; i++)
            {
                int subEntryCount = (SubHeader[i].Entries != null) ? SubHeader[i].Entries.Count : 0;
                bytes.AddRange(BitConverter.GetBytes(subEntryCount));
                bytes.AddRange(BitConverter.GetBytes(0)); //Offset
                bytes.AddRange(BitConverter.GetBytes((long)0)); //Padding
            }

            //Odf Sub Entries
            for(int i = 0; i < odfEntryCount; i++)
            {
                int subEntryCount = (SubHeader[i].Entries != null) ? SubHeader[i].Entries.Count : 0;

                if(subEntryCount > 0)
                {
                    //Fill in offset
                    int offsetToReplace = (i * 16) + 16 + 4;
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - 16), offsetToReplace);
                }

                for(int a = 0; a < subEntryCount; a++)
                {
                    bytes.AddRange(SubHeader[i].Entries[a].Write(Version));
                }
            }

            return bytes.ToArray();
        }
    }

    [YAXSerializeAs("Header")]
    public class ODF_SubHeader
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PartnerEntry")]
        public List<ODF_Entry> Entries { get; set; }


    }

    [YAXSerializeAs("PartnerEntry")]
    public class ODF_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Partner_ID")]
        public int I_00 { get; set; }
        [YAXAttributeFor("DefaultPartSet")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("ColorPartSet")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("StatSet")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("SuperSoul")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("SuperSkill1")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("SuperSkill2")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("SuperSkill3")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("SuperSKill4")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("UltimateSkill1")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("UltimateSkill2")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("EvasiveSkill")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("AwokenSkill")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("Menu_ID")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }

        //The following values are only in OCCDefaultTable.odf (ver 60)
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int I_60 { get; set; }

        public static ODF_Entry Read(byte[] rawBytes, int offset, ushort version)
        {
            ODF_Entry entry = new ODF_Entry()
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

            if(version == 60)
            {
                entry.I_56 = BitConverter.ToInt32(rawBytes, offset + 56);
                entry.I_60 = BitConverter.ToInt32(rawBytes, offset + 60);
            }

            return entry;
        }

        public byte[] Write(ushort version)
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

            if(version == 60)
            {
                bytes.AddRange(BitConverter.GetBytes(I_56));
                bytes.AddRange(BitConverter.GetBytes(I_60));
            }

            if (bytes.Count != 56 && version == 56) throw new InvalidDataException("ODF_SubEntry is an invalid size. Expected 56 bytes.");
            if (bytes.Count != 64 && version == 60) throw new InvalidDataException("ODF_SubEntry is an invalid size. Expected 64 bytes.");
            return bytes.ToArray();
        }
    }
}
