using YAXLib;

namespace Xv2CoreLib.QED
{
    public interface IGenericTemplate
    {
        int I_00 { get; set; }
        int I_04 { get; set; }
        int I_08 { get; set; }
        int I_12 { get; set; }
        int I_16 { get; set; }
        int I_20 { get; set; }
        int I_24 { get; set; }
        int I_28 { get; set; }
    }

    public interface ITemplate1
    {
        float F_00 { get; set; }
        int I_04 { get; set; }
        int I_08 { get; set; }
        int I_12 { get; set; }
        int I_16 { get; set; }
        int I_20 { get; set; }
        int I_24 { get; set; }
        int I_28 { get; set; }
    }

    public interface ITemplate2
    {
        string Str_00 { get; set; }
        int I_20 { get; set; }
        int I_24 { get; set; }
        int I_28 { get; set; }
    }

    public interface ITemplate3
    {
        int I_00 { get; set; }
        int I_04 { get; set; }
        float F_08 { get; set; }
        int I_12 { get; set; }
        int I_16 { get; set; }
        int I_20 { get; set; }
        int I_24 { get; set; }
        int I_28 { get; set; }
    }

    public interface ITemplate4
    {
        int I_00 { get; set; }
        float F_04 { get; set; }
        int I_08 { get; set; }
        int I_12 { get; set; }
        int I_16 { get; set; }
        int I_20 { get; set; }
        int I_24 { get; set; }
        int I_28 { get; set; }
    }

    public interface ITemplate5
    {
        int I_00 { get; set; }
        bool FLAG { get; set; }
        int I_08 { get; set; }
        int I_12 { get; set; }
        int I_16 { get; set; }
        int I_20 { get; set; }
        int I_24 { get; set; }
        int I_28 { get; set; }
    }


    public class QED_Types
    {
        
        public class Template0 : IGenericTemplate
        {

            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class Template1 : ITemplate1
        {
            [YAXAttributeFor("F_00")]
            [YAXSerializeAs("value")]
            [YAXFormat("0.0###########")]
            public float F_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class Template2 : ITemplate2
        {
            [YAXAttributeForClass]
            public string Str_00 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class Template3 : ITemplate3
        {
            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("F_08")]
            [YAXSerializeAs("value")]
            [YAXFormat("0.0###########")]
            public float F_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class Template4 : ITemplate4
        {
            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("F_04")]
            [YAXSerializeAs("value")]
            [YAXFormat("0.0###########")]
            public float F_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }


        //Specific Conditions

        public class TYPE3_KO_TRIGGER : IGenericTemplate
        {
            public string DESCRIPTION = "KO";

            [YAXAttributeForClass]
            [YAXSerializeAs("QML ID")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE_4_TIME_PASSED_LOCAL : ITemplate1
        {
            [YAXAttributeForClass]
            [YAXSerializeAs("Seconds")]
            [YAXFormat("0.0###########")]
            public float F_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE8_TIME_PASSED_GLOBAL : ITemplate4
        {
            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("Seconds")]
            [YAXFormat("0.0###########")]
            public float F_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE9_HEALTH_TRIGGER : ITemplate3
        {
            public string DESCRIPTION = "HEALTH";

            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("QML ID")]
            public int I_04 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("PercentAmount")]
            [YAXFormat("0.0###########")]
            public float F_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE20_BOOLEAN_CHECK : ITemplate5
        {
            public string DESCRIPTION = "BOOLEAN_CHECK";

            [YAXAttributeForClass]
            [YAXSerializeAs("BooleanID")]
            public int I_00 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("value")]
            public bool FLAG { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE25_HIT_WITH_SKILL : IGenericTemplate
        {
            public string DESCRIPTION = "HIT_WITH_SKILL";

            [YAXAttributeFor("QML ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("SkillType")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("Skill ID2")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE26_KO_WITH_SKILL : IGenericTemplate
        {
            public string DESCRIPTION = "KO_WITH_SKILL";

            [YAXAttributeFor("QML ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("SkillType")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("Skill ID2")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE27_ON_AUDIO_FINISH_TRIGGER : IGenericTemplate
        {
            public string DESCRIPTION = "AUDIO_FINISH";

            [YAXAttributeForClass]
            [YAXSerializeAs("QBT ID")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE33_DELAYED_KO_TRIGGER : IGenericTemplate
        {
            public string DESCRIPTION = "DELAYED_KO";

            [YAXAttributeForClass]
            [YAXSerializeAs("QML ID")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE42_AFTER_EVENT : IGenericTemplate
        {

            public string DESCRIPTION = "AFTER_EVENT";

            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("Index")]
            public int I_04 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("SubIndex")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }


        //Specific Actions

        public class TYPE6_LOAD_DEMO : ITemplate2
        {
            public string DESCRIPTION = "LOAD_DEMO";
            [YAXAttributeForClass]
            [YAXSerializeAs("File")]
            public string Str_00 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE8_PLAY_DEMO : IGenericTemplate
        {
            public string DESCRIPTION = "PLAY_DEMO";
            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE16_CHARA_LEAVE : IGenericTemplate
        {
            public string DESCRIPTION = "CHARA_LEAVE";
            [YAXAttributeFor("QML ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE19_QUEST_CLEAR : IGenericTemplate
        {
            public string DESCRIPTION = "QUEST_CLEAR";
            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE21_QUEST_START : IGenericTemplate
        {
            public string DESCRIPTION = "QUEST_START";

            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE23_ADVANCE_INDEX : IGenericTemplate
        {
            public string DESCRIPTION = "ADVANCE_INDEX";

            [YAXAttributeForClass]
            [YAXSerializeAs("Index")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE25_PORTAL : IGenericTemplate
        {
            public string DESCRIPTION = "PORTAL";

            [YAXAttributeFor("Start Stage ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("Dest Stage ID")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("Unlocked Flag")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("Focus Flag")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE26_PLAY_AUDIO : IGenericTemplate
        {
            public string DESCRIPTION = "PLAY_AUDIO";

            [YAXAttributeForClass]
            [YAXSerializeAs("QBT ID")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("Stage ID")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE27_CHARA_SPAWN : IGenericTemplate
        {
            public string DESCRIPTION = "CHARA_SPAWN";

            [YAXAttributeFor("QML ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("QBT ID")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("CMN_BEV Index")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("Stage_ID")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE33_FINISH_STATE : IGenericTemplate
        {

            [YAXAttributeFor("State")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE34_GLOBAL_TRIGGER_SEND : ITemplate5
        {
            public string DESCRIPTION = "BOOLEAN_SET";

            [YAXAttributeForClass]
            [YAXSerializeAs("BooleanID")]
            public int I_00 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("value")]
            public bool FLAG { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE35_ULT_FINISH_START : IGenericTemplate
        {
            public string DESCRIPTION = "ULT_FINISH_START";

            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE37_HEALTH_CAP : ITemplate3
        {
            public string DESCRIPTION = "HEALTH_CAP";

            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("QML ID")]
            public int I_04 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("PercentAmount")]
            [YAXFormat("0.0###########")]
            public float F_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE39_STATS : IGenericTemplate
        {
            public string DESCRIPTION = "STATS";

            [YAXAttributeFor("QML_ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("Type")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("Amount")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE41_USE_SKILL : IGenericTemplate
        {
            public string DESCRIPTION = "USE_SKILL";

            [YAXAttributeFor("QML_ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("EquippedIndex")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE45_MULTI_CHARA_SPAWN : IGenericTemplate
        {
            public string DESCRIPTION = "MULTI_CHARA_SPAWN";

            [YAXAttributeFor("QML ID 1")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("QML ID 2")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("QML ID 3")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("QBT ID")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("Stage_ID")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE46_CHARA_LEAVE : IGenericTemplate
        {
            public string DESCRIPTION = "CHARA_LEAVE";

            [YAXAttributeFor("QML ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("Direction")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("QBT ID")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("CMN_BEV Index")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE49_TRANSFORM : IGenericTemplate
        {
            public string DESCRIPTION = "TRANSFORM";

            [YAXAttributeFor("QML ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("QBT ID")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("CMN_BEV Index")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE54_TRANSFORM_CHARA_SWAP : IGenericTemplate
        {
            public string DESCRIPTION = "TRANSFORM_CHARA_SWAP";

            [YAXAttributeFor("Current QML ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("New QML ID")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("QBT ID")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("CMN_BEV Index")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE55_PRELOAD_CHARA : IGenericTemplate
        {
            public string DESCRIPTION = "PRELOAD_CHARA";

            [YAXAttributeForClass]
            [YAXSerializeAs("QML ID")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE57_WAIT : ITemplate1
        {
            public string DESCRIPTION = "WAIT";

            [YAXAttributeForClass]
            [YAXSerializeAs("Seconds")]
            [YAXFormat("0.0###########")]
            public float F_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE58_CHANGE_STAGE : IGenericTemplate
        {
            public string DESCRIPTION = "CHANGE_STAGE";

            [YAXAttributeForClass]
            [YAXSerializeAs("Stage_ID")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE67_LOAD_BEV : ITemplate2
        {
            public string DESCRIPTION = "LOAD_BEV";

            [YAXAttributeForClass]
            [YAXSerializeAs("File")]
            public string Str_00 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE71_TUT_POPUP : IGenericTemplate
        {
            public string DESCRIPTION = "TUT_POPUP";

            [YAXAttributeForClass]
            [YAXSerializeAs("ID")]
            public int I_00 { get; set; }
            [YAXAttributeFor("I_04")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE83_TRANSFORM_CHARA_SWAP_1 : IGenericTemplate
        {
            public string DESCRIPTION = "TRANSFORM_CHARA_SWAP_1";

            [YAXAttributeFor("Current QML ID")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("New QML ID")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("QBT ID")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("CMN_BEV Index")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE85_ADD_TIME : ITemplate4
        {
            public string DESCRIPTION = "ADD_TIME";

            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeForClass]
            [YAXSerializeAs("Seconds")]
            [YAXFormat("0.0###########")]
            public float F_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }

        public class TYPE88_SKILLS_EQUIP : IGenericTemplate
        {
            public string DESCRIPTION = "SKILLS_EQUIP";

            [YAXAttributeFor("I_00")]
            [YAXSerializeAs("value")]
            public int I_00 { get; set; }
            [YAXAttributeFor("Mode")]
            [YAXSerializeAs("value")]
            public int I_04 { get; set; }
            [YAXAttributeFor("I_08")]
            [YAXSerializeAs("value")]
            public int I_08 { get; set; }
            [YAXAttributeFor("I_12")]
            [YAXSerializeAs("value")]
            public int I_12 { get; set; }
            [YAXAttributeFor("I_16")]
            [YAXSerializeAs("value")]
            public int I_16 { get; set; }
            [YAXAttributeFor("I_20")]
            [YAXSerializeAs("value")]
            public int I_20 { get; set; }
            [YAXAttributeFor("I_24")]
            [YAXSerializeAs("value")]
            public int I_24 { get; set; }
            [YAXAttributeFor("I_28")]
            [YAXSerializeAs("value")]
            public int I_28 { get; set; }
        }




    }
}
