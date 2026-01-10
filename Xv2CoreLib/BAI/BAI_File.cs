using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BAI
{
    [YAXSerializeAs("BAI")]
    public class BAI_File : IIsNull
    {
        internal const byte CURRENT_VERSION = 2;
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (byte)0)]
        public byte Version { get; set; } = CURRENT_VERSION;

        public const int ENTRY_HEADER_SIZE = 24;
        public const int ENTRY_SIZE_OLD = 84;
        public const int ENTRY_SIZE_V1 = 88; //When they updated the AI for Crossversus
        public const int ENTRY_SIZE_V2 = 92; //New in v1.24/1.25(?)

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AI")]
        public List<BAI_Entry> Entries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static BAI_File Load(byte[] bytes)
        {
            return new Parser(bytes).baiFile;
        }

        public static BAI_File Load(string path)
        {
            return new Parser(path, false).baiFile;
        }

        public void Save(string path)
        {
            new Deserializer(this, path);
        }

        #region Helper
        public bool IsNull()
        {
            return Entries?.Count == 0;
        }

        public bool IsVersionValid()
        {
            return Version >= 0 && Version <= CURRENT_VERSION;
        }

        public static byte GetBaiVersion(byte[] rawBytes, int entryTableOffset, int entryCount)
        {
            if (entryCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(entryCount));

            // Find the first entry that has subentries
            int sampleStart = -1;
            int sampleCount = 0;

            for (int i = 0; i < entryCount; i++)
            {
                int offset = entryTableOffset + (i * ENTRY_HEADER_SIZE);
                int subCount = BitConverter.ToInt32(rawBytes, offset + 16);
                int subOffset = BitConverter.ToInt32(rawBytes, offset + 20);

                if (subCount > 0)
                {
                    sampleStart = subOffset;
                    sampleCount = subCount;
                    break;
                }
            }

            //No subentries anywhere > return latest version
            if (sampleStart == -1)
                return CURRENT_VERSION;

            int end = rawBytes.Length;

            for (int i = 0; i < entryCount; i++)
            {
                int offset = entryTableOffset + (i * ENTRY_HEADER_SIZE);
                int otherOffset = BitConverter.ToInt32(rawBytes, offset + 20);

                if (otherOffset > sampleStart && otherOffset < end)
                    end = otherOffset;
            }

            int bytes = end - sampleStart;

            if (bytes <= 0 || (bytes % sampleCount) != 0)
                throw new InvalidDataException($"Could not detect BAI subEntry size. start={sampleStart}, end={end}, bytes={bytes}, count={sampleCount}");

            int entrySize = bytes / sampleCount;

            switch (entrySize)
            {
                case ENTRY_SIZE_OLD: return 0;
                case ENTRY_SIZE_V1: return 1;
                case ENTRY_SIZE_V2: return 2;
                default:
                    throw new InvalidDataException($"BAI file version not supported. subEntrySize={entrySize}");
            }
        }

        public static int GetSubEntrySize(int version)
        {
            switch (version)
            {
                case 0:
                    return ENTRY_SIZE_OLD;

                case 1:
                    return ENTRY_SIZE_V1;

                case 2:
                    return ENTRY_SIZE_V2;

                default:
                    throw new InvalidDataException("BAI file version not supported.");
            }
        }
        #endregion
    }

    [YAXSerializeAs("AI")]
    public class BAI_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public UInt32 I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Behavior")]
        public List<BAI_SubEntry> SubEntries { get; set; }

    }

    [YAXSerializeAs("Behavior")]
    public class BAI_SubEntry
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeFor("B_08")]
        [YAXSerializeAs("value")]
        public bool I_08 { get; set; } //Int32 with two possible values: 0, 1. So a boolean.
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
        [YAXAttributeFor("I_84")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0)]
        public int I_84 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = -1)]
        public int I_88 { get; set; }

        public enum ActivationConditionTarget
        {
            Self = 0,
            Enemy = 1,
            Unknown = 2
        }

    }

}
