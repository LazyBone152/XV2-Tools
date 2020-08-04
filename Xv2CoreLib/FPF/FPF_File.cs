using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.FPF
{
    [YAXSerializeAs("FPF")]
    public class FPF_File
    {
        [YAXDontSerialize]
        public const int FPF_SIGNATURE = 1179665955;
        [YAXDontSerialize]
        public const int UnknownIndexListOffset = 112;
        [YAXDontSerialize]
        public const int UnknownIndexListCount = 60;
        [YAXDontSerialize]
        public const int EntryPointerListOffset = 352;
        [YAXDontSerialize]
        public const int EntryPointerListEntryCount = 70;
        [YAXDontSerialize]
        public const int EntryPointerListEntrySize = 8;

        [YAXAttributeForClass]
        [YAXSerializeAs("Version")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("CharacterID")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Costume")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("F_48")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("F_52")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("F_56")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public int I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public int I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public int I_72 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        public int I_76 { get; set; }
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("value")]
        public int I_80 { get; set; }
        [YAXAttributeFor("I_84")]
        [YAXSerializeAs("value")]
        public int I_84 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("value")]
        public int I_88 { get; set; }
        [YAXAttributeFor("I_92")]
        [YAXSerializeAs("value")]
        public int I_92 { get; set; }
        [YAXAttributeFor("F_96")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_96 { get; set; }
        [YAXAttributeFor("I_100")]
        [YAXSerializeAs("value")]
        public int I_100 { get; set; }
        [YAXAttributeFor("F_104")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_104 { get; set; }
        [YAXAttributeFor("I_108")]
        [YAXSerializeAs("value")]
        public int I_108 { get; set; }
        
        [YAXComment("Dont delete or add entries to this list (entry count must be 60).")]
        public Unknown_Indexes UnknownIndexes { get; set; }
        [YAXDontSerializeIfNull]
        public List<FPF_Entry> FpfEntries { get; set; }


        public static FPF_File Parse(string path, bool writeXml)
        {
            FPF_File fpfFile = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(FPF_File));
                serializer.SerializeToFile(fpfFile, path + ".xml");
            }

            return fpfFile;
        }

        public static FPF_File Parse(byte[] rawBytes)
        {
            FPF_File fpfFile = new FPF_File();

            //Header
            fpfFile.I_06 = BitConverter.ToUInt16(rawBytes, 6);
            fpfFile.I_08 = BitConverter.ToInt32(rawBytes, 8);
            fpfFile.I_12 = BitConverter.ToInt32(rawBytes, 12);
            fpfFile.F_16 = BitConverter.ToSingle(rawBytes, 16);
            fpfFile.F_20 = BitConverter.ToSingle(rawBytes, 20);
            fpfFile.F_24 = BitConverter.ToSingle(rawBytes, 24);
            fpfFile.F_28 = BitConverter.ToSingle(rawBytes, 28);
            fpfFile.F_32 = BitConverter.ToSingle(rawBytes, 32);
            fpfFile.F_36 = BitConverter.ToSingle(rawBytes, 36);
            fpfFile.I_40 = BitConverter.ToInt32(rawBytes, 40);
            fpfFile.I_44 = BitConverter.ToInt32(rawBytes, 44);
            fpfFile.F_48 = BitConverter.ToSingle(rawBytes, 48);
            fpfFile.F_52 = BitConverter.ToSingle(rawBytes, 52);
            fpfFile.F_56 = BitConverter.ToSingle(rawBytes, 56);
            fpfFile.I_60 = BitConverter.ToInt32(rawBytes, 60);
            fpfFile.I_64 = BitConverter.ToInt32(rawBytes, 64);
            fpfFile.I_68 = BitConverter.ToInt32(rawBytes, 68);
            fpfFile.I_72 = BitConverter.ToInt32(rawBytes, 72);
            fpfFile.I_76 = BitConverter.ToInt32(rawBytes, 76);
            fpfFile.I_80 = BitConverter.ToInt32(rawBytes, 80);
            fpfFile.I_84 = BitConverter.ToInt32(rawBytes, 84);
            fpfFile.I_88 = BitConverter.ToInt32(rawBytes, 88);
            fpfFile.I_92 = BitConverter.ToInt32(rawBytes, 92);
            fpfFile.F_96 = BitConverter.ToSingle(rawBytes, 96);
            fpfFile.I_100 = BitConverter.ToInt32(rawBytes, 100);
            fpfFile.F_104 = BitConverter.ToSingle(rawBytes, 104);
            fpfFile.I_108 = BitConverter.ToInt32(rawBytes, 108);

            //Unknown indexes
            fpfFile.UnknownIndexes = Unknown_Indexes.Read(rawBytes, UnknownIndexListOffset);

            //Entries
            fpfFile.FpfEntries = new List<FPF_Entry>();
            int offset = EntryPointerListOffset;
            for (int i = 0; i < EntryPointerListEntryCount; i++)
            {
                if(BitConverter.ToInt32(rawBytes, offset) != 0)
                {
                    fpfFile.FpfEntries.Add(FPF_Entry.Read(rawBytes, BitConverter.ToInt32(rawBytes, offset), i));
                }
                offset += EntryPointerListEntrySize;
            }

            //Return
            return fpfFile;
        }

        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(FPF_File), YAXSerializationOptions.DontSerializeNullObjects);
            var fpfFile = (FPF_File)serializer.DeserializeFromFile(xmlPath);
            var bytes = fpfFile.Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();
            List<int> entryOffsets = new List<int>();
            

            //Header
            bytes.AddRange(BitConverter.GetBytes(FPF_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));
            bytes.AddRange(BitConverter.GetBytes(F_32));
            bytes.AddRange(BitConverter.GetBytes(F_36));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(F_48));
            bytes.AddRange(BitConverter.GetBytes(F_52));
            bytes.AddRange(BitConverter.GetBytes(F_56));
            bytes.AddRange(BitConverter.GetBytes(I_60));
            bytes.AddRange(BitConverter.GetBytes(I_64));
            bytes.AddRange(BitConverter.GetBytes(I_68));
            bytes.AddRange(BitConverter.GetBytes(I_72));
            bytes.AddRange(BitConverter.GetBytes(I_76));
            bytes.AddRange(BitConverter.GetBytes(I_80));
            bytes.AddRange(BitConverter.GetBytes(I_84));
            bytes.AddRange(BitConverter.GetBytes(I_88));
            bytes.AddRange(BitConverter.GetBytes(I_92));
            bytes.AddRange(BitConverter.GetBytes(F_96));
            bytes.AddRange(BitConverter.GetBytes(I_100));
            bytes.AddRange(BitConverter.GetBytes(F_104));
            bytes.AddRange(BitConverter.GetBytes(I_108));

            //Unknown indexes
            bytes.AddRange(UnknownIndexes.Write());

            //FPF_Entries
            if (FpfEntries == null) FpfEntries = new List<FPF_Entry>();
            for(int i = 0; i < EntryPointerListEntryCount; i++)
            {
                var entry = FpfEntries.Find(a => a.ID == i);

                if(entry != null)
                {
                    entryOffsets.Add(bytes.Count);
                }

                bytes.AddRange(new byte[8]);
            }

            for(int i = 0; i < FpfEntries.Count; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), entryOffsets[i]);
                bytes.AddRange(FpfEntries[i].Write());
            }

            return bytes;
        }
        
    }

    public class Unknown_Indexes
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "UnknownIndex")]
        public List<int> Indexes { get; set; }

        public static Unknown_Indexes Read(byte[] rawBytes, int offset)
        {
            return new Unknown_Indexes()
            {
                Indexes = BitConverter_Ex.ToInt32Array(rawBytes, offset, FPF_File.UnknownIndexListCount).ToList()
            };
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter_Ex.GetBytes(Indexes.ToArray()));

            if (bytes.Count != FPF_File.UnknownIndexListCount * 4) throw new InvalidDataException("UnknownIndexes is an invalid size.");
            return bytes;
        }
    }

    public class FPF_Entry
    {
        [YAXAttributeForClass]
        public int ID { get; set; }
        [YAXAttributeForClass]
        public int I_00 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "FpfSubEntry")]
        public List<FPF_SubEntry> SubEntries { get; set; }

        public static FPF_Entry Read(byte[] rawBytes, int offset, int index)
        {
            return new FPF_Entry()
            {
                ID = index,
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                SubEntries = FPF_SubEntry.ReadAll(rawBytes, offset + 16, BitConverter.ToInt32(rawBytes, offset + 4))
            };
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();
            int subEntryCount = (SubEntries != null) ? SubEntries.Count : 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(subEntryCount));
            bytes.AddRange(new byte[8]);

            //Subentries
            for(int i = 0; i < subEntryCount; i++)
            {
                bytes.AddRange(SubEntries[i].Write());
            }

            //Return
            if (bytes.Count != (320 * subEntryCount) + 16) throw new InvalidDataException("FPF_Entry is an invalid size.");
            return bytes;
        }
    }

    [YAXSerializeAs("FpfSubEntry")]
    public class FPF_SubEntry
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        public TransformMatrix4x4 Transform1 { get; set; }
        public TransformMatrix4x4 Transform2 { get; set; }
        public TransformMatrix4x4 Transform3 { get; set; }
        public TransformMatrix4x4 Transform4 { get; set; }
        public TransformMatrix4x4 Transform5 { get; set; }

        public static List<FPF_SubEntry> ReadAll(byte[] rawBytes, int offset, int count)
        {
            List<FPF_SubEntry> entries = new List<FPF_SubEntry>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(rawBytes, offset, i));
                offset += 320;
            }

            return entries;
        }

        public static FPF_SubEntry Read(byte[] rawBytes, int offset, int index)
        {
            return new FPF_SubEntry()
            {
                Index = index,
                Transform1 = TransformMatrix4x4.Read(rawBytes, offset + 0),
                Transform2 = TransformMatrix4x4.Read(rawBytes, offset + 64),
                Transform3 = TransformMatrix4x4.Read(rawBytes, offset + 128),
                Transform4 = TransformMatrix4x4.Read(rawBytes, offset + 192),
                Transform5 = TransformMatrix4x4.Read(rawBytes, offset + 256)
            };
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(Transform1.Write());
            bytes.AddRange(Transform2.Write());
            bytes.AddRange(Transform3.Write());
            bytes.AddRange(Transform4.Write());
            bytes.AddRange(Transform5.Write());

            if (bytes.Count != 320) throw new InvalidDataException("FPF_SubEntry is an invalid size.");
            return bytes;
        }

    }

    public class TransformMatrix4x4
    {
        [YAXAttributeFor("F_00")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_00 { get; set; }
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
        [YAXAttributeFor("F_60")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_60 { get; set; }

        public static TransformMatrix4x4 Read(byte[] rawBytes, int offset)
        {
            return new TransformMatrix4x4()
            {
                F_00 = BitConverter.ToSingle(rawBytes, offset + 0),
                F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                F_52 = BitConverter.ToSingle(rawBytes, offset + 52),
                F_56 = BitConverter.ToSingle(rawBytes, offset + 56),
                F_60 = BitConverter.ToSingle(rawBytes, offset + 60)
            };
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(F_00));
            bytes.AddRange(BitConverter.GetBytes(F_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));
            bytes.AddRange(BitConverter.GetBytes(F_32));
            bytes.AddRange(BitConverter.GetBytes(F_36));
            bytes.AddRange(BitConverter.GetBytes(F_40));
            bytes.AddRange(BitConverter.GetBytes(F_44));
            bytes.AddRange(BitConverter.GetBytes(F_48));
            bytes.AddRange(BitConverter.GetBytes(F_52));
            bytes.AddRange(BitConverter.GetBytes(F_56));
            bytes.AddRange(BitConverter.GetBytes(F_60));

            if (bytes.Count != 64) throw new InvalidDataException("TransformMatrix4x4 is an invalid size.");
            return bytes;
        }

    }

}
