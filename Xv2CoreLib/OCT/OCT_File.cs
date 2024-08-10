using System.Collections.Generic;
using YAXLib;

namespace Xv2CoreLib.OCT
{
    [YAXSerializeAs("OCT")]
    public class OCT_File
    {
        [YAXAttributeForClass]
        public ushort Version { get; set; } // 0x6: 0x14 = pre 1.22, 0x18 = 1.22 or later

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Partner")]
        public List<OCT_TableEntry> OctTableEntries { get; set; }

        #region LoadSave
        public static OCT_File Load(byte[] bytes)
        {
            return new Parser(bytes).octFile;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }
        #endregion
    }

    [YAXSerializeAs("Partner")]
    public class OCT_TableEntry : IInstallable_2<OCT_SubEntry>, IInstallable
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
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SuperSoul")]
        public List<OCT_SubEntry> SubEntries { get; set; }
        

    }

    [YAXSerializeAs("PartnerSuperSoul")]
    public class OCT_SubEntry : IInstallable
    {

        #region Installer
        [YAXDontSerialize]
        public int SortID { get { return I_04; } set { I_04 = value; } }
        [YAXDontSerialize]
        public string Index { get { return I_04.ToString(); } set { I_04 = Utils.TryParseInt(value); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Order")]
        public int I_04 { get; set; } //0x4
        [YAXAttributeForClass]
        [YAXSerializeAs("Super_Soul")]
        public int I_16 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("TP_Cost_Toggle")]
        public int I_08 { get; set; } // uint32
        [YAXAttributeForClass]
        [YAXSerializeAs("TP_Cost")]
        public int I_12 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("STP_Cost")] // New in 1.22
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int STP_Cost { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("DLC_Flag")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = -1)]
        public int I_20 { get; set; }

    }
}
