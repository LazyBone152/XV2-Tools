using System.Collections.Generic;
using YAXLib;

namespace Xv2CoreLib.QSL
{
    [YAXSerializeAs("QSL")]
    public struct QSL_File
    {
        [YAXAttributeForClass]
        public short I_10 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Stage")]
        public List<Stage> Stages { get; set; }
    }

    public struct Stage {
        [YAXAttributeForClass]
        public int StageID { get; set; }
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public short[] I_04 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "QSL_Entry")]
        public List<Position_Entry> Entries { get; set; }
    }

    [YAXSerializeAs("QSL_Entry")]
    public struct Position_Entry {
        [YAXAttributeForClass]
        [YAXSerializeAs("Position")]
        public string MapString { get; set; } //max size = 32
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public short I_32 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("QML ID")]
        public int I_34 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_38")]
        public short[] I_38 { get; set; } //size = 13

    }
}
