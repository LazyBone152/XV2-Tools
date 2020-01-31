using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCP
{
    [YAXSerializeAs("OCP")]
    class OCP_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Partner")]
        public List<OCP_TableEntry> TableEntries { get; set; }
    }

    [YAXSerializeAs("Partner")]
    public class OCP_TableEntry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Partner_ID")]
        public uint Index { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "StatType")]
        public List<OCP_SubEntry> SubEntries { get; set; }
        

    }

    [YAXSerializeAs("StatType")]
    public class OCP_SubEntry
    {

        [YAXAttributeForClass]
        [YAXSerializeAs("Order")]
        public uint Index { get; set; } //0x4
        [YAXAttributeFor("TP_Cost_Toggle")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; } // uint32
        [YAXAttributeFor("TP_Cost")]
        [YAXSerializeAs("value")]
        public uint I_12 { get; set; }
        [YAXAttributeFor("StatType")]
        [YAXSerializeAs("ID")]
        public uint I_16 { get; set; }

    }
}
