using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BAS
{
    [YAXSerializeAs("BAS")]
    public class BAS_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BAS_Entry")]
        public List<BAS_Entry> Entries { get; set; }


        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static BAS_File Load(byte[] bytes)
        {
            return new Parser(bytes).basFile;
        }

        public static BAS_File Load(string path)
        {
            return new Parser(path, false).basFile;
        }

        public void Save(string path)
        {
            new Deserializer(this, path);
        }
    }

    public class BAS_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public UInt32 I_00 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Behavior")]
        public List<BAS_SubEntry> SubEntries { get; set; }

    }

    [YAXSerializeAs("Behavior")]
    public struct BAS_SubEntry
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeFor("B_08")]
        [YAXSerializeAs("value")]
        public bool I_08 { get; set; }
        [YAXAttributeFor("ActivationCondition1")]
        [YAXSerializeAs("Target")]
        public ActivationConditionTarget I_12 { get; set; } //Int32
        [YAXAttributeFor("ActivationCondition2")]
        [YAXSerializeAs("Target")]
        public ActivationConditionTarget I_16 { get; set; } //Int32
        [YAXAttributeFor("ActivationCondition3")]
        [YAXSerializeAs("Target")]
        public ActivationConditionTarget I_20 { get; set; } //Int32
        [YAXAttributeFor("ActivationCondition1")]
        [YAXSerializeAs("Condition")]
        public int I_24 { get; set; }
        [YAXAttributeFor("ActivationCondition2")]
        [YAXSerializeAs("Condition")]
        public int I_28 { get; set; }
        [YAXAttributeFor("ActivationCondition3")]
        [YAXSerializeAs("Condition")]
        public int I_32 { get; set; }
        [YAXAttributeFor("ActivationCondition1")]
        [YAXSerializeAs("Args")]
        public int I_36 { get; set; }
        [YAXAttributeFor("ActivationCondition2")]
        [YAXSerializeAs("Args")]
        public int I_40 { get; set; }
        [YAXAttributeFor("ActivationCondition3")]
        [YAXSerializeAs("Args")]
        public int I_44 { get; set; }
        [YAXAttributeFor("Weight")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("State")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("Parameter")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public int I_64 { get; set; }
        [YAXAttributeFor("F_68")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("ChargeThreshold")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_72 { get; set; }
        [YAXAttributeFor("F_76")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_76 { get; set; }
        [YAXAttributeFor("F_80")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_80 { get; set; }

        public enum ActivationConditionTarget
        {
            Self = 0,
            Enemy = 1,
            Unknown = 2
        }

    }

}
