using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    [YAXSerializeAs("SuperSoul")]
    public class OCT_SubEntry
    {

        [YAXAttributeForClass]
        [YAXSerializeAs("Order")]
        public int Index { get; set; } //0x4
        [YAXAttributeFor("TP_Cost_Toggle")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; } // uint32
        [YAXAttributeFor("TP_Cost")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("Super_Soul")]
        [YAXSerializeAs("ID")]
        public int I_16 { get; set; }

    }
}
