using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CMS
{

    [YAXSerializeAs("CMS")]
    public class CMS_File : ISorting
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Entry")]
        public List<CMS_Entry> CMS_Entries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SortEntries()
        {
            CMS_Entries.Sort((x, y) => x.SortID - y.SortID);
        }

        public CMS_Entry GetEntry(string id)
        {
            foreach(var entry in CMS_Entries)
            {
                if (entry.Index == id) return entry;
            }

            return null;
        }

        public static CMS_File Load(byte[] rawBytes)
        {
            return new Parser(rawBytes).GetCmsFile();
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }
    }

    [YAXSerializeAs("Entry")]
    public class CMS_Entry : IInstallable
    {
        #region WrapperProperties
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }
        [YAXDontSerialize]
        public int ID { get { return int.Parse(Index); } set { Index = value.ToString(); } }
        [YAXDontSerialize]
        public string ShortName { get { return Str_04; } set { Str_04 = value; } }
        [YAXDontSerialize]
        public string BcsPath { get { return Str_32; } set { Str_32 = value; } }
        [YAXDontSerialize]
        public string EanPath { get { return Str_36; } set { Str_36 = value; } }
        [YAXDontSerialize]
        public string FceEanPath { get { return Str_44; } set { Str_44 = value; } }
        [YAXDontSerialize]
        public string FcePath { get { return Str_48; } set { Str_48 = value; } }
        [YAXDontSerialize]
        public string CamEanPath { get { return Str_56; } set { Str_56 = value; } }
        [YAXDontSerialize]
        public string BacPath { get { return Str_60; } set { Str_60 = value; } }
        [YAXDontSerialize]
        public string BcmPath { get { return Str_64; } set { Str_64 = value; } }
        [YAXDontSerialize]
        public string BaiPath { get { return Str_68; } set { Str_68 = value; } }
        [YAXDontSerialize]
        public string BdmPath { get { return Str_80; } set { Str_80 = value; } }

        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } //int32
        [YAXAttributeForClass]
        [YAXSerializeAs("ShortName")]
        public string Str_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public Int64 I_08 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_16 { get; set; }
        [YAXAttributeFor("LoadCamDist")]
        [YAXSerializeAs("value")]
        public ushort I_20 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_24 { get; set; }
        [YAXAttributeFor("I_26")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_26 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_28 { get; set; }
        [YAXAttributeFor("BCS")]
        [YAXSerializeAs("value")]
        public string Str_32 { get; set; }
        [YAXAttributeFor("EAN")]
        [YAXSerializeAs("value")]
        public string Str_36 { get; set; }
        [YAXAttributeFor("FCE_EAN")]
        [YAXSerializeAs("value")]
        public string Str_44 { get; set; }
        [YAXAttributeFor("FCE")]
        [YAXSerializeAs("value")]
        public string Str_48 { get; set; }
        [YAXAttributeFor("CAM_EAN")]
        [YAXSerializeAs("value")]
        public string Str_56 { get; set; }
        [YAXAttributeFor("BAC")]
        [YAXSerializeAs("value")]
        public string Str_60 { get; set; }
        [YAXAttributeFor("BCM")]
        [YAXSerializeAs("value")]
        public string Str_64 { get; set; }
        [YAXAttributeFor("BAI")]
        [YAXSerializeAs("value")]
        public string Str_68 { get; set; }
        [YAXAttributeFor("BDM")]
        [YAXSerializeAs("value")]
        public string Str_80 { get; set; }


        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "MsgComponent")]
        public List<MSG.Msg_Component> MsgComponents { get; set; } //Only for LB Mod Installer

        public bool IsSelfReference(string path)
        {
            return (ShortName == path || path == string.Format("../{0}/{0}", ShortName));
        }
    }

}
