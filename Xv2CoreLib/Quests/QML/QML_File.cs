using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.QML
{
    [YAXComment("Control: character is controlled by player or AI. 0 = Player, 2 = AI\n" +
               "Team: ally or enemy. 1 = Ally, 2 = Enemy\n" +
               "Skills defined here will only be used if the QED switches them in (this is done for mentor missions)")]
    [YAXSerializeAs("QML")]
    public class QML_File
    {
       
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "QML_Entry")]
        public List<QML_Entry> qml_Entry { get; set; }
    }

    public class QML_Entry {
        [YAXSerializeAs("QML ID")]
        [YAXAttributeForClass]
        public int I_00 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_04")]
        public int I_04 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_08")]
        public int I_08 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_12")]
        public int I_12 { get; set; }
        [YAXSerializeAs("ID")]
        [YAXAttributeFor("Stage")]
        public int I_16 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Spawn_At_Start")]
        public int I_20 { get; set; }
        [YAXComment("0 = Player, 2 = AI")]
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Control")]
        public int I_24 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Team")]
        public int I_28 { get; set; }
        [YAXSerializeAs("QXD Chara ID")]
        [YAXAttributeForClass]
        public int I_32 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_36")]
        public int I_36 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_40")]
        public int I_40 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_44")]
        public int I_44 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_48")]
        public short[] I_48 { get; set; } // size = 5
        [YAXSerializeAs("Skills")]
        public Skills _Skills { get; set; }

    }

    public struct Skills
    {
        [YAXAttributeFor("Super_1")]
        [YAXSerializeAs("ID2")]
        public UInt16 I_00 { get; set; }
        [YAXAttributeFor("Super_2")]
        [YAXSerializeAs("ID2")]
        public UInt16 I_02 { get; set; }
        [YAXAttributeFor("Super_3")]
        [YAXSerializeAs("ID2")]
        public UInt16 I_04 { get; set; }
        [YAXAttributeFor("Super_4")]
        [YAXSerializeAs("ID2")]
        public UInt16 I_06 { get; set; }
        [YAXAttributeFor("Ultimate_1")]
        [YAXSerializeAs("ID2")]
        public UInt16 I_08 { get; set; }
        [YAXAttributeFor("Ultimate_2")]
        [YAXSerializeAs("ID2")]
        public UInt16 I_10 { get; set; }
        [YAXAttributeFor("Evasive")]
        [YAXSerializeAs("ID2")]
        public UInt16 I_12 { get; set; }
        [YAXAttributeFor("Blast_Type")]
        [YAXSerializeAs("ID2")]
        public UInt16 I_14 { get; set; }
        [YAXAttributeFor("Awoken")]
        [YAXSerializeAs("ID2")]
        public UInt16 I_16 { get; set; }
    }
}
