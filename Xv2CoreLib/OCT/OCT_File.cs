using System.Collections.Generic;
using YAXLib;

namespace Xv2CoreLib.OCT
{
    [YAXSerializeAs("OCT")]
    class OCT_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Partner")]
        public List<OCT_TableEntry> OctTableEntries { get; set; }
    }

    [YAXSerializeAs("Partner")]
    public class OCT_TableEntry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Partner_ID")]
        public uint Index { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SuperSoul")]
        public List<OCT_SubEntry> OctSubEntries { get; set; }
        

    }

    [YAXSerializeAs("PartnerSuperSoul")]
    public class OCT_SubEntry
    {

        [YAXAttributeForClass]
        [YAXSerializeAs("Order")]
        public int Index { get; set; } //0x4
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
        [YAXSerializeAs("I_20")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = -1)]
        public int I_20 { get; set; }

    }
}
