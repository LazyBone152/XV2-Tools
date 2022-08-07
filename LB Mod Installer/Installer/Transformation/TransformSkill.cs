using System;
using System.Collections.Generic;
using System.Linq;
using YAXLib;
using Xv2CoreLib.CUS;

namespace LB_Mod_Installer.Installer.Transformation
{
    public class TransformSkill
    {
        [YAXAttributeForClass]
        public string SkillCode { get; set; }
        [YAXAttributeForClass]
        public CusRaceLock RaceLock { get; set; }

        //Localization keys
        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        [YAXAttributeFor("Desc")]
        [YAXSerializeAs("value")]
        public string Info { get; set; }


        [YAXAttributeFor("BuyPrice")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 1000)]
        public int BuyPrice { get; set; } = -1;

        //Default values. These are overriden by those defined on stages.
        [YAXAttributeFor("CusAura")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = -1)]
        public int CusAura { get; set; } = -1;
        [YAXAttributeFor("PartSet")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = -1)]
        public int PartSet { get; set; } = -1;
        [YAXAttributeFor("CharaSwapId")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = -1)]
        public int CharaSwapId { get; set; } = -1;

        //Files (path to put in CUS skill entry, the files will need to be installed via the installer normally)
        [YAXAttributeFor("VfxPath")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string VfxPath { get; set; }
        [YAXAttributeFor("EanPath")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Error)]
        public string EanPath { get; set; }
        [YAXAttributeFor("CamEanPath")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Error)]
        public string CamEanPath { get; set; }
        [YAXAttributeFor("SeAcbPath")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string SeAcbPath { get; set; }
        [YAXAttributeFor("VoxAcbPath")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string VoxAcbPath { get; set; } //Follows the same naming rules as X2M skill vox 

        /// <summary>
        /// Defines all stages to be used in the skill.
        /// </summary>
        public List<TransformStage> Stages { get; set; }

        /// <summary>
        /// Defines how stages are accessed.
        /// </summary>
        public List<TransformState> TransformStates { get; set; } 

        [YAXDontSerialize]
        public int NumStages => HasMoveSkillSetChange() ? Stages.Count + 1 : Stages.Count;

        public int GetMaxKiRequired()
        {
            int ki = 0;

            foreach(var state in TransformStates)
            {
                if(state.TransformOptions != null)
                {
                    foreach (var stage in state.TransformOptions)
                    {
                        if (stage.KiRequired > ki)
                            ki = (int)stage.KiRequired;
                    }
                }
            }

            return ki;
        }

        public int IndexOfMoveSkillSetChange()
        {
            var movesetChange = Stages.FirstOrDefault(x => x.MovesetChange || x.SkillsetChange != -1);

            if (movesetChange != null)
                return movesetChange.StageIndex;

            return -1;
        }
    
        public bool HasMoveSkillSetChange()
        {
            return IndexOfMoveSkillSetChange() != -1;
        }
        
        public int GetTransStage(int stageIndex)
        {
            if (HasMoveSkillSetChange())
                return stageIndex + 1;
            else
                return stageIndex;
        }
   
        public int GetSkillSetChangeId()
        {
            int idx = IndexOfMoveSkillSetChange();

            if (idx != -1)
                return Stages[idx].SkillsetChange;

            return -1;
        }
        
    }

    public class TransformStage
    {
        [YAXAttributeForClass]
        public string Key { get; set; }
        [YAXAttributeForClass]
        public int StageIndex { get; set; }

        //Optional:
        //Both of these force the stage to activate on Index 0
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = false)]
        public bool MovesetChange { get; set; } //AFTER_BAC, AFTER_BCM
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = -1)]
        public int SkillsetChange { get; set; } = -1; //Preset ID (-1 = no change)
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = true)]
        public bool DeactivateFirst { get; set; } = true;


    }

    public class TransformState
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (uint)0)]
        public uint KiRequired { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0f)]
        public float HealthRequired { get; set; }

        [YAXDontSerializeIfNull]
        public List<TransformOption> TransformOptions { get; set; } 
        [YAXDontSerializeIfNull]
        public List<TransformOption> RevertOptions { get; set; }

        public bool HasTransformOptions()
        {
            return TransformOptions?.Count > 0;
        }

        public bool HasRevertOptions()
        {
            return RevertOptions?.Count > 0;
        }
    }

    public class TransformOption
    {
        [YAXAttributeForClass]
        public int StageIndex { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (uint)0)]
        public uint KiRequired { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (uint)0)]
        public uint KiCost { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0f)]
        public float HealthRequired { get; set; }
    }

}
