using System.Collections.Generic;
using YAXLib;

namespace Xv2CoreLib.QBT
{
    //Same values as Eternity source
    public enum QbtInteractionType
    {
        DEFAULT = 0,
        EFFECT = 1,
        GIVE_ITEM = 2,
        JOIN = 3
    };

    public enum QbtEvent
    {
        HYPNO_ALLY = 3,
        YPNO_ATTACK = 4,
        GIANT_KI_BLAST = 5,
        AREA_CHANGE = 6,
        GIANT_KI_BLAST_RETURNED = 7,
        TELEPORT_ATTACK_FAILED = 8,
        CONTROLLED_ALLY_DEFEATED = 9,
        TELEPORT_END = 11,
        CRYSTALS_DESTROYED = 12,
        TELEPORT_ATTACK_SUCCEDED = 14
    };

    [YAXSerializeAs("QBT")]
    public class QBT_File : IIsNull
    {
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("NormalDialogue")]
        public List<DialogueEntry> NormalDialogues { get; set; } = new List<DialogueEntry>();
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("InteractiveDialogue")]
        public List<DialogueEntry> InteractiveDialogues { get; set; } = new List<DialogueEntry>();
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SpecialDialogues")]
        public List<DialogueEntry> SpecialDialogues { get; set; } = new List<DialogueEntry>();

        public static QBT_File Load(byte[] bytes)
        {
            return new Parser(bytes).QbtFile;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public bool IsNull()
        {
            return NormalDialogues.Count == 0 && InteractiveDialogues.Count == 0 && SpecialDialogues.Count == 0;
        }
    }

    [YAXSerializeAs("DialogueEntry")]
    public class DialogueEntry : IInstallable
    {
        #region IInstallable
        [YAXDontSerialize]
        public int SortID => ID;

        [YAXDontSerialize]
        public ushort ID
        {
            get => (ushort)Utils.TryParseInt(Index);
            set => Index = value.ToString();
        }
        #endregion

        [YAXAttributeForClass]
        [BindingAutoId]
        [YAXSerializeAs("ID")]
        public string Index { get; set; } //ushort

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("Interaction")]
        [YAXSerializeAs("Type")]
        public QbtInteractionType InteractionType { get; set; }
        [YAXAttributeFor("Interaction")]
        [YAXSerializeAs("Parameter")]
        public int InteractionParam { get; set; }
        [YAXAttributeFor("SpecialEvent")]
        [YAXSerializeAs("value")]
        public QbtEvent SpecialEvent { get; set; }
        [YAXAttributeFor("SpecialOnEventEnd")]
        [YAXSerializeAs("value")]
        public int SpecialOnEventEnd { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("CharaID")]
        [YAXSerializeAs("value")]
        public ushort CharaID { get; set; } //32
        [YAXAttributeFor("I_34")]
        [YAXSerializeAs("value")]
        public ushort I_34 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXDontSerializeIfNull]
        public int[] I_36 { get; set; } //size 7


        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "DialoguePart")]
        public List<QuoteData> DialogueEntries { get; set; } = new List<QuoteData>();
    }

    [YAXSerializeAs("DialoguePart")]
    public class QuoteData
    {
        [YAXSerializeAs("Sub_ID")]
        [YAXAttributeForClass]
        public short I_02 { get; set; }
        [YAXAttributeFor("CharaID")]
        [YAXSerializeAs("value")]
        public short I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public short I_06 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public short I_08 { get; set; }
        [YAXAttributeFor("Portrait")]
        [YAXSerializeAs("CharaID")]
        public short I_10 { get; set; }
        [YAXAttributeFor("Portrait")]
        [YAXSerializeAs("CostumeIndex")]
        public short I_12 { get; set; }
        [YAXAttributeFor("Portrait")]
        [YAXSerializeAs("State")]
        public short I_14 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public short I_16 { get; set; }
        [YAXSerializeAs("MSG")]
        [YAXAttributeForClass]
        public string MSG_Name { get; set; }

    }


}
