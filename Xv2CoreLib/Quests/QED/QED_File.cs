using System.Collections.Generic;
using YAXLib;

namespace Xv2CoreLib.QED
{
    [YAXSerializeAs("QED")]
    public class QED_File : IIsNull
    {
        [YAXDontSerializeIfNull]
        public List<Event> Events { get; set; } = new List<Event>();

        public static QED_File Load(byte[] bytes)
        {
            return new Parser(bytes).QedFile;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public bool IsNull()
        {
            return Events.Count == 0;
        }
    }

    public class Event : IInstallable
    {
        #region IInstallable
        [YAXDontSerialize]
        public int SortID => ID;

        [YAXDontSerialize]
        public string Index
        {
            get => $"{ID}_{SubIndex}";
            set
            {
                string[] vals = value?.Split('_');

                if(vals?.Length == 2)
                {
                    ID = (short)Utils.TryParseInt(vals[0]);
                    SubIndex = (short)Utils.TryParseInt(vals[1]);
                }
            }
        }
        #endregion

        //Contains all the Event1s and Event2s from a single index combination (example, index 4, subindex 2)
        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public short ID { get; set; }
        [YAXAttributeForClass]
        public short SubIndex { get; set; }
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<string> CONDITIONS { get; set; }

        [YAXDontSerializeIfNull]
        public List<Condition> Conditions { get; set; }
        [YAXDontSerializeIfNull]
        public List<Action> Actions { get; set; }


    }

    public class Condition
    {
        [YAXAttributeForClass]
        public short Type { get; set; }
        [YAXAttributeForClass]
        public string CONDITION { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public short I_06 { get; set; }

        [YAXDontSerializeIfNull]
        public QED_Types.Template1 Template_1 { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.Template2 Template_2 { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.Template3 Template_3 { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.Template4 Template_4 { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.Template0 TemplateGeneric { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE3_KO_TRIGGER KO_TRIGGER { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE_4_TIME_PASSED_LOCAL TIME_PASSED_LOCAL { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE8_TIME_PASSED_GLOBAL TIME_PASSED_GLOBAL { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE20_BOOLEAN_CHECK BOOLEAN_CHECK { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE25_HIT_WITH_SKILL HIT_WITH_SKILL { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE26_KO_WITH_SKILL KO_WITH_SKILL { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE9_HEALTH_TRIGGER HEALTH_TRIGGER { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE27_ON_AUDIO_FINISH_TRIGGER ON_AUDIO_FINISH_TRIGGER { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE33_DELAYED_KO_TRIGGER DELAYED_KO_TRIGGER { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE42_AFTER_EVENT AFTER_EVENT { get; set; }

    }

    public class Action
    {

        [YAXAttributeForClass]
        public short Type { get; set; }
        [YAXAttributeForClass]
        public string ACTION { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public short I_06 { get; set; }


        [YAXDontSerializeIfNull]
        public QED_Types.Template1 Template_1 { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.Template2 Template_2 { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.Template3 Template_3 { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.Template4 Template_4 { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.Template0 TemplateGeneric { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE16_CHARA_LEAVE CHARA_LEAVE16 { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE19_QUEST_CLEAR QUEST_CLEAR { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE21_QUEST_START QUEST_START { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE23_ADVANCE_INDEX ADVANCE_INDEX { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE25_PORTAL PORTAL { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE26_PLAY_AUDIO PLAY_AUDIO { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE27_CHARA_SPAWN CHARA_SPAWN { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE33_FINISH_STATE QUEST_FINISH_STATE { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE34_GLOBAL_TRIGGER_SEND BOOLEAN_SET { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE35_ULT_FINISH_START ULT_FINISH_START { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE37_HEALTH_CAP HEALTH_CAP { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE39_STATS STATS { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE45_MULTI_CHARA_SPAWN MULTI_CHARA_SPAWN { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE46_CHARA_LEAVE CHARA_LEAVE { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE49_TRANSFORM TRANSFORM { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE54_TRANSFORM_CHARA_SWAP TRANSFORM_CHARA_SWAP { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE55_PRELOAD_CHARA PRELOAD_CHARA { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE83_TRANSFORM_CHARA_SWAP_1 TRANSFORM_CHARA_SWAP1 { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE85_ADD_TIME ADD_TIME { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE88_SKILLS_EQUIP SKILLS_EQUIP { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE71_TUT_POPUP TUT_POPUP { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE57_WAIT WAIT { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE6_LOAD_DEMO LOAD_DEMO { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE8_PLAY_DEMO PLAY_DEMO { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE41_USE_SKILL USE_SKILL { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE67_LOAD_BEV LOAD_BEV { get; set; }
        [YAXDontSerializeIfNull]
        public QED_Types.TYPE58_CHANGE_STAGE CHANGE_STAGE { get; set; }

    }
}
