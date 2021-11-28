using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CSO
{
    [YAXSerializeAs("CSO")]
    public class CSO_File : ISorting
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CsoEntry")]
        public List<CSO_Entry> CsoEntries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SortEntries()
        {
            CsoEntries.Sort((x, y) => x.SortID - y.SortID);
        }

        public static CSO_File Load(byte[] rawBytes)
        {
            return new Parser(rawBytes).csoFile;
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public CSO_Entry GetEntry(int id)
        {
            foreach(var entry in CsoEntries)
            {
                if (entry.CharaID == id) return entry;
            }

            return null;
        }


        public static CSO_Entry GetAndAddEntry(int id, int costume, IList<CSO_Entry> entries)
        {
            CSO_Entry entry = entries.FirstOrDefault(x => x.CharaID == id && x.Costume == costume);

            if (entry == null)
            {
                entry = new CSO_Entry();
                entry.CharaID = id;
                entry.Costume = (uint)costume;
                entries.Add(entry);
            }

            return entry;
        }
    }

    [YAXSerializeAs("CsoEntry")]
    public class CSO_Entry : IInstallable
    {
        #region WrapperProperties
        [YAXDontSerialize]
        public string Index
        {
            get
            {
                return string.Format("{0}_{1}", CharaID, Costume);
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 2)
                {
                    CharaID = int.Parse(split[0]);
                    Costume = uint.Parse(split[1]);
                }
            }
        }
        [YAXDontSerialize]
        public int SortID
        {
            get
            {
                return CharaID;
            }
        }
        
        [YAXDontSerialize]
        public bool HasSePath { get { return !(SePath == "NULL" || string.IsNullOrWhiteSpace(SePath)); } }
        [YAXDontSerialize]
        public bool HasVoxPath { get { return !(VoxPath == "NULL" || string.IsNullOrWhiteSpace(VoxPath)); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Chara_ID")]
        public int CharaID { get; set; } 
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public uint Costume { get; set; }
        [YAXAttributeFor("SE")]
        [YAXSerializeAs("Path")]
        public string SePath { get; set; }
        [YAXAttributeFor("VOX")]
        [YAXSerializeAs("Path")]
        public string VoxPath { get; set; }
        [YAXAttributeFor("AMK")]
        [YAXSerializeAs("Path")]
        public string AmkPath { get; set; } //Relative to data/chara. Doesn't include extension.
        [YAXAttributeFor("Skills")]
        [YAXSerializeAs("Path")]
        public string SkillCharaCode { get; set; } //"NULL" = use own chara code

        //64 bits padding at end
    }
}
