namespace LB_Mod_Installer.Installer
{
    /// <summary>
    /// Section names for TrackingXml
    /// </summary>
    public static class Sections
    {
        public const string MSG_Entries = "MSG_Entries";
        public const string IDB_Entries = "IDB_Entries";
        public const string CUS_Skillsets = "CUS_Skillsets";
        public const string CUS_SuperSkills = "CUS_SuperSkills";
        public const string CUS_UltimateSkills = "CUS_UltimateSkills";
        public const string CUS_EvasiveSkills = "CUS_EvasiveSkills";
        public const string CUS_BlastSkills = "CUS_BlastSkills";
        public const string CUS_AwokenSkills = "CUS_AwokenSkills";
        public const string BAC_Entries = "BAC_Entries";
        public const string BCS_PartSets = "BCS_PartSets";
        public const string BCS_PartColors = "BCS_PartColor_";
        public const string BCS_PartColorsOverwrite = "BCS_PartColorOverwrite_";
        public const string BCS_Bodies = "BCS_Bodies";
        public const string BCS_SkeletonData = "BCS_SkeletonData";
        public const string BDM_Entries = "BDM_Entries";
        public const string BEV_Entries = "BEV_Entries";
        public const string BPE_Entries = "BPE_Entries";
        public const string BSA_Entries = "BSA_Entries";
        public const string CMS_Entries = "CMS_Entries";
        public const string CNC_Entries = "CNC_Entries";
        public const string CNS_Entries = "CNS_Entries";
        public const string CSO_Entries = "CSO_Entries";
        public const string EAN_Entries = "EAN_Animations";
        public const string ERS_Entries = "ERS_Entry";
        public const string EEPK_Effect = "EEPK_Effect";
        public const string PSC_Spec_Entry = "PSC_Entry";
        public const string AUR_Aura = "AUR_Aura";
        public const string AUR_Chara = "AUR_Chara";
        public const string PUP_Entry = "PUP_Entry";
        public const string TSD_Trigger = "TSD_Trigger";
        public const string TSD_Global = "TSD_Global";
        public const string TSD_Constant = "TSD_Constant";
        public const string TSD_Zone = "TSD_Zone";
        public const string TSD_Event = "TSD_Event";
        public const string TNL_Character = "TNL_Character";
        public const string TNL_Teacher = "TNL_Teacher";
        public const string TNL_Object = "TNL_Object";
        public const string TNL_Action = "TNL_Action";
        public const string EMB_Entry = "EMB_Entry";
        public const string QXD_Quest = "QXD_Quest";
        public const string QXD_Character1 = "QXD_Character1";
        public const string QXD_Character2 = "QXD_Character2";
        public const string QXD_Collection = "QXD_Collection";
        public const string ACB_Cue = "ACB_Cue";
        public const string ACB_Voice_Cue = "ACB_Voice_Cue";
        public const string PAL_Entry = "PAL_Entry";
        public const string QSF_Entry = "QSF_Entry";
        public const string QBT_NormalDialogue = "QBT_NormalDialogue";
        public const string QBT_InteractiveDialogue = "QBT_InteractiveDialogue";
        public const string QBT_SpecialDialogue = "QBT_SpecialDialogue";
        public const string QSL_Entry = "QSL_Entry";
        public const string QED_Entry = "QED_Entry";
        public const string DML_Entry = "DML_Entry";
        public const string TNN_Tutorial = "TNN_Tutorial";
        public const string ODF_Entry = "ODF_Entry";
        public const string PSO_Entry = "PSO_Entry";
        public const string BCM_Entry = "BCM_Entry";

        public const string TTB_Entry = "TTB_Entry";
        public const string TTC_Entry = "TTC_Entry";
        public const string SEV_Entry = "SEV_Entry";
        public const string HCI_Entry = "HCI_Entry";
        public const string CML_Entry = "CML_Entry";
        public const string IKD_Entry = "IKD_Entry";
        public const string OCS_Skill = "OCS_Skill";
        public const string OCO_Entry = "OCO_Entry";
        public const string OCT_Entry = "OCT_Entry";
        public const string OCP_Entry = "OCP_Entry";
        public const string QML_Entry = "QML_Entry";
        public const string CST_Entry = "CST_Entry";
        public const string CharaSlotEntry = "CharaSlotEntry";
        public const string StageSlotEntry = "StageSlotEntry";
        public const string StageDefEntry = "StageDefEntry";

        //Prebaked
        public const string PrebakedCusAura = "PrebakedCusAura";
        public const string PrebakedBodyShape = "PrebakedBodyShape";
        public const string PrebakedAlias = "PrebakedAlias";
        public const string PrebakedOzarus = "PrebakedOzarus";
        public const string PrebakedCellMax = "PrebakedCelMax";
        public const string PrebakedAutoBattlePortrait = "PrebakedAutoBattlePortrait";
        public const string PrebakedAnyDualSkillList = "PrebakedAnyDualSkillList";
        public const string PrebakedAuraExtraData = "PrebakedAuraExtraData";


        public static string GetBcsPartColor(string id) { return string.Format("{0}{1}", BCS_PartColors, id); }
        public static string GetBcsPartColorOverwrite(string id) { return string.Format("{0}{1}", BCS_PartColorsOverwrite, id); }
        public static string GetPscEntry(string charaID) { return string.Format("{0}_{1}", PSC_Spec_Entry, charaID); }
    }
}
