using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCC
{
    [YAXSerializeAs("OCC")]
    class OCC_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Partner")]
        public List<OCC_TableEntry> OccTableEntries { get; set; }
    }

    [YAXSerializeAs("Partner")]
    public class OCC_TableEntry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Partner_ID")]
        public uint Index { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ColorData")]
        public List<OCC_SubEntry> SubEntries { get; set; }
        

    }

    [YAXSerializeAs("ColorData")]
    public class OCC_SubEntry
    {

        [YAXAttributeForClass]
        [YAXSerializeAs("Colorable_Part_Type")]
        public uint I_04 { get; set; } //0x4
        [YAXAttributeFor("TP_Cost_Toggle")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; } // uint32
        [YAXAttributeFor("TP_Cost")]
        [YAXSerializeAs("value")]
        public uint I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public uint I_16 { get; set; }

    }
}
