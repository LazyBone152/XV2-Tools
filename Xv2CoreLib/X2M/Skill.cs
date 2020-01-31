using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.X2M
{
    //Not finished.

    public class Skill
    {
        [YAXSerializeAs("X2M")]
        public class X2M_Skill
        {
            [YAXAttributeForClass]
            [YAXSerializeAs("type")]
            public string X2MType { get; set; } = "NEW_SKILL";
            [YAXAttributeFor("MOD_NAME")]
            [YAXSerializeAs("value")]
            public string Name { get; set; }
            [YAXAttributeFor("MOD_AUTHOR")]
            [YAXSerializeAs("value")]
            public string Author { get; set; }
            [YAXAttributeFor("MOD_VERSION")]
            [YAXSerializeAs("value")]
            public float Version { get; set; }
            [YAXAttributeFor("MOD_GUID")]
            [YAXSerializeAs("value")]
            public Guid ModGuid { get; set; }
            [YAXAttributeFor("UDATA")]
            [YAXSerializeAs("value")]
            public string UDATA { get; set; } = @"/DFYuS27iINlve3hWpp39xsjjXtZaYizu6yZciTwKik4/pLDjzJNBiW/gvG6rttfmGK3Qb41LvCP&#x0A;PZtJtLOjpg==";

            //Skill names
            [YAXAttributeFor("SKILL_NAME_EN")]
            [YAXSerializeAs("value")]
            public string SkillName_En { get; set; }
            [YAXAttributeFor("SKILL_NAME_ES")]
            [YAXSerializeAs("value")]
            public string SkillName_Es { get; set; }
            [YAXAttributeFor("SKILL_NAME_CA")]
            [YAXSerializeAs("value")]
            public string SkillName_Ca { get; set; }
            [YAXAttributeFor("SKILL_NAME_FR")]
            [YAXSerializeAs("value")]
            public string SkillName_Fr { get; set; }
            [YAXAttributeFor("SKILL_NAME_DE")]
            [YAXSerializeAs("value")]
            public string SkillName_De { get; set; }
            [YAXAttributeFor("SKILL_NAME_IT")]
            [YAXSerializeAs("value")]
            public string SkillName_It { get; set; }
            [YAXAttributeFor("SKILL_NAME_PT")]
            [YAXSerializeAs("value")]
            public string SkillName_PT { get; set; }
            [YAXAttributeFor("SKILL_NAME_PL")]
            [YAXSerializeAs("value")]
            public string SkillName_Pl { get; set; }
            [YAXAttributeFor("SKILL_NAME_RU")]
            [YAXSerializeAs("value")]
            public string SkillName_Ru { get; set; }
            [YAXAttributeFor("SKILL_NAME_TW")]
            [YAXSerializeAs("value")]
            public string SkillName_Tw { get; set; }
            [YAXAttributeFor("SKILL_NAME_ZH")]
            [YAXSerializeAs("value")]
            public string SkillName_Zh { get; set; }
            [YAXAttributeFor("SKILL_NAME_KR")]
            [YAXSerializeAs("value")]
            public string SkillName_Kr { get; set; }

            //Skill descriptions
            [YAXAttributeFor("SKILL_DESC_EN")]
            [YAXSerializeAs("value")]
            public string SkillDesc_En { get; set; }
            [YAXAttributeFor("SKILL_DESC_ES")]
            [YAXSerializeAs("value")]
            public string SkillDesc_Es { get; set; }
            [YAXAttributeFor("SKILL_DESC_CA")]
            [YAXSerializeAs("value")]
            public string SkillDesc_Ca { get; set; }
            [YAXAttributeFor("SKILL_DESC_FR")]
            [YAXSerializeAs("value")]
            public string SkillDesc_Fr { get; set; }
            [YAXAttributeFor("SKILL_DESC_DE")]
            [YAXSerializeAs("value")]
            public string SkillDesc_De { get; set; }
            [YAXAttributeFor("SKILL_DESC_IT")]
            [YAXSerializeAs("value")]
            public string SkillDesc_It { get; set; }
            [YAXAttributeFor("SKILL_DESC_PT")]
            [YAXSerializeAs("value")]
            public string SkillDesc_PT { get; set; }
            [YAXAttributeFor("SKILL_DESC_PL")]
            [YAXSerializeAs("value")]
            public string SkillDesc_Pl { get; set; }
            [YAXAttributeFor("SKILL_DESC_RU")]
            [YAXSerializeAs("value")]
            public string SkillDesc_Ru { get; set; }
            [YAXAttributeFor("SKILL_DESC_TW")]
            [YAXSerializeAs("value")]
            public string SkillDesc_Tw { get; set; }
            [YAXAttributeFor("SKILL_DESC_ZH")]
            [YAXSerializeAs("value")]
            public string SkillDesc_Zh { get; set; }
            [YAXAttributeFor("SKILL_DESC_KR")]
            [YAXSerializeAs("value")]
            public string SkillDesc_Kr { get; set; }

            [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "X2mSkillTransName")]
            public List<X2mSkillTransName> X2mTransNames { get; set; }

            public CusSkill Skill { get; set; }

            [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PupEntry")]
            public List<PupEntry> PupEntries { get; set; }

            public void SetName(string name)
            {
                SkillName_En = name;
                SkillName_Es = name;
                SkillName_Ca = name;
                SkillName_Fr = name;
                SkillName_De = name;
                SkillName_It = name;
                SkillName_PT = name;
                SkillName_Ru = name;
                SkillName_Tw = name;
                SkillName_Zh = name;
                SkillName_Kr = name;
            }

            public void SetDescription(string desc)
            {
                SkillDesc_En = desc;
                SkillDesc_Es = desc;
                SkillDesc_Ca = desc;
                SkillDesc_Fr = desc;
                SkillDesc_De = desc;
                SkillDesc_It = desc;
                SkillDesc_PT = desc;
                SkillDesc_Ru = desc;
                SkillDesc_Tw = desc;
                SkillDesc_Zh = desc;
                SkillDesc_Kr = desc;
            }
        }

        public class X2mSkillTransName
        {
            [YAXAttributeFor("TRANS_NAME_EN")]
            [YAXSerializeAs("value")]
            public string SkillName_En { get; set; }
            [YAXAttributeFor("TRANS_NAME_ES")]
            [YAXSerializeAs("value")]
            public string SkillName_Es { get; set; }
            [YAXAttributeFor("TRANS_NAME_CA")]
            [YAXSerializeAs("value")]
            public string SkillName_Ca { get; set; }
            [YAXAttributeFor("TRANS_NAME_FR")]
            [YAXSerializeAs("value")]
            public string SkillName_Fr { get; set; }
            [YAXAttributeFor("TRANS_NAME_DE")]
            [YAXSerializeAs("value")]
            public string SkillName_De { get; set; }
            [YAXAttributeFor("TRANS_NAME_IT")]
            [YAXSerializeAs("value")]
            public string SkillName_It { get; set; }
            [YAXAttributeFor("TRANS_NAME_PT")]
            [YAXSerializeAs("value")]
            public string SkillName_PT { get; set; }
            [YAXAttributeFor("TRANS_NAME_PL")]
            [YAXSerializeAs("value")]
            public string SkillName_Pl { get; set; }
            [YAXAttributeFor("TRANS_NAME_RU")]
            [YAXSerializeAs("value")]
            public string SkillName_Ru { get; set; }
            [YAXAttributeFor("TRANS_NAME_TW")]
            [YAXSerializeAs("value")]
            public string SkillName_Tw { get; set; }
            [YAXAttributeFor("TRANS_NAME_ZH")]
            [YAXSerializeAs("value")]
            public string SkillName_Zh { get; set; }
            [YAXAttributeFor("TRANS_NAME_KR")]
            [YAXSerializeAs("value")]
            public string SkillName_Kr { get; set; }

            public void SetName(string name)
            {
                SkillName_En = name;
                SkillName_Es = name;
                SkillName_Ca = name;
                SkillName_Fr = name;
                SkillName_De = name;
                SkillName_It = name;
                SkillName_PT = name;
                SkillName_Ru = name;
                SkillName_Tw = name;
                SkillName_Zh = name;
                SkillName_Kr = name;
            }

        }

        [YAXSerializeAs("Skill")]
        public class CusSkill
        {
            [YAXAttributeForClass]
            public string name { get; set; }
            [YAXAttributeForClass]
            public string id1 { get; set; } = "47818";
            [YAXAttributeForClass]
            public string id2 { get; set; } = "47818";
            [YAXAttributeFor("RACE_LOCK")]
            [YAXSerializeAs("value")]
            public string RACE_LOCK { get; set; }
            [YAXAttributeFor("TYPE")]
            [YAXSerializeAs("value")]
            public string TYPE { get; set; }
            [YAXAttributeFor("U_0E")]
            [YAXSerializeAs("value")]
            public string U_0E { get; set; }
            [YAXAttributeFor("PARTSET")]
            [YAXSerializeAs("value")]
            public string PARTSET { get; set; } = "0xffff";
            [YAXAttributeFor("U_12")]
            [YAXSerializeAs("value")]
            public string U_12 { get; set; } = "0xff00";
            [YAXAttributeFor("PATHS")]
            [YAXSerializeAs("value")]
            public string[] PATHS { get; set; } //size 7
            [YAXAttributeFor("U_30")]
            [YAXSerializeAs("value")]
            public string U_30 { get; set; } = "0x0";
            [YAXAttributeFor("U_32")]
            [YAXSerializeAs("value")]
            public string U_32 { get; set; } = "0x10f";
            [YAXAttributeFor("U_34")]
            [YAXSerializeAs("value")]
            public string U_34 { get; set; } = "0x3";
            [YAXAttributeFor("U_36")]
            [YAXSerializeAs("value")]
            public string U_36 { get; set; } = "0x2";
            [YAXAttributeFor("PUP_ID")]
            [YAXSerializeAs("value")]
            public string PUP_ID { get; set; } = "0xffff";
            [YAXAttributeFor("AURA")]
            [YAXSerializeAs("value")]
            public string AURA { get; set; } = "0xffff";
            [YAXAttributeFor("MODEL")]
            [YAXSerializeAs("value")]
            public string MODEL { get; set; } = "0xffff";
            [YAXAttributeFor("CHANGE_SKILLSET")]
            [YAXSerializeAs("value")]
            public string CHANGE_SKILLSET { get; set; } = "0xffff";
            [YAXAttributeFor("NUM_TRANSFORMS")]
            [YAXSerializeAs("value")]
            public string NUM_TRANSFORMS { get; set; }
        }

        public class PupEntry
        {
            [YAXAttributeForClass]
            public string id { get; set; } = "0xbacabaca";
            [YAXAttributeFor("U_04")]
            [YAXSerializeAs("value")]
            public string U_04 { get; set; }
            [YAXAttributeFor("U_08")]
            [YAXSerializeAs("value")]
            public string U_08 { get; set; }
            [YAXAttributeFor("U_0C")]
            [YAXSerializeAs("value")]
            public string U_0C { get; set; }
            [YAXAttributeFor("HEA")]
            [YAXSerializeAs("value")]
            public string HEA { get; set; }
            [YAXAttributeFor("F_14")]
            [YAXSerializeAs("value")]
            public string F_14 { get; set; }
            [YAXAttributeFor("KI")]
            [YAXSerializeAs("value")]
            public string KI { get; set; }
            [YAXAttributeFor("KI_RECOVERY")]
            [YAXSerializeAs("value")]
            public string KI_RECOVERY { get; set; }
            [YAXAttributeFor("STM")]
            [YAXSerializeAs("value")]
            public string STM { get; set; }
            [YAXAttributeFor("STAMINA_RECOVERY")]
            [YAXSerializeAs("value")]
            public string STAMINA_RECOVERY { get; set; }
            [YAXAttributeFor("ENEMY_STAMINA_ERASER")]
            [YAXSerializeAs("value")]
            public string ENEMY_STAMINA_ERASER { get; set; }
            [YAXAttributeFor("STAMINA_ERASER")]
            [YAXSerializeAs("value")]
            public string STAMINA_ERASER { get; set; }
            [YAXAttributeFor("F_30")]
            [YAXSerializeAs("value")]
            public string F_30 { get; set; }
            [YAXAttributeFor("ATK")]
            [YAXSerializeAs("value")]
            public string ATK { get; set; }
            [YAXAttributeFor("BASIC_KI_ATTACK")]
            [YAXSerializeAs("value")]
            public string BASIC_KI_ATTACK { get; set; }
            [YAXAttributeFor("STR")]
            [YAXSerializeAs("value")]
            public string STR { get; set; }
            [YAXAttributeFor("BLA")]
            [YAXSerializeAs("value")]
            public string BLA { get; set; }
            [YAXAttributeFor("ATK_DAMAGE")]
            [YAXSerializeAs("value")]
            public string ATK_DAMAGE { get; set; }
            [YAXAttributeFor("KI_DAMAGE")]
            [YAXSerializeAs("value")]
            public string KI_DAMAGE { get; set; }
            [YAXAttributeFor("STR_DAMAGE")]
            [YAXSerializeAs("value")]
            public string STR_DAMAGE { get; set; }
            [YAXAttributeFor("BLA_DAMAGE")]
            [YAXSerializeAs("value")]
            public string BLA_DAMAGE { get; set; }
            [YAXAttributeFor("GROUND_SPEED")]
            [YAXSerializeAs("value")]
            public string GROUND_SPEED { get; set; }
            [YAXAttributeFor("AIR_SPEED")]
            [YAXSerializeAs("value")]
            public string AIR_SPEED { get; set; }
            [YAXAttributeFor("BOOSTING_SPEED")]
            [YAXSerializeAs("value")]
            public string BOOSTING_SPEED { get; set; }
            [YAXAttributeFor("DASH_SPEED")]
            [YAXSerializeAs("value")]
            public string DASH_SPEED { get; set; }
            [YAXAttributeFor("F_64")]
            [YAXSerializeAs("value")]
            public string F_64 { get; set; }
            [YAXAttributeFor("F_68")]
            [YAXSerializeAs("value")]
            public string F_68 { get; set; }
            [YAXAttributeFor("F_6C")]
            [YAXSerializeAs("value")]
            public string F_6C { get; set; }
            [YAXAttributeFor("F_70")]
            [YAXSerializeAs("value")]
            public string F_70 { get; set; }
            [YAXAttributeFor("F_74")]
            [YAXSerializeAs("value")]
            public string F_74 { get; set; }
            [YAXAttributeFor("F_78")]
            [YAXSerializeAs("value")]
            public string F_78 { get; set; }
            [YAXAttributeFor("F_7C")]
            [YAXSerializeAs("value")]
            public string F_7C { get; set; }
            [YAXAttributeFor("F_80")]
            [YAXSerializeAs("value")]
            public string F_80 { get; set; }
            [YAXAttributeFor("F_84")]
            [YAXSerializeAs("value")]
            public string F_84 { get; set; }
            [YAXAttributeFor("F_88")]
            [YAXSerializeAs("value")]
            public string F_88 { get; set; }
            [YAXAttributeFor("F_8C")]
            [YAXSerializeAs("value")]
            public string F_8C { get; set; }
            [YAXAttributeFor("F_90")]
            [YAXSerializeAs("value")]
            public string F_90 { get; set; }
            [YAXAttributeFor("F_94")]
            [YAXSerializeAs("value")]
            public string F_94 { get; set; }
        }

        public class X2mSkillAura
        {
            [YAXDontSerializeIfNull]
            public CusAuraData CusAuraData { get; set; }
            [YAXDontSerializeIfNull]
            public Aura Aura { get; set; }
        }

        public class CusAuraData
        {
            [YAXAttributeForClass]
            public string cus_aura_id { get; set; } = "0xbaca";
            [YAXAttributeForClass]
            public string aur_aura_id { get; set; } = "0xcaca";
            [YAXAttributeFor("BEHAVIOUR_11")]
            [YAXSerializeAs("value")]
            public string BEHAVIOUR_11 { get; set; }
            [YAXAttributeFor("INTEGER_2")]
            [YAXSerializeAs("value")]
            public string INTEGER_2 { get; set; }
            [YAXAttributeFor("BEHAVIOUR_10")]
            [YAXSerializeAs("value")]
            public string BEHAVIOUR_10 { get; set; }
            [YAXAttributeFor("INTEGER_3")]
            [YAXSerializeAs("value")]
            public string INTEGER_3 { get; set; }
            [YAXAttributeFor("FORCE_TELEPORT")]
            [YAXSerializeAs("value")]
            public string FORCE_TELEPORT { get; set; }
            [YAXAttributeFor("BEHAVIOUR_13")]
            [YAXSerializeAs("value")]
            public string BEHAVIOUR_13 { get; set; }
            [YAXAttributeFor("BEHAVIOUR_66")]
            [YAXSerializeAs("value")]
            [YAXDontSerializeIfNull]
            public string BEHAVIOUR_66 { get; set; }
            [YAXAttributeFor("REMOVE_HAIR_ACCESSORIES")]
            [YAXSerializeAs("value")]
            public string REMOVE_HAIR_ACCESSORIES { get; set; }
            [YAXAttributeFor("BCS_HAIR_COLOR")]
            [YAXSerializeAs("value")]
            public string BCS_HAIR_COLOR { get; set; }
            [YAXAttributeFor("BCS_EYES_COLOR")]
            [YAXSerializeAs("value")]
            public string BCS_EYES_COLOR { get; set; }
        }

        public class Aura
        {
            [YAXAttributeForClass]
            public string cus_aura_id { get; set; } = "0xbaca";
            [YAXAttributeForClass]
            public string unknow_0 { get; set; } = "0x0";

            public List<Effect> Effects { get; set; }
        }

        public class Effect
        {
            [YAXAttributeForClass]
            public string auraType { get; set; }
            [YAXAttributeForClass]
            public string idEffect { get; set; }
        }

        public class X2mDepends
        {
            [YAXAttributeForClass]
            public string id { get; set; } = "0xc000";
            [YAXAttributeForClass]
            public string name { get; set; }

            [YAXAttributeFor("GUID")]
            [YAXSerializeAs("value")]
            public Guid DependGuid { get; set; }
            [YAXAttributeFor("TYPE")]
            [YAXSerializeAs("value")]
            public string TYPE { get; set; }
        }
    }
}
