using System.Collections.Generic;
using Xv2CoreLib.EEPK;

namespace Xv2CoreLib.ValuesDictionary
{
    public static class EEPK
    {
        public static void AddMissing(EEPK_File eepkFile)
        {
            foreach (var effect in eepkFile.Effects)
            {
                foreach(var effectPart in effect.EffectParts)
                {
                    if (!Deactivation.ContainsKey(effectPart.Deactivation))
                        Deactivation.Add(effectPart.Deactivation, effectPart.Deactivation.ToString());

                    if (!Orientation.ContainsKey(effectPart.Orientation))
                        Orientation.Add(effectPart.Orientation, effectPart.Orientation.ToString());

                    if (!AttachmentTypes.ContainsKey(effectPart.AttachementType))
                        AttachmentTypes.Add(effectPart.AttachementType, effectPart.AttachementType.ToString());
                }
            }
        }

        public static string[] CommonBones { get; private set; } = new string[]
        {
            "b_C_Base",
            "b_C_Pelvis",
            "g_C_Pelvis",
            "b_C_Head",
            "g_C_Head",
            "b_C_Neck1",
            "b_C_Chest",
            "b_C_Spine1",
            "b_C_Spine2",
            "b_C_Hand",
            "b_L_Shoulder",
            "b_L_Arm1",
            "b_L_Arm2",
            "b_L_Elbow",
            "b_L_Hand",
            "g_L_Hand",
            "b_R_Shoulder",
            "b_R_Arm1",
            "b_R_Arm2",
            "b_R_Elbow",
            "b_R_Hand",
            "g_R_Hand",
            "b_L_Leg1",
            "b_L_Leg2",
            "b_L_Knee",
            "b_L_Foot",
            "g_L_Foot",
            "b_L_Toe",
            "b_R_Leg1",
            "b_R_Leg2",
            "b_R_Knee",
            "b_R_Foot",
            "g_R_Foot",
            "b_R_Toe",
            "g_x_LND",
            "TRS",
            "SCENE_ROOT",
            "f_L_Eye",
            "f_R_Eye",
        };

        public static Dictionary<DeactivationMode, string> Deactivation { get; private set; } = new Dictionary<DeactivationMode, string>()
        {
            { DeactivationMode.Never, "Never" },
            { DeactivationMode.Immediate, "Immediate" },
            { DeactivationMode.LoopCancel, "Loop Cancel" }
        };

        public static Dictionary<OrientationType, string> Orientation { get; private set; } = new Dictionary<OrientationType, string>()
        {
            { OrientationType.None, "None" },
            { OrientationType.User, "User" },
            { OrientationType.AttachmentBone, "Bone" },
            { OrientationType.Camera, "Camera" },
            { OrientationType.RotateMovement, "Trail" },
        };

        public static Dictionary<Attachment, string> AttachmentTypes { get; private set; } = new Dictionary<Attachment, string>()
        {
            { Attachment.External, "None" },
            { Attachment.Unk1, "Unk 1" },
            { Attachment.Bone, "Bone" },
            { Attachment.Camera, "Camera" },
        };

        public static string[] DirectionAxis { get; private set; } = new string[]
        {
            "X",
            "Y",
            "-Z"
        };
    }
}
