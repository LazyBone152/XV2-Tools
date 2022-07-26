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

        public List<TransformStage> Stages { get; set; }

        public List<TransformOption> InitialTransformOptions { get; set; }
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
        public int KiRequired { get; set; }
        [YAXAttributeForClass]
        public int KiCost { get; set; }
    }

}
