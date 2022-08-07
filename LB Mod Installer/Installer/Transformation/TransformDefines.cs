using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using Xv2CoreLib.Eternity;
using Xv2CoreLib.BAC;
using Xv2CoreLib.PUP;

namespace LB_Mod_Installer.Installer.Transformation
{
    [YAXSerializeAs("TransformDefines")]
    public class TransformDefines
    {

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Transformation")]
        public List<TransformDefine> Transformations { get; set; } 

    }

    [YAXSerializeAs("Transformation")]
    public class TransformDefine
    {
        //Hard-coded BAC entry keys for entries that are used to create BAC / BCM files.
        public const string BAC_PAGE_SE_CALLBACK_KEY = "BAC_PAGE_CHANGE_SE_CALLBACK_KEY";
        public const string BAC_HOLD_DOWN_LOOP_KEY = "BAC_HOLD_DOWN_LOOP_KEY";
        public const string BAC_UNTRANSFORM_KEY = "BAC_UNTRANSFORM_KEY";
        public const string BAC_REVERT_LOOP_KEY = "BAC_REVERT_LOOP_KEY";
        public const string BAC_NOT_ALLOWED_SE_CALLBACK_KEY = "BAC_NOT_ALLOWED_SE_CALLBACK_KEY";

        //BAC Index of hard-coded entries (as skills, 100 is the max amount of entries so they cant be any greater)
        public const int BAC_PAGE_SE_CALLBACK_IDX = 95;
        public const int BAC_HOLD_DOWN_LOOP_IDX = 96;
        public const int BAC_UNTRANSFORM_IDX = 97;
        public const int BAC_REVERT_IDX = 98;
        public const int BAC_NOT_ALLOWED_SE_CALLBACK_IDX = 99;

        [YAXAttributeForClass]
        public string Key { get; set; }

        [YAXAttributeForClass]
        public int BacEntry { get; set; }
        [YAXAttributeForClass]
        public string BacPath { get; set; }

        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (uint)0)]
        public uint KiRequired { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (uint)0)]
        public uint KiCost { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0f)]
        public float HealthRequired { get; set; }

        [YAXDontSerialize]
        public BAC_Entry BacEntryInstance = null;
    }
}
