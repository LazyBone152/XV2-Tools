using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.QBT
{
    [YAXSerializeAs("QBT")]
    public struct QBT_File
    {
        [YAXAttributeForClass]
        public bool I_04 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("NormalDialogue")]
        public List<TableEntry> Type0 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("InteractiveDialogue")]
        public List<TableEntry> Type1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Type2_Unknown")]
        public List<TableEntry> Type2 { get; set; }

    }

    [YAXSerializeAs("DialogueEntry")]
    public struct TableEntry
    {
        [YAXAttributeForClass]
        public short QBT_ID { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("Table")]
        [YAXSerializeAs("values")]
        public short[] I_00 { get; set; } //size = 30

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "DialoguePart")]
        public List<QuoteData> DialogueEntries { get; set; }
    }

    [YAXSerializeAs("DialoguePart")]
    public struct QuoteData
    {
        [YAXSerializeAs("Sub-ID")]
        [YAXAttributeForClass]
        public short I_02 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_04")]
        public short I_04 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_06")]
        public short I_06 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_08")]
        public short I_08 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Chara ID")]
        public short I_10 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Costume Index")]
        public short I_12 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("State")]
        public short I_14 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_16")]
        public short I_16 { get; set; }
        [YAXSerializeAs("MSG")]
        [YAXAttributeForClass]
        public string Str_18 { get; set; }

    }


}
