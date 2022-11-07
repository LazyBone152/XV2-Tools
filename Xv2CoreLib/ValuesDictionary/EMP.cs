using System.Collections.Generic;
using Xv2CoreLib.EMP_NEW;

namespace Xv2CoreLib.ValuesDictionary
{
    public static class EMP
    {
        public static Dictionary<ParticleNodeType, string> NodeType { get; private set; } = new Dictionary<ParticleNodeType, string>()
        {
            { ParticleNodeType.Null , "None" },
            { ParticleNodeType.Emitter , "Emitter" },
            { ParticleNodeType.Emission , "Emission" },
        };

        public static Dictionary<ParticleBillboardType, string> BillboardTypes { get; private set; } = new Dictionary<ParticleBillboardType, string>()
        {
            { ParticleBillboardType.None , "None" },
            { ParticleBillboardType.Camera , "Camera" },
            { ParticleBillboardType.Front , "Front" },
        };

        public static Dictionary<ParticleEmitter.ParticleEmitterShape, string> EmitterShapes { get; private set; } = new Dictionary<ParticleEmitter.ParticleEmitterShape, string>()
        {
            { ParticleEmitter.ParticleEmitterShape.Circle , "Circle" },
            { ParticleEmitter.ParticleEmitterShape.Square , "Square" },
            { ParticleEmitter.ParticleEmitterShape.Sphere , "Sphere" },
            { ParticleEmitter.ParticleEmitterShape.Point , "Point" },
        };

        public static Dictionary<ParticleEmission.ParticleEmissionType, string> EmissionTypes { get; private set; } = new Dictionary<ParticleEmission.ParticleEmissionType, string>()
        {
            { ParticleEmission.ParticleEmissionType.Plane , "Plane" },
            { ParticleEmission.ParticleEmissionType.ConeExtrude , "Cone Extrude" },
            { ParticleEmission.ParticleEmissionType.Mesh , "Static Mesh" },
            { ParticleEmission.ParticleEmissionType.ShapeDraw , "Shape Draw" },
        };

        public static Dictionary<EMP_ScrollState.ScrollTypeEnum, string> ScrollType { get; private set; } = new Dictionary<EMP_ScrollState.ScrollTypeEnum, string>()
        {
            { EMP_ScrollState.ScrollTypeEnum.Static , "Static" },
            { EMP_ScrollState.ScrollTypeEnum.Speed , "Scroll Speed" },
            { EMP_ScrollState.ScrollTypeEnum.SpriteSheet , "Sprite Sheet" },
        };

        public static Dictionary<EMP_TextureSamplerDef.TextureFiltering, string> TextureFiltering { get; private set; } = new Dictionary<EMP_TextureSamplerDef.TextureFiltering, string>()
        {
            { EMP_TextureSamplerDef.TextureFiltering.None , "None" },
            { EMP_TextureSamplerDef.TextureFiltering.Point , "Point" },
            { EMP_TextureSamplerDef.TextureFiltering.Linear , "Linear" },
        };

        public static Dictionary<EMP_TextureSamplerDef.TextureRepitition, string> TextureRepitition { get; private set; } = new Dictionary<EMP_TextureSamplerDef.TextureRepitition, string>()
        {
            { EMP_TextureSamplerDef.TextureRepitition.Wrap , "Wrap" },
            { EMP_TextureSamplerDef.TextureRepitition.Mirror , "Mirror" },
            { EMP_TextureSamplerDef.TextureRepitition.Clamp , "Clamp" },
            { EMP_TextureSamplerDef.TextureRepitition.Border , "Border" }
        };

        //Not exactly EMP... but I'm not making another file just for 1 value
        public static Dictionary<ECF.ECF_Node.PlayMode, string> ECF_LoopMode { get; private set; } = new Dictionary<ECF.ECF_Node.PlayMode, string>()
        {
            { ECF.ECF_Node.PlayMode.NoLoop , "No Loop" },
            { ECF.ECF_Node.PlayMode.Loop , "Loop" },
            { ECF.ECF_Node.PlayMode.Unk0 , "Unk0" },
            { ECF.ECF_Node.PlayMode.Unk1 , "Unk1" },
        };

        //ETR:
        public static Dictionary<ETR.ETR_InterpolationType, string> ETR_InterpolationType { get; private set; } = new Dictionary<ETR.ETR_InterpolationType, string>()
        {
            { ETR.ETR_InterpolationType.ShapeStartToEnd , "Start To End" },
            { ETR.ETR_InterpolationType.Default , "Default" },
            { ETR.ETR_InterpolationType.DefaultEnd , "Default End" },
        };

        public static List<string> ETR_CommonAttachBones { get; private set; } = new List<string>()
        {
            "TRS",
            "b_C_Base",
            "b_C_Pelvis",
            "g_C_Pelvis",
            "b_C_Chest",
            "b_C_Head",
            "g_C_Head",
            "b_R_Shoulder",
            "b_L_Shoulder",
            "b_R_Arm1",
            "b_L_Arm1",
            "b_R_Arm2",
            "b_L_Arm2",
            "b_R_Elbow",
            "b_L_Elbow",
            "b_R_Hand",
            "b_L_Hand",
            "g_R_Hand",
            "g_L_Hand",
            "b_R_Leg1",
            "b_L_Leg1",
            "b_R_Leg2",
            "b_L_Leg2",
            "b_R_Knee",
            "b_L_Knee",
            "g_R_Foot",
            "g_L_Foot",
            "b_R_Foot",
            "b_L_Foot",
            "b_R_Toe",
            "b_L_Toe",
            "h_L_Middle3",
            "f_R_Eye",
            "f_L_Eye",
            "h_L_Pinky3",
            "h_L_Ring3",
            "h_L_Index3",
            "h_L_Thumb3",
            "h_R_Middle3",
            "h_R_Pinky3",
            "h_R_Ring3",
            "h_R_Index3",
            "h_R_Thumb3",
            "h_R_Middle1",
            "X_T_TAIL1",
            "X_T_TAIL2",
            "X_T_TAIL3",
            "X_T_TAIL4",
            "X_T_TAIL5",
            "X_T_TAIL6",
            "X_T_TAIL7",
            "X_T_TAIL8",
            "X_T_TAIL9",
            "X_T_TAIL10",
            "DBL_DBLball01",
            "DBL_DBLball02",
            "DBL_DBLball03",
            "DBL_DBLball04",
            "DBL_DBLball05",
            "DBL_DBLball06",
            "DBL_DBLball07",
            "x_x_DBallS01",
            "x_x_DBallS02",
            "x_x_DBallS03",
            "x_x_DBallS04",
            "x_x_DBallS05",
            "x_x_DBallS06",
            "x_x_DBallS07"
        };

    }
}
