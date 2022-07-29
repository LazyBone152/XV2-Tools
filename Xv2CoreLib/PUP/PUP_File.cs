using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.PUP
{
    [YAXSerializeAs("PUP")]
    public class PUP_File : ISorting
    {
        public const int PUP_SIGNATURE = 1347768355;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PUP_Entry")]
        public List<PUP_Entry> PupEntries { get; set; } = new List<PUP_Entry>();

        public void SortEntries()
        {
            if(PupEntries != null)
                PupEntries.Sort((x, y) => x.SortID - y.SortID);
        }

        public static PUP_File Serialize(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);

            PUP_File file = Load(rawBytes);

            //Write Xml
            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(PUP_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }
        
        public static void Deserialize(string xmlPath)
        {
            string path = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(PUP_File), YAXSerializationOptions.DontSerializeNullObjects);
            Write((PUP_File)serializer.DeserializeFromFile(xmlPath), path);
        }

        public static void Write(PUP_File file, string path)
        {
            byte[] bytes = file.SaveToBytes();

            //Saving
            File.WriteAllBytes(path, bytes.ToArray());
        }

        public static PUP_File Load(byte[] bytes)
        {
            PUP_File pupFile = new PUP_File();

            int count = BitConverter.ToInt32(bytes, 8);
            int offset = 16;

            for(int i = 0; i < count; i++)
            {
                pupFile.PupEntries.Add(PUP_Entry.Read(bytes, offset));
                offset += 152;
            }

            return pupFile;
        }

        public byte[] SaveToBytes()
        {
            List<byte> bytes = new List<byte>();

            int count = (PupEntries != null) ? PupEntries.Count : 0;
            SortEntries();

            //Header
            bytes.AddRange(BitConverter.GetBytes(PUP_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)16));
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(new byte[4]);

            //PupEntries
            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(PupEntries[i].Write());
            }

            return bytes.ToArray();
        }


        #region Helper
        public List<PUP_Entry> GetSequence(ushort id, ushort count)
        {
            List<PUP_Entry> entries = new List<PUP_Entry>();
            if (id == ushort.MaxValue || count == ushort.MaxValue) return entries;

            for(int i = id; i < id + count; i++)
            {
                var entry = GetEntry(i);
                if (entry != null)
                {
                    entries.Add(entry);
                }
                else
                {
                    entries.Add(PUP_Entry.GetEmptyEntry(i));
                }
            }

            return entries;
        }

        public PUP_Entry GetEntry(int id)
        {
            return PupEntries.FirstOrDefault(p => p.ID == id);
        }
    
        public int GetEntryId(PUP_Entry entry)
        {
            foreach(var _entry in PupEntries)
            {
                if (_entry.CompareEntry(entry)) return _entry.ID;
            }

            return -1;
        }

        public int CheckForSequence(IList<PUP_Entry> entries)
        {
            if(entries.Count > 0)
            {
                int id = GetEntryId(entries[0]);

                if(id != -1)
                {
                    for (int i = 1; i < entries.Count; i++)
                    {
                        PUP_Entry existingEntry = GetEntry(id + i);

                        if (existingEntry == null) return -1;
                        if (!existingEntry.CompareEntry(entries[i])) return -1;
                    }

                    return id;
                }
            }

            return -1;
        }

        public void AddEntry(PUP_Entry entry, int id)
        {
            //Remove existing
            var existing = PupEntries.FirstOrDefault(x => x.ID == id);
            if (existing != null) PupEntries.Remove(existing);

            //Add
            entry.ID = id;
            PupEntries.Add(entry);
        }
        
        public static void SetPupId(IList<PUP_Entry> entries, int id)
        {
            if (entries?.Count == 0) return;

            for (int i = 0; i < entries.Count; i++)
                entries[i].ID = id + i;
        }

        public int GetNewPupId(int count)
        {
            int min = 500;

            while (PupEntries.Any(x => x.ID >= min && x.ID <= min + count))
                min++;

            return min;
        }
        #endregion
    }

    [Serializable]
    public class PUP_Entry : IInstallable
    {

        #region WrappedProperties
        [YAXDontSerialize]
        public int SortID { get { return  int.Parse(Index);  } }
        [YAXDontSerialize]
        public int ID { get { return int.Parse(Index); }  set { Index = value.ToString(); } }

        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } //Int32, offset 0
        [YAXAttributeFor("CMN_Effect_ID")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; } = -1;
        [YAXAttributeFor("Talisman_ID_1")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; } = -1;
        [YAXAttributeFor("Talisman_ID_2")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; } = -1;
        [YAXAttributeFor("HEA")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("KI")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("KI_Recovery")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("STM")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Stamina_Recovery")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Enemy_Stamina_Eraser")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Stamima_Eraser")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("F_48")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("ATK")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("KI_BLA")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("STR")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("BLA")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("ATK_Damage")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("KI_BLA_Damage")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_72 { get; set; }
        [YAXAttributeFor("STR_Damage")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_76 { get; set; }
        [YAXAttributeFor("BLA_Damage")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_80 { get; set; }
        [YAXAttributeFor("Ground_Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_84 { get; set; }
        [YAXAttributeFor("Air_Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_88 { get; set; }
        [YAXAttributeFor("Boosting_Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_92 { get; set; }
        [YAXAttributeFor("Dash_Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_96 { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_100 { get; set; }
        [YAXAttributeFor("F_104")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_104 { get; set; }
        [YAXAttributeFor("F_108")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_108 { get; set; }
        [YAXAttributeFor("F_112")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_112 { get; set; }
        [YAXAttributeFor("F_116")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_116 { get; set; }
        [YAXAttributeFor("F_120")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_120 { get; set; }
        [YAXAttributeFor("F_124")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_124 { get; set; }
        [YAXAttributeFor("F_128")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_128 { get; set; }
        [YAXAttributeFor("F_132")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_132 { get; set; }
        [YAXAttributeFor("F_136")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_136 { get; set; }
        [YAXAttributeFor("F_140")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_140 { get; set; }
        [YAXAttributeFor("F_144")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_144 { get; set; }
        [YAXAttributeFor("F_148")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_148 { get; set; }

        public PUP_Entry() { }

        public PUP_Entry(int id)
        {
            ID = id;
        }

        public static PUP_Entry Read(byte[] bytes, int offset)
        {
            return new PUP_Entry()
            {
                Index = BitConverter.ToInt32(bytes, offset + 0).ToString(),
                I_04 = BitConverter.ToInt32(bytes, offset + 4),
                I_08 = BitConverter.ToInt32(bytes, offset + 8),
                I_12 = BitConverter.ToInt32(bytes, offset + 12),
                F_16 = BitConverter.ToSingle(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24),
                F_28 = BitConverter.ToSingle(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                F_36 = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44),
                F_48 = BitConverter.ToSingle(bytes, offset + 48),
                F_52 = BitConverter.ToSingle(bytes, offset + 52),
                F_56 = BitConverter.ToSingle(bytes, offset + 56),
                F_60 = BitConverter.ToSingle(bytes, offset + 60),
                F_64 = BitConverter.ToSingle(bytes, offset + 64),
                F_68 = BitConverter.ToSingle(bytes, offset + 68),
                F_72 = BitConverter.ToSingle(bytes, offset + 72),
                F_76 = BitConverter.ToSingle(bytes, offset + 76),
                F_80 = BitConverter.ToSingle(bytes, offset + 80),
                F_84 = BitConverter.ToSingle(bytes, offset + 84),
                F_88 = BitConverter.ToSingle(bytes, offset + 88),
                F_92 = BitConverter.ToSingle(bytes, offset + 92),
                F_96 = BitConverter.ToSingle(bytes, offset + 96),
                F_100 = BitConverter.ToSingle(bytes, offset + 100),
                F_104 = BitConverter.ToSingle(bytes, offset + 104),
                F_108 = BitConverter.ToSingle(bytes, offset + 108),
                F_112 = BitConverter.ToSingle(bytes, offset + 112),
                F_116 = BitConverter.ToSingle(bytes, offset + 116),
                F_120 = BitConverter.ToSingle(bytes, offset + 120),
                F_124 = BitConverter.ToSingle(bytes, offset + 124),
                F_128 = BitConverter.ToSingle(bytes, offset + 128),
                F_132 = BitConverter.ToSingle(bytes, offset + 132),
                F_136 = BitConverter.ToSingle(bytes, offset + 136),
                F_140 = BitConverter.ToSingle(bytes, offset + 140),
                F_144 = BitConverter.ToSingle(bytes, offset + 144),
                F_148 = BitConverter.ToSingle(bytes, offset + 148)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            //Write values
            bytes.AddRange(BitConverter.GetBytes(int.Parse(Index)));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
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
            bytes.AddRange(BitConverter.GetBytes(F_64));
            bytes.AddRange(BitConverter.GetBytes(F_68));
            bytes.AddRange(BitConverter.GetBytes(F_72));
            bytes.AddRange(BitConverter.GetBytes(F_76));
            bytes.AddRange(BitConverter.GetBytes(F_80));
            bytes.AddRange(BitConverter.GetBytes(F_84));
            bytes.AddRange(BitConverter.GetBytes(F_88));
            bytes.AddRange(BitConverter.GetBytes(F_92));
            bytes.AddRange(BitConverter.GetBytes(F_96));
            bytes.AddRange(BitConverter.GetBytes(F_100));
            bytes.AddRange(BitConverter.GetBytes(F_104));
            bytes.AddRange(BitConverter.GetBytes(F_108));
            bytes.AddRange(BitConverter.GetBytes(F_112));
            bytes.AddRange(BitConverter.GetBytes(F_116));
            bytes.AddRange(BitConverter.GetBytes(F_120));
            bytes.AddRange(BitConverter.GetBytes(F_124));
            bytes.AddRange(BitConverter.GetBytes(F_128));
            bytes.AddRange(BitConverter.GetBytes(F_132));
            bytes.AddRange(BitConverter.GetBytes(F_136));
            bytes.AddRange(BitConverter.GetBytes(F_140));
            bytes.AddRange(BitConverter.GetBytes(F_144));
            bytes.AddRange(BitConverter.GetBytes(F_148));

            //Validate and return
            if (bytes.Count != 152) throw new InvalidDataException("PUP_Entry must be 152 bytes.");
            return bytes.ToArray();
        }
        
        public PUP_Entry Clone(int newId = -1)
        {
            var newEntry = this.Copy();
            if (newId > -1) newEntry.ID = newId;
            return newEntry;
        }
        
        public static PUP_Entry GetEmptyEntry(int id)
        {
            PUP_Entry newEntry = new PUP_Entry();
            newEntry.ID = id;
            return newEntry;
        }
    
        public bool CompareEntry(PUP_Entry entry)
        {
            return this.Compare(entry, nameof(Index), nameof(SortID), nameof(ID));
        }
    }


}
