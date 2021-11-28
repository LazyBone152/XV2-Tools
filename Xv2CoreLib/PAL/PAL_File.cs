using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.PAL
{
    public class PAL_File : ISorting
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("I_06")]
        public ushort I_06 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PalEntry")]
        public List<PalEntry> PalEntries { get; set; } = new List<PalEntry>();

        public void SortEntries()
        {
            PalEntries.Sort((x, y) => x.SortID - y.SortID);
        }

        #region LoadSave

        public static PAL_File Parse(string path, bool writeXml)
        {
            PAL_File palFile = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(PAL_File));
                serializer.SerializeToFile(palFile, path + ".xml");
            }

            return palFile;
        }

        public static PAL_File Parse(byte[] bytes)
        {
            PAL_File palFile = new PAL_File();
            palFile.I_06 = BitConverter.ToUInt16(bytes, 6);

            int count = BitConverter.ToInt32(bytes, 8);
            int offset = BitConverter.ToInt32(bytes, 12);

            palFile.PalEntries = PalEntry.ReadAll(bytes, offset, count);

            List<Equipment> equipments = Equipment.ReadAll(bytes, offset + (10 * count), count);
            List<Stats> stats = Stats.ReadAll(bytes, offset + (10 * count) + (78 * count), count);

            for(int i = 0; i < palFile.PalEntries.Count; i++)
            {
                palFile.PalEntries[i].Equipment = equipments[i];
                palFile.PalEntries[i].Stats = stats[i];
            }

            return palFile;
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .pal file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(PAL_File), YAXSerializationOptions.DontSerializeNullObjects);
            var oblFile = (PAL_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, oblFile.Write());
        }

        /// <summary>
        /// Save the PAL_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }
        
        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            SortEntries();

            //Header
            bytes.AddRange(BitConverter.GetBytes((uint)1279348771)); //Signature "#PAL"
            bytes.AddRange(BitConverter.GetBytes((ushort)65534)); //Endianess
            bytes.AddRange(BitConverter.GetBytes((ushort)I_06)); //Unknown... but probably file version
            bytes.AddRange(BitConverter.GetBytes((uint)PalEntries?.Count));
            bytes.AddRange(BitConverter.GetBytes((uint)16));

            //Info entries
            bytes.AddRange(PalEntry.WriteAll(PalEntries));

            //Equipment entries
            foreach (var entry in PalEntries)
                bytes.AddRange(entry.Equipment.Write());

            //Stat entries
            foreach (var entry in PalEntries)
                bytes.AddRange(entry.Stats.Write());

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }
        
        #endregion
    }

    public class PalEntry : IInstallable
    {
        #region IInstallable
        [YAXDontSerialize]
        public int SortID { get { return I_00; } }
        [YAXDontSerialize]
        public string Index { get { return I_00.ToString(); } set { I_00 = ushort.Parse(value); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public ushort I_00 { get; set; } //uint16
        [YAXAttributeForClass]
        [YAXSerializeAs("NameID")]
        public ushort I_02 { get; set; } //uint16
        [YAXAttributeFor("CMS_ID")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; } //uint16
        [YAXAttributeFor("Voice")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; } //uint16
        [YAXAttributeFor("TeamMate")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; } //uint16

        [BindingSubClass]
        public Equipment Equipment { get; set; }
        [BindingSubClass]
        public Stats Stats { get; set; }

        public static List<PalEntry> ReadAll(byte[] bytes, int offset, int count)
        {
            List<PalEntry> entries = new List<PalEntry>();

            for (int i = 0; i < count; i++)
                entries.Add(Read(bytes, offset + (10 * i)));

            return entries;
        }

        public static PalEntry Read(byte[] bytes, int offset)
        {
            return new PalEntry()
            {
                I_00 = BitConverter.ToUInt16(bytes, offset + 0),
                I_02 = BitConverter.ToUInt16(bytes, offset + 2),
                I_04 = BitConverter.ToUInt16(bytes, offset + 4),
                I_06 = BitConverter.ToUInt16(bytes, offset + 6),
                I_08 = BitConverter.ToUInt16(bytes, offset + 8)
            };
        }

        public static byte[] WriteAll(IList<PalEntry> entries)
        {
            List<byte> bytes = new List<byte>();

            foreach (var entry in entries)
                bytes.AddRange(entry.Write());

            return bytes.ToArray();
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));

            return bytes.ToArray();
        }
    }

    public class Equipment
    {
        [YAXAttributeFor("FaceBase")]
        [YAXSerializeAs("value")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("FaceForehead")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("Eyes")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("Nose")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("Ears")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("Hair")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("Top")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("Bottom")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("Gloves")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("Shoes")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }
        [YAXAttributeFor("BodyShape")]
        [YAXSerializeAs("value")]
        public ushort I_20 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("SkinColor1")]
        [YAXSerializeAs("value")]
        public ushort I_24 { get; set; }
        [YAXAttributeFor("SkinColor2")]
        [YAXSerializeAs("value")]
        public ushort I_26 { get; set; }
        [YAXAttributeFor("SkinColor3")]
        [YAXSerializeAs("value")]
        public ushort I_28 { get; set; }
        [YAXAttributeFor("SkinColor4")]
        [YAXSerializeAs("value")]
        public ushort I_30 { get; set; }
        [YAXAttributeFor("HairColor")]
        [YAXSerializeAs("value")]
        public ushort I_32 { get; set; }
        [YAXAttributeFor("EyeColor")]
        [YAXSerializeAs("value")]
        public ushort I_34 { get; set; }
        [YAXAttributeFor("TopColor1")]
        [YAXSerializeAs("value")]
        public ushort I_36 { get; set; }
        [YAXAttributeFor("TopColor2")]
        [YAXSerializeAs("value")]
        public ushort I_38 { get; set; }
        [YAXAttributeFor("TopColor3")]
        [YAXSerializeAs("value")]
        public ushort I_40 { get; set; }
        [YAXAttributeFor("TopColor4")]
        [YAXSerializeAs("value")]
        public ushort I_42 { get; set; }
        [YAXAttributeFor("BottomColor1")]
        [YAXSerializeAs("value")]
        public ushort I_44 { get; set; }
        [YAXAttributeFor("BottomColor2")]
        [YAXSerializeAs("value")]
        public ushort I_46 { get; set; }
        [YAXAttributeFor("BottomColor3")]
        [YAXSerializeAs("value")]
        public ushort I_48 { get; set; }
        [YAXAttributeFor("BottomColor4")]
        [YAXSerializeAs("value")]
        public ushort I_50 { get; set; }
        [YAXAttributeFor("GlovesColor1")]
        [YAXSerializeAs("value")]
        public ushort I_52 { get; set; }
        [YAXAttributeFor("GlovesColor2")]
        [YAXSerializeAs("value")]
        public ushort I_54 { get; set; }
        [YAXAttributeFor("GlovesColor3")]
        [YAXSerializeAs("value")]
        public ushort I_56 { get; set; }
        [YAXAttributeFor("GlovesColor4")]
        [YAXSerializeAs("value")]
        public ushort I_58 { get; set; }
        [YAXAttributeFor("ShoesColor1")]
        [YAXSerializeAs("value")]
        public ushort I_60 { get; set; }
        [YAXAttributeFor("ShoesColor2")]
        [YAXSerializeAs("value")]
        public ushort I_62 { get; set; }
        [YAXAttributeFor("ShoesColor3")]
        [YAXSerializeAs("value")]
        public ushort I_64 { get; set; }
        [YAXAttributeFor("ShoesColor4")]
        [YAXSerializeAs("value")]
        public ushort I_66 { get; set; }
        [YAXAttributeFor("MakeupColor1")]
        [YAXSerializeAs("value")]
        public ushort I_68 { get; set; }
        [YAXAttributeFor("MakeupColor2")]
        [YAXSerializeAs("value")]
        public ushort I_70 { get; set; }
        [YAXAttributeFor("MakeupColor3")]
        [YAXSerializeAs("value")]
        public ushort I_72 { get; set; }
        [YAXAttributeFor("Accesory")]
        [YAXSerializeAs("value")]
        public ushort I_74 { get; set; }
        [YAXAttributeFor("Talisman")]
        [YAXSerializeAs("value")]
        public ushort I_76 { get; set; } //ushort
        
        public static List<Equipment> ReadAll(byte[] bytes, int offset, int count)
        {
            List<Equipment> entries = new List<Equipment>();

            for (int i = 0; i < count; i++)
                entries.Add(Read(bytes, offset + (78 * i)));

            return entries;
        }

        public static Equipment Read(byte[] bytes, int offset)
        {
            return new Equipment()
            {
                I_00 = BitConverter.ToUInt16(bytes, offset + 0),
                I_02 = BitConverter.ToUInt16(bytes, offset + 2),
                I_04 = BitConverter.ToUInt16(bytes, offset + 4),
                I_06 = BitConverter.ToUInt16(bytes, offset + 6),
                I_08 = BitConverter.ToUInt16(bytes, offset + 8),
                I_10 = BitConverter.ToUInt16(bytes, offset + 10),
                I_12 = BitConverter.ToUInt16(bytes, offset + 12),
                I_14 = BitConverter.ToUInt16(bytes, offset + 14),
                I_16 = BitConverter.ToUInt16(bytes, offset + 16),
                I_18 = BitConverter.ToUInt16(bytes, offset + 18),
                I_20 = BitConverter.ToUInt16(bytes, offset + 20),
                I_22 = BitConverter.ToUInt16(bytes, offset + 22),
                I_24 = BitConverter.ToUInt16(bytes, offset + 24),
                I_26 = BitConverter.ToUInt16(bytes, offset + 26),
                I_28 = BitConverter.ToUInt16(bytes, offset + 28),
                I_30 = BitConverter.ToUInt16(bytes, offset + 30),
                I_32 = BitConverter.ToUInt16(bytes, offset + 32),
                I_34 = BitConverter.ToUInt16(bytes, offset + 34),
                I_36 = BitConverter.ToUInt16(bytes, offset + 36),
                I_38 = BitConverter.ToUInt16(bytes, offset + 38),
                I_40 = BitConverter.ToUInt16(bytes, offset + 40),
                I_42 = BitConverter.ToUInt16(bytes, offset + 42),
                I_44 = BitConverter.ToUInt16(bytes, offset + 44),
                I_46 = BitConverter.ToUInt16(bytes, offset + 46),
                I_48 = BitConverter.ToUInt16(bytes, offset + 48),
                I_50 = BitConverter.ToUInt16(bytes, offset + 50),
                I_52 = BitConverter.ToUInt16(bytes, offset + 52),
                I_54 = BitConverter.ToUInt16(bytes, offset + 54),
                I_56 = BitConverter.ToUInt16(bytes, offset + 56),
                I_58 = BitConverter.ToUInt16(bytes, offset + 58),
                I_60 = BitConverter.ToUInt16(bytes, offset + 60),
                I_62 = BitConverter.ToUInt16(bytes, offset + 62),
                I_64 = BitConverter.ToUInt16(bytes, offset + 64),
                I_66 = BitConverter.ToUInt16(bytes, offset + 66),
                I_68 = BitConverter.ToUInt16(bytes, offset + 68),
                I_70 = BitConverter.ToUInt16(bytes, offset + 70),
                I_72 = BitConverter.ToUInt16(bytes, offset + 72),
                I_74 = BitConverter.ToUInt16(bytes, offset + 74),
                I_76 = BitConverter.ToUInt16(bytes, offset + 76)
            };
        }
        
        public static byte[] WriteAll(IList<Equipment> entries)
        {
            List<byte> bytes = new List<byte>();

            foreach (var entry in entries)
                bytes.AddRange(entry.Write());

            return bytes.ToArray();
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_10));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_14));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_18));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_22));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_26));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(I_30));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(I_34));
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_38));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_42));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(I_46));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(I_50));
            bytes.AddRange(BitConverter.GetBytes(I_52));
            bytes.AddRange(BitConverter.GetBytes(I_54));
            bytes.AddRange(BitConverter.GetBytes(I_56));
            bytes.AddRange(BitConverter.GetBytes(I_58));
            bytes.AddRange(BitConverter.GetBytes(I_60));
            bytes.AddRange(BitConverter.GetBytes(I_62));
            bytes.AddRange(BitConverter.GetBytes(I_64));
            bytes.AddRange(BitConverter.GetBytes(I_66));
            bytes.AddRange(BitConverter.GetBytes(I_68));
            bytes.AddRange(BitConverter.GetBytes(I_70));
            bytes.AddRange(BitConverter.GetBytes(I_72));
            bytes.AddRange(BitConverter.GetBytes(I_74));
            bytes.AddRange(BitConverter.GetBytes(I_76));

            return bytes.ToArray();
        }
    }

    public class Stats
    {
        [YAXAttributeFor("Level")]
        [YAXSerializeAs("value")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("Health")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("Ki")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("Stamina")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("Attack")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("Strike")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("Blast")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("SuperSkill1")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; } //ushort
        [YAXAttributeFor("SuperSkill2")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; } //ushort
        [YAXAttributeFor("SuperSkill3")]
        [YAXSerializeAs("value")]
        public ushort I_20 { get; set; } //ushort
        [YAXAttributeFor("SuperSkill4")]
        [YAXSerializeAs("value")]
        public ushort I_22 { get; set; } //ushort
        [YAXAttributeFor("UltimateSkill1")]
        [YAXSerializeAs("value")]
        public ushort I_24 { get; set; } //ushort
        [YAXAttributeFor("UltimateSkill2")]
        [YAXSerializeAs("value")]
        public ushort I_26 { get; set; } //ushort
        [YAXAttributeFor("EvasiveSkill")]
        [YAXSerializeAs("value")]
        public ushort I_28 { get; set; } //ushort
        [YAXAttributeFor("BlastSkill")]
        [YAXSerializeAs("value")]
        public ushort I_30 { get; set; } //ushort
        [YAXAttributeFor("AwokenSkill")]
        [YAXSerializeAs("value")]
        public ushort I_32 { get; set; } //ushort

        public Stats()
        {
            //Set default stats
            I_00 = 1;
            I_14 = 100;
            I_16 = ushort.MaxValue;
            I_18 = ushort.MaxValue;
            I_20 = ushort.MaxValue;
            I_22 = ushort.MaxValue;
            I_24 = ushort.MaxValue;
            I_26 = ushort.MaxValue;
            I_28 = ushort.MaxValue;
            I_30 = ushort.MaxValue;
            I_32 = ushort.MaxValue;
        }

        public static List<Stats> ReadAll(byte[] bytes, int offset, int count)
        {
            List<Stats> entries = new List<Stats>();

            for (int i = 0; i < count; i++)
                entries.Add(Read(bytes, offset + (34 * i)));

            return entries;
        }

        public static Stats Read(byte[] bytes, int offset)
        {
            if(offset < bytes.Length)
            {
                return new Stats()
                {
                    I_00 = BitConverter.ToUInt16(bytes, offset + 0),
                    I_02 = BitConverter.ToUInt16(bytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(bytes, offset + 4),
                    I_06 = BitConverter.ToUInt16(bytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(bytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(bytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(bytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(bytes, offset + 14),
                    I_16 = BitConverter.ToUInt16(bytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(bytes, offset + 18),
                    I_20 = BitConverter.ToUInt16(bytes, offset + 20),
                    I_22 = BitConverter.ToUInt16(bytes, offset + 22),
                    I_24 = BitConverter.ToUInt16(bytes, offset + 24),
                    I_26 = BitConverter.ToUInt16(bytes, offset + 26),
                    I_28 = BitConverter.ToUInt16(bytes, offset + 28),
                    I_30 = BitConverter.ToUInt16(bytes, offset + 30),
                    I_32 = BitConverter.ToUInt16(bytes, offset + 32)
                };
            }
            else
            {
                //Entry has no stats, since apparantly this is a thing now...
                return new Stats();
            }
        }

        public static byte[] WriteAll(IList<Stats> entries)
        {
            List<byte> bytes = new List<byte>();

            foreach (var entry in entries)
                bytes.AddRange(entry.Write());

            return bytes.ToArray();
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_10));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_14));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_18));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_22));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_26));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(I_30));
            bytes.AddRange(BitConverter.GetBytes(I_32));

            return bytes.ToArray();
        }
    }

}
