using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.QSF
{
    [YAXSerializeAs("QSF")]
    public class QSF_File
    {
        [YAXSerializeAs("I_12")]
        [YAXAttributeForClass]
        public int I_12 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Quest")]
        public List<TableSection> Tables { get; set; }

        public TableSection GetQuestType(string type)
        {
            foreach(var entry in Tables)
            {
                if (entry.Type == type) return entry;
            }
            return null;
        }
    }

    [YAXSerializeAs("Quest")]
    public class TableSection {
        [YAXSerializeAs("Type")]
        [YAXAttributeForClass]
        public string Type { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_12")]
        public int I_12 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Entry")]
        public List<DataSection> TableEntry { get; set; }

        

    }

    [YAXSerializeAs("Entry")]
    public class DataSection {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SubEntry")]
        public List<QuestEntry> TableSubEntry { get; set; }
    }

    [YAXSerializeAs("SubEntry")]
    public class QuestEntry {
        [YAXAttributeForClass]
        [YAXSerializeAs("QuestID")]
        public string QuestID { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("SortedID")]
        public int Alias_ID { get; set; }

    }

}
