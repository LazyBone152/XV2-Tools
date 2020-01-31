using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace EmbPack_LB.EMB
{
    public struct EmbIndex
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("I_08")]
        public UInt16 I_08 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_10")]
        public UInt16 I_10 { get; set; }
        [YAXAttributeForClass]
        public bool UseFileNames { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EmbEntry")]
        public List<EmbEntry> Entry { get; set; }
    }

    public struct EmbEntry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public int Index { get; set; }
    }



    //Olganix EmbPack

    [YAXSerializeAs("EMB")]
    public struct EmbIndex_Compat
    {

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "File")]
        public List<EmbEntry_Compat> Entry { get; set; }
    }

    [YAXSerializeAs("File")]
    public struct EmbEntry_Compat
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("name")]
        public string FileName1 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("filename")]
        public string FileName2 { get; set; }
    }


}
