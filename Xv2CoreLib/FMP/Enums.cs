using System;

namespace Xv2CoreLib.FMP
{
    [Flags]
    public enum FmpFlags
    {
        Unk1 = 0x1,
        Unk2 = 0x2,
        Unk3 = 0x4,
        Unk4 = 0x8,
        UseHavok = 0x10,
        UseCompressedHavok = 0x20,
        Unk7 = 0x40,
        Unk8 = 0x80
    }

    public enum HavokTriggerType
    {
        None = 0,
        BroadPhase = 1,
        NarrowPhase = 2,
        ContactSolver = 3
    }

    public enum HavokMotionType : ushort
    {
        Static = 0,
        Kinematic = 1,
        Dynamic = 2
    }

    [Flags]
    public enum ObjectFlags : ushort
    {
        Enabled = 0x1,
        CastShadow = 0x2,
        PerSubmeshCulling = 0x4, //Per part culling (cull each part of the model separately, instead of the whole model at once)
        Instancing = 0x8,
        StaticCollision = 0x10,
        Unk6 = 0x20,
        InstancedDraw = 0x40, //Flags the node to be drawn as instanced geometry. The game will also automatically set this flag if the node has InstanceData. Requires 0x8 as well (game won't auto set this one)

        //Legacy flags
        Unk3 = 0x4, Unk5 = 0x10, Unk7 = 0x40
    }

    [Flags]
    public enum TriggerFlags : ushort
    {
        CollisionBegin = 0x1,
        CollisionEnd = 0x2,
        NodeEnabled = 0x4,
        NodeDisabled = 0x8,
        CollisionContact = 0x10,
        ObjectBreak = 0x20,
        AnimationFinish = 0x40,
        Unk8 = 0x80 //Unused

        //Higher bits are technically valid, but they are never used in any FMP file
    }

    public enum CommandType : short
    {
        TimerSet = -20,
        OccluderDel = -16,
        OccluderAdd = -15,
        MirrorObject = -10,
        MotionSet = -8,
        MotionState = -7,
        EventCall = -5,
        DisableObject = -2,
        EnableObject = -1,
        NoParameter = 0,
        SetSky = 1,
        SetSun = 4,
        AddSound = 5,
        PlaySound3D = 7,
        PlayEffect = 16,
        Water = 100,
        ChangeArea = 101,
        SetLct = 102,
        WaterFall = 103,
        SmallScene = 104,
        SetBreakPartsEffect = 105,
        SetShadowArea = 106,
        SetFlagUseCmnBreakPartsEffect = 107,
        SetBreakPartsInvisible = 108,
        SetMotion = 110,
        LimitMoveInvisible = 112,
        HideObject = 113,
        HitCapsule = 114,
        LockonEnable = 115,
        HenshinImpossible = 116,
        PlayMassiveParticles = 117,
        ScatterFragment = 118,
        ScatterFragmentCustom = 119,
        Camera = 200,
        ChaseStart = 202,
        LockCamera = 204,
        UnlockCamera = 205,
        ChangeFootEffect = 208,
        ChangeInteraction = 209,
        AddPlaneSound = 210,
        ChangeEnvSound = 211
    }
    
    public enum ColliderShapeType
    {
        Box = 1,
        Sphere = 2,
        Cylinder = 3,
        Capsule = 4
    }
}
