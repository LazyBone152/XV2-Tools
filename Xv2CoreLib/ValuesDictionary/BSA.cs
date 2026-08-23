using System.Collections.Generic;
using Xv2CoreLib.BSA;

namespace Xv2CoreLib.ValuesDictionary
{
    // BSA numbers EepkType and AcbType differently to BAC, so these cannot be shared with ValuesDictionary.BAC.
    // BSA has no AwokenSkill = 12, and BSA AcbType 3 is Skill_SE while BAC AcbType 3 is Character_VOX.
    public static class BSA
    {
        public static Dictionary<EepkType, string> EepkType { get; private set; } = new Dictionary<EepkType, string>()
        {
            { Xv2CoreLib.BSA.EepkType.Common, "Common" },
            { Xv2CoreLib.BSA.EepkType.StageBG, "Stage BG" },
            { Xv2CoreLib.BSA.EepkType.Character, "Character" },
            { Xv2CoreLib.BSA.EepkType.AwokenSkill, "Awoken Skill" },
            { Xv2CoreLib.BSA.EepkType.SuperSkill, "Super Skill" },
            { Xv2CoreLib.BSA.EepkType.UltimateSkill, "Ultimate Skill" },
            { Xv2CoreLib.BSA.EepkType.EvasiveSkill, "Evasive Skill" },
            { Xv2CoreLib.BSA.EepkType.KiBlastSkill, "Ki Blast Skill" },
            { Xv2CoreLib.BSA.EepkType.Stage, "Stage" }
        };

        public static Dictionary<uint, string> CommonEepkType { get; private set; } = new Dictionary<uint, string>()
        {
            { 0, "BTL_CMN" },
            { 1, "BTL_AURA" },
            { 2, "BTL_KDN" },
            { 6, "BTL_CMN2" },
            { 3, "lby_cmn/LBY_CMN" },
            { 4, "TTL/TTL" },
            { 5, "ttl_lby/TTL_LBY" }
        };

        public static Dictionary<AcbType, string> AcbType { get; private set; } = new Dictionary<AcbType, string>()
        {
            { Xv2CoreLib.BSA.AcbType.Common_SE, "Common SE" },
            { Xv2CoreLib.BSA.AcbType.Chara_SE, "Character SE" },
            { Xv2CoreLib.BSA.AcbType.Skill_SE, "Skill SE" }
        };

        public static Dictionary<Switch, string> Switch { get; private set; } = new Dictionary<Switch, string>()
        {
            { Xv2CoreLib.BSA.Switch.On, "On" },
            { Xv2CoreLib.BSA.Switch.Off, "Off" }
        };

        public static Dictionary<int, string> SignalDeliveryMode { get; private set; } = new Dictionary<int, string>()
        {
            { 0, "Broadcast" },
            { 1, "Same-Context Highest Priority" }
        };

        public static Dictionary<ProjectileProtectionOperation, string> ProjectileProtectionState { get; private set; } = new Dictionary<ProjectileProtectionOperation, string>()
        {
            { Xv2CoreLib.BSA.ProjectileProtectionOperation.EnableOrReplace, "On" },
            { Xv2CoreLib.BSA.ProjectileProtectionOperation.DisableWithoutSignal, "Off" }
        };

        public static Dictionary<SpatialEffectGeometryMode, string> EffectPlacementMode { get; private set; } = new Dictionary<SpatialEffectGeometryMode, string>()
        {
            { Xv2CoreLib.BSA.SpatialEffectGeometryMode.Default, "Default Placement" },
            { Xv2CoreLib.BSA.SpatialEffectGeometryMode.DistanceRelative, "Distance-Based Placement" },
            { Xv2CoreLib.BSA.SpatialEffectGeometryMode.FullVector, "Explicit Vector Placement" }
        };

        public static Dictionary<float, string> AdditionalSelectorCoverage { get; private set; } = new Dictionary<float, string>()
        {
            { 0f, "None" },
            { 1f, "Selectors 4 and 5" },
            { 2f, "Selector 6" },
            { 3f, "Selectors 4, 5, and 6" }
        };
    }
}
