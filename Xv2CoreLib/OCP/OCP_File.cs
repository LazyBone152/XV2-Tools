﻿using System.Collections.Generic;
using YAXLib;

namespace Xv2CoreLib.OCP
{
    [YAXSerializeAs("OCP")]
    public class OCP_File
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 16)]
        public ushort Version { get; set; } // 16 = pre 1.22, 20 = 1.22 or later
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Partner")]
        public List<OCP_TableEntry> TableEntries { get; set; }

        #region LoadSave
        public static OCP_File Load(byte[] bytes)
        {
            return new Parser(bytes).ocpFile;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }
        #endregion
    }

    [YAXSerializeAs("Partner")]
    public class OCP_TableEntry : IInstallable_2<OCP_SubEntry>, IInstallable
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
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "StatType")]
        public List<OCP_SubEntry> SubEntries { get; set; }
        

    }

    [YAXSerializeAs("StatType")]
    public class OCP_SubEntry : IInstallable
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
        [YAXAttributeFor("TP_Cost_Toggle")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; } // uint32
        [YAXAttributeFor("TP_Cost")]
        [YAXSerializeAs("value")]
        public uint I_12 { get; set; }
        [YAXAttributeFor("STP_Cost")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public uint NEW_I_16 { get; set; } = 0; //Added in v1.22
        [YAXAttributeFor("StatType")]
        [YAXSerializeAs("ID")]
        public uint I_16 { get; set; }

    }
}
