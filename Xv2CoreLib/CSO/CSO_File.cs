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

        public CSO_Entry GetEntry(string id)
        {
            foreach(var entry in CsoEntries)
            {
                if (entry.I_00 == id) return entry;
            }

            return null;
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
                return string.Format("{0}_{1}", I_00, I_04);
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 2)
                {
                    I_00 = split[0];
                    I_04 = uint.Parse(split[1]);
                }
            }
        }
        [YAXDontSerialize]
        public int SortID
        {
            get
            {
                return int.Parse(I_00);
            }
        }
        [YAXDontSerialize]
        public int CharaID { get { return int.Parse(I_00); } set { I_00 = value.ToString(); } }
        [YAXDontSerialize]
        public uint Costume { get { return I_04; } set { I_04 = value; } }
        [YAXDontSerialize]
        public string SePath { get { return Str_08; } set { Str_08 = value; } }
        [YAXDontSerialize]
        public string VoxPath { get { return Str_12; } set { Str_12 = value; } }
        [YAXDontSerialize]
        public string AmkPath { get { return Str_16; } set { Str_16 = value; } } //Relative to data/chara. Doesn't include extension.
        [YAXDontSerialize]
        public string SkillCharaCode { get { return Str_20; } set { Str_20 = value; } } //"NULL" = use own chara code
        
        [YAXDontSerialize]
        public bool HasSePath { get { return !(SePath == "NULL" || string.IsNullOrWhiteSpace(SePath)); } }
        [YAXDontSerialize]
        public bool HasVoxPath { get { return !(VoxPath == "NULL" || string.IsNullOrWhiteSpace(VoxPath)); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Chara_ID")]
        public string I_00 { get; set; } //uint32
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public uint I_04 { get; set; }
        [YAXAttributeFor("SE")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_08 { get; set; }
        [YAXAttributeFor("VOX")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_12 { get; set; }
        [YAXAttributeFor("AMK")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_16 { get; set; }
        [YAXAttributeFor("Skills")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_20 { get; set; }
        //64 bits padding at end
    }
}
