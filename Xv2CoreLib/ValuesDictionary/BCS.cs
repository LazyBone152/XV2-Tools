using System.Collections.Generic;

namespace Xv2CoreLib.ValuesDictionary
{
    public static class BCS
    {

        public static List<string> CommonAttachBones { get; private set; } = new List<string>()
        {
            "b_C_Base",
            "b_C_Pelvis",
            "b_C_Head",
            "b_C_Chest",
            "b_C_Spine1",
            "b_C_Spine2",
            "b_C_Neck1"
        };

        public static List<string> CommonBoneScales { get; private set; } = new List<string>()
        {
            "b_C_Base",
            "b_C_Head",
            "b_C_Neck1",
            "b_C_Spine1",
            "b_C_Spine2",
            "b_C_Pelvis",
            "b_C_Chest",
            "b_R_Shoulder",
            "b_L_Shoulder",
            "b_R_Arm1",
            "b_R_Arm2",
            "b_L_Arm1",
            "b_L_Arm2",
            "b_R_Hand",
            "b_L_Hand",
            "b_R_Leg1",
            "b_R_Leg2",
            "b_L_Leg1",
            "b_L_Leg2",
            "b_R_Foot",
            "b_L_Foot",
            "b_R_ArmHelper",
            "b_L_ArmHelper",
            "b_R_LegHelper",
            "b_L_LegHelper"
        };

        public static List<string> CommonSkeletonDataBones { get; private set; } = new List<string>()
        {
            "b_C_Base",
            "b_C_Head",
            "b_C_Neck1",
            "b_C_Spine1",
            "b_C_Pelvis",
            "b_C_Chest",
            "b_R_Leg1",
            "b_L_Leg1",
            "b_R_Leg2",
            "b_L_Leg2",
            "b_R_Arm1",
            "b_L_Arm1",
            "b_R_Arm2",
            "b_L_Arm2",
            "b_R_Foot",
            "b_L_Foot",
            "b_R_Hand",
            "b_L_Hand",
            "g_R_Hand",
            "g_L_Hand",
            "x_R_ArmZoom7",
            "x_L_ArmZoom7",
            "x_R_ArmZoom3",
            "x_L_ArmZoom3",
            "b_L_Shoulder",
            "b_R_Shoulder",
            "b_L_LegHelper",
            "b_R_LegHelper",
            "a_x_cane_root"
        };

    }
}
