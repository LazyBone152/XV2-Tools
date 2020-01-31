using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCO
{
    [YAXSerializeAs("OCO")]
    class OCO_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Partner")]
        public List<OCO_TableEntry> TableEntries { get; set; }
    }

    [YAXSerializeAs("Partner")]
    public class OCO_TableEntry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Partner_ID")]
        public int Index { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ColorData")]
        public List<OCO_SubEntry> SubEntries { get; set; }
        

    }

    [YAXSerializeAs("ColorData")]
    public class OCO_SubEntry
    {

        [YAXAttributeForClass]
        [YAXSerializeAs("CostumeNumber")]
        public uint I_04 { get; set; } //0x4
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; } // uint32
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public uint I_12 { get; set; }
        [YAXAttributeFor("PartSet")]
        [YAXSerializeAs("value")]
        public uint I_16 { get; set; }

    }
}
