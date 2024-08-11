using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.PSO
{
    public class PSO_File
    {
        public const int PSO_SIGNATURE = 1330860067;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PSO_Entry")]
        public List<PSO_Entry> PsoEntries { get; set; }

        public static PSO_File Serialize(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);
            PSO_File odfFile = Read(rawBytes);

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(PSO_File));
                serializer.SerializeToFile(odfFile, path + ".xml");
            }

            return odfFile;
        }

        public static void Deserialize(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(PSO_File), YAXSerializationOptions.DontSerializeNullObjects);
            PSO_File odfFile = (PSO_File)serializer.DeserializeFromFile(xmlPath);

            byte[] bytes = odfFile.Write();
            File.WriteAllBytes(saveLocation, bytes);

        }

        public void Save(string saveLocation)
        {
            byte[] bytes = this.Write();
            File.WriteAllBytes(saveLocation, bytes);
        }


        public static PSO_File Read(byte[] rawBytes)
        {
            if(BitConverter.ToUInt16(rawBytes, 6) != 120)
            {
                throw new InvalidDataException("Unsupported PSO File version.");
            }

            PSO_File psoFile = new PSO_File();
            psoFile.PsoEntries = new List<PSO_Entry>();

            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = 16;

            for(int i = 0; i < count; i++)
            {
                int subCount = BitConverter.ToInt32(rawBytes, offset + 0);
                int subOffset = BitConverter.ToInt32(rawBytes, offset + 4) + 16;
                PSO_Entry psoEntry = new PSO_Entry();
                psoEntry.PsoSubEntries = new List<PSO_SubEntry>();

                for(int a = 0; a < subCount; a++)
                {
                    psoEntry.PsoSubEntries.Add(PSO_SubEntry.Read(rawBytes, subOffset));
                    subOffset += 120;
                }

                psoFile.PsoEntries.Add(psoEntry);
                offset += 16;
            }

            return psoFile;
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int odfEntryCount = (PsoEntries != null) ? PsoEntries.Count : 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(PSO_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)120));
            bytes.AddRange(BitConverter.GetBytes(odfEntryCount));
            bytes.AddRange(BitConverter.GetBytes(0));

            //Odf Entries
            for(int i = 0; i < odfEntryCount; i++)
            {
                int subEntryCount = (PsoEntries[i].PsoSubEntries != null) ? PsoEntries[i].PsoSubEntries.Count : 0;
                bytes.AddRange(BitConverter.GetBytes(subEntryCount));
                bytes.AddRange(BitConverter.GetBytes(0)); //Offset
                bytes.AddRange(BitConverter.GetBytes((long)0)); //Padding
            }

            //Odf Sub Entries
            for(int i = 0; i < odfEntryCount; i++)
            {
                int subEntryCount = (PsoEntries[i].PsoSubEntries != null) ? PsoEntries[i].PsoSubEntries.Count : 0;

                if(subEntryCount > 0)
                {
                    //Fill in offset
                    int offsetToReplace = (i * 16) + 16 + 4;
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - 16), offsetToReplace);
                }

                for(int a = 0; a < subEntryCount; a++)
                {
                    bytes.AddRange(PsoEntries[i].PsoSubEntries[a].Write());
                }
            }

            return bytes.ToArray();
        }
    }

    public class PSO_Entry
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PSO_SubEntry")]
        public List<PSO_SubEntry> PsoSubEntries { get; set; }


    }

    public class PSO_SubEntry : IInstallable
    {
        #region Install
        [YAXDontSerialize]
        public string Index
        {
            set => I_00 = Utils.TryParseInt(value);
            get => I_00.ToString();
        }

        [YAXDontSerialize]
        public int SortID => I_00;
        #endregion
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int I_00 { get; set; }
        [YAXAttributeFor("F_04")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_04 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        public float F_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("F_52")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_52 { get; set; }
        [YAXAttributeFor("F_56")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_56 { get; set; }
        [YAXAttributeFor("F_60")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_60 { get; set; }
        [YAXAttributeFor("F_64")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_64 { get; set; }
        [YAXAttributeFor("F_68")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_68 { get; set; }
        [YAXAttributeFor("F_72")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_72 { get; set; }
        [YAXAttributeFor("F_76")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_76 { get; set; }
        [YAXAttributeFor("F_80")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_80 { get; set; }
        [YAXAttributeFor("F_84")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_84 { get; set; }
        [YAXAttributeFor("F_88")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_88 { get; set; }
        [YAXAttributeFor("F_92")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_92 { get; set; }
        [YAXAttributeFor("F_96")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_96 { get; set; }
        [YAXAttributeFor("I_100")]
        [YAXSerializeAs("value")]
        public int I_100 { get; set; }
        [YAXAttributeFor("I_104")]
        [YAXSerializeAs("value")]
        public int I_104 { get; set; }
        [YAXAttributeFor("I_108")]
        [YAXSerializeAs("value")]
        public int I_108 { get; set; }
        [YAXAttributeFor("I_112")]
        [YAXSerializeAs("value")]
        public int I_112 { get; set; }
        [YAXAttributeFor("F_116")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##############")]
        public float F_116 { get; set; }

        public static PSO_SubEntry Read(byte[] rawBytes, int offset)
        {
            return new PSO_SubEntry()
            {
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                F_52 = BitConverter.ToSingle(rawBytes, offset + 52),
                F_56 = BitConverter.ToSingle(rawBytes, offset + 56),
                F_60 = BitConverter.ToSingle(rawBytes, offset + 60),
                F_64 = BitConverter.ToSingle(rawBytes, offset + 64),
                F_68 = BitConverter.ToSingle(rawBytes, offset + 68),
                F_72 = BitConverter.ToSingle(rawBytes, offset + 72),
                F_76 = BitConverter.ToSingle(rawBytes, offset + 76),
                F_80 = BitConverter.ToSingle(rawBytes, offset + 80),
                F_84 = BitConverter.ToSingle(rawBytes, offset + 84),
                F_88 = BitConverter.ToSingle(rawBytes, offset + 88),
                F_92 = BitConverter.ToSingle(rawBytes, offset + 92),
                F_96 = BitConverter.ToSingle(rawBytes, offset + 96),
                I_100 = BitConverter.ToInt32(rawBytes, offset + 100),
                I_104 = BitConverter.ToInt32(rawBytes, offset + 104),
                I_108 = BitConverter.ToInt32(rawBytes, offset + 108),
                I_112 = BitConverter.ToInt32(rawBytes, offset + 112),
                F_116 = BitConverter.ToSingle(rawBytes, offset + 116)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(F_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(F_36));
            bytes.AddRange(BitConverter.GetBytes(F_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(F_52));
            bytes.AddRange(BitConverter.GetBytes(F_56));
            bytes.AddRange(BitConverter.GetBytes(F_60));
            bytes.AddRange(BitConverter.GetBytes(F_64));
            bytes.AddRange(BitConverter.GetBytes(F_68));
            bytes.AddRange(BitConverter.GetBytes(F_72));
            bytes.AddRange(BitConverter.GetBytes(F_76));
            bytes.AddRange(BitConverter.GetBytes(F_80));
            bytes.AddRange(BitConverter.GetBytes(F_84));
            bytes.AddRange(BitConverter.GetBytes(F_88));
            bytes.AddRange(BitConverter.GetBytes(F_92));
            bytes.AddRange(BitConverter.GetBytes(F_96));
            bytes.AddRange(BitConverter.GetBytes(I_100));
            bytes.AddRange(BitConverter.GetBytes(I_104));
            bytes.AddRange(BitConverter.GetBytes(I_108));
            bytes.AddRange(BitConverter.GetBytes(I_112));
            bytes.AddRange(BitConverter.GetBytes(F_116));

            if (bytes.Count != 120) throw new InvalidDataException("PSO_SubEntry is an invalid size. Expected 120 bytes.");
            return bytes.ToArray();
        }
    }
}
