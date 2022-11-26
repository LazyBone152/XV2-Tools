using System.Collections.Generic;
using YAXLib;

namespace Xv2CoreLib.QSL
{
    public enum QslPositionType
    {
        POSITION_ITEM = 0,
        POSITION_CHAR = 1,
        POSITION_CHAR2 = 2,
        POSITION_CHAR3 = 3,
        POSITION_CHAR5 = 5
    };

    [YAXSerializeAs("QSL")]
    public class QSL_File : IIsNull
    {
        [YAXAttributeForClass]
        public short I_10 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Stage")]
        public List<StageEntry> Stages { get; set; } = new List<StageEntry>();

        public static QSL_File Load(byte[] bytes)
        {
            return new Parser(bytes).QslFile;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public bool IsNull()
        {
            return Stages.Count == 0;
        }
    }

    public class StageEntry : IInstallable_2<PositionEntry>, IInstallable
    {
        #region WrappedProperties
        [YAXDontSerialize]
        public string Index { get { return StageID.ToString(); } set { StageID = Utils.TryParseInt(value); } }
        [YAXDontSerialize]
        public int SortID => StageID;
        #endregion

        [YAXAttributeForClass]
        public int StageID { get; set; }
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public short[] I_04 { get; set; } //size 3, all 0
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PositionEntry")]
        public List<PositionEntry> SubEntries { get; set; }
    }

    [YAXSerializeAs("PositionEntry")]
    public class PositionEntry : IInstallable
    {
        #region IInstallable
        [YAXDontSerialize]
        public int SortID => ID;

        [YAXDontSerialize]
        public string Index
        {
            get => Type == QslPositionType.POSITION_ITEM ? $"{ID}_{Position}_" : ID.ToString(); //Consider position as part of the ID for Items (as they can use the same numeric ID), but for char positions, just use the number
            set => ID = (ushort)Utils.TryParseInt(value);
        }
        #endregion

        [YAXAttributeForClass]
        public ushort ID { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Position")]
        public string Position { get; set; } //max size = 32
        [YAXAttributeForClass]
        public QslPositionType Type { get; set; }

        [YAXAttributeFor("ChangeDialogue")]
        [YAXSerializeAs("value")]
        public ushort ChanceDialogue { get; set; }
        [YAXAttributeFor("I_38")]
        [YAXSerializeAs("value")]
        public ushort I_38 { get; set; }
        [YAXAttributeFor("QML_Change")]
        [YAXSerializeAs("value")]
        public ushort QML_Change { get; set; }
        [YAXAttributeFor("DefaultPose")]
        [YAXSerializeAs("value")]
        public ushort DefaultPose { get; set; }
        [YAXAttributeFor("TalkingPose")]
        [YAXSerializeAs("value")]
        public ushort TalkingPose { get; set; }
        [YAXAttributeFor("EffectPose")]
        [YAXSerializeAs("value")]
        public ushort EffectPose { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public ushort I_48 { get; set; }

        [YAXAttributeFor("I_50")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_50 { get; set; } //size 7, all 0

    }
}
