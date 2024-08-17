using System;
using System.Collections.Generic;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.CDT
{
    [YAXSerializeAs("CDT")]
    public class CDT_File
    {
        [YAXAttributeForClass]
        public uint I_08 { get; set; }
        [YAXAttributeForClass]
        public uint I_12 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Entry")]
        public List<CDT_Entry> Entries { get; set; }

        #region LoadSave
        public static CDT_File Load(byte[] bytes)
        {
            return new Parser(bytes).cdtFile;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }
        #endregion
    }

    [YAXSerializeAs("Entry")]
    public class CDT_Entry : IInstallable
    {
        #region Installer
        [YAXDontSerialize]
        public int SortID { get { return ID; } set { ID = value; } }
        [YAXDontSerialize]
        public string Index { get { return ID.ToString(); } set { ID = Utils.TryParseInt(value); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int ID { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("TextLeftLength")]
        [YAXDontSerialize]
        public int TextLeftLength { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("TextLeft")]
        public string TextLeft { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("TextRightLength")]
        [YAXDontSerialize]
        public int TextRightLength { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("TextRight")]
        public string TextRight { get; set; }
        //These ints do many different things like text align, padding, but also control images that show in the credits
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
    }
}
