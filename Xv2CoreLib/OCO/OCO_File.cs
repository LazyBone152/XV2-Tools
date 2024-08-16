using System;
using System.Collections.Generic;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.OCO
{
    [YAXSerializeAs("OCO")]
    public class OCO_File
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 16)]
        public ushort Version { get; set; } // 16 = pre 1.22, 20 = 1.22 or later
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Partner")]
        public List<OCO_Partner> Partners { get; set; }

        #region LoadSave
        public static OCO_File Load(byte[] bytes)
        {
            return new Parser(bytes).ocoFile;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }
        #endregion
    }

    [YAXSerializeAs("Partner")]
    public class OCO_Partner : IInstallable_2<OCO_Costume>, IInstallable
    {
        #region Installer
        [YAXDontSerialize]
        public int SortID { get { return PartnerID; } set { PartnerID = value; } }
        [YAXDontSerialize]
        public string Index { get { return PartnerID.ToString(); } set { PartnerID = Utils.TryParseInt(value); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Partner_ID")]
        public int PartnerID { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Costume")]
        public List<OCO_Costume> SubEntries { get; set; }
        

    }

    [YAXSerializeAs("Costume")]
    public class OCO_Costume : IInstallable
    {

        #region Installer
        [YAXDontSerialize]
        public int SortID { get { return I_04; } set { I_04 = value; } }
        [YAXDontSerialize]
        public string Index { get { return I_04.ToString(); } set { I_04 = Utils.TryParseInt(value); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int I_04 { get; set; } //0x4
        [YAXAttributeForClass]
        [YAXSerializeAs("I_08")]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_12")]
        public int I_12 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("NEW_I_16")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int NEW_I_16 { get; set; } //Added in v1.22
        [YAXAttributeForClass]
        [YAXSerializeAs("PartSet")]
        public int I_16 { get; set; }

    }
}
