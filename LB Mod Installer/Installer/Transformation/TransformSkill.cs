using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using Xv2CoreLib.Eternity;

namespace LB_Mod_Installer.Installer.Transformation
{
    public class TransformSkill
    {
        [YAXAttributeForClass]
        public string ThreeLetterCode { get; set; }
        [YAXAttributeForClass]
        public byte RaceLock { get; set; }

        //Localization keys
        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        [YAXAttributeFor("Desc")]
        [YAXSerializeAs("value")]
        public string Info { get; set; }

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

        public int NumStages => HasMoveSkillSetChange() ? Stages.Count + 1 : Stages.Count;

        public int GetMaxKiRequired()
        {
            if (TransformStates.Count - 2 < 0) return (int)TransformStates[0].KiRequired;

            return (int)TransformStates[TransformStates.Count - 2].KiRequired;
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


    }

    public class TransformState
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0)]
        public uint KiRequired { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0f)]
        public float HealthRequired { get; set; }

        [YAXDontSerializeIfNull]
        public List<TransformOption> TransformOptions { get; set; }
        [YAXDontSerializeIfNull]
        public List<TransformOption> RevertOptions { get; set; }
    }

    public class TransformOption
    {
        [YAXAttributeForClass]
        public int StageIndex { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0)]
        public uint KiRequired { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0)]
        public uint KiCost { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0f)]
        public float HealthRequired { get; set; }
    }

}
