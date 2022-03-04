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
        [YAXAttributeFor("CusAura")]
        [YAXSerializeAs("value")]
        public int CusAura { get; set; }

        //Might refactor these out into localized strings and have a key here
        public string[] Name { get; set; }
        [YAXDontSerializeIfNull]
        public string[] Info { get; set; }

        public List<TransformStage> Stages { get; set; }

        public List<TransformOption> InitialTransformOptions { get; set; }
    }

    public class TransformStage
    {
        [YAXAttributeForClass]
        public string Key { get; set; }
        [YAXAttributeForClass]
        public int StageIndex { get; set; }

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
        public int KiRequire { get; set; }
        [YAXAttributeForClass]
        public int KiCost { get; set; }
    }

}
