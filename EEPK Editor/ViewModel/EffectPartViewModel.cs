using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.EEPK.EffectPart;

namespace EEPK_Organiser.ViewModel
{
    public class EffectPartViewModel : ObservableObject, IDisposable
    {
        private EffectPart effectPart;

        public bool IsNotTBIND { get { return effectPart.AssetType != AssetType.TBIND; } }

        public ushort StartTime
        {
            get
            {
                return effectPart.StartTime;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.StartTime), effectPart, effectPart.StartTime, value, "Start Time"));
                effectPart.StartTime = value;
            }
        }
        public Attachment Attachment
        {
            get
            {
                return effectPart.AttachementType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.AttachementType), effectPart, effectPart.AttachementType, value, "Attachment"));
                effectPart.AttachementType = value;
            }
        }
        public OrientationType OrientationType
        {
            get => effectPart.Orientation;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.Orientation), effectPart, effectPart.Orientation, value, "Orientation Type"));
                effectPart.Orientation = value;
            }
        }
        public byte Deactivation
        {
            get => (byte)effectPart.Deactivation;
            set
            {
                DeactivationMode newValue = (DeactivationMode)value;

                if(newValue != effectPart.Deactivation)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.Deactivation), effectPart, effectPart.Deactivation, newValue, "Deactivation"));
                    effectPart.Deactivation = newValue;
                }
            }
        }
        public byte I_06
        {
            get
            {
                return effectPart.I_06;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_06), effectPart, effectPart.I_06, value, "I_06"));
                effectPart.I_06 = value;
            }
        }
        public byte I_07
        {
            get
            {
                return effectPart.I_07;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_07), effectPart, effectPart.I_07, value, "I_07"));
                effectPart.I_07 = value;
            }
        }
        public int I_08
        {
            get
            {
                return effectPart.I_08;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_08), effectPart, effectPart.I_08, value, "I_08"));
                effectPart.I_08 = value;
            }
        }
        public int I_12
        {
            get
            {
                return effectPart.I_12;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_12), effectPart, effectPart.I_12, value, "I_12"));
                effectPart.I_12 = value;
            }
        }
        public int I_16
        {
            get
            {
                return effectPart.I_16;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_16), effectPart, effectPart.I_16, value, "I_16"));
                effectPart.I_16 = value;
            }
        }
        public int I_20
        {
            get
            {
                return effectPart.I_20;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_20), effectPart, effectPart.I_20, value, "I_20"));
                effectPart.I_20 = value;
            }
        }
        public float F_24
        {
            get
            {
                return effectPart.AvoidSphere;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.AvoidSphere), effectPart, effectPart.AvoidSphere, value, "AvoidSphere"));
                effectPart.AvoidSphere = value;
            }
        }
        public bool I_32_0
        {
            get
            {
                return effectPart.PositionUpdate;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.PositionUpdate), effectPart, effectPart.PositionUpdate, value, "MoveWithBone"));
                effectPart.PositionUpdate = value;
            }
        }
        public bool I_32_1
        {
            get
            {
                return effectPart.RotateUpdate;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.RotateUpdate), effectPart, effectPart.RotateUpdate, value, "RotateWithBone"));
                effectPart.RotateUpdate = value;
            }
        }
        public bool I_32_2
        {
            get
            {
                return effectPart.InstantUpdate;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.InstantUpdate), effectPart, effectPart.InstantUpdate, value, "InstantMoveAndRotate"));
                effectPart.InstantUpdate = value;
            }
        }
        public bool I_32_3
        {
            get
            {
                return effectPart.OnGroundOnly;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.OnGroundOnly), effectPart, effectPart.OnGroundOnly, value, "OnGroundOnly"));
                effectPart.OnGroundOnly = value;
            }
        }
        public bool I_32_4
        {
            get
            {
                return effectPart.UseTimeScale;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.UseTimeScale), effectPart, effectPart.UseTimeScale, value, "UseTimeScale"));
                effectPart.UseTimeScale = value;
            }
        }
        public bool I_32_5
        {
            get
            {
                return effectPart.UseBoneDirection;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.UseBoneDirection), effectPart, effectPart.UseBoneDirection, value, "UseBoneDirection"));
                effectPart.UseBoneDirection = value;
            }
        }
        public bool I_32_6
        {
            get
            {
                return effectPart.EnableRotationValues;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.EnableRotationValues), effectPart, effectPart.EnableRotationValues, value, "UseBoneToCameraDirection"));
                effectPart.EnableRotationValues = value;
            }
        }
        public bool I_32_7
        {
            get
            {
                return effectPart.UseScreenCenterToBoneDirection;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.UseScreenCenterToBoneDirection), effectPart, effectPart.UseScreenCenterToBoneDirection, value, "UseSceneCenterToBoneDirection"));
                effectPart.UseScreenCenterToBoneDirection = value;
            }
        }
        public short I_34
        {
            get
            {
                return effectPart.I_34;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_34), effectPart, effectPart.I_34, value, "I_34"));
                effectPart.I_34 = value;
            }
        }
        public bool I_36_1
        {
            get
            {
                return effectPart.I_36_1;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_36_1), effectPart, effectPart.I_36_1, value, "Flag36_Unk1"));
                effectPart.I_36_1 = value;
            }
        }
        public bool I_36_2
        {
            get
            {
                return effectPart.I_36_2;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_36_2), effectPart, effectPart.I_36_2, value, "Flag36_Unk2"));
                effectPart.I_36_2 = value;
            }
        }
        public bool I_36_3
        {
            get
            {
                return effectPart.I_36_3;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_36_3), effectPart, effectPart.I_36_3, value, "Flag36_Unk3"));
                effectPart.I_36_3 = value;
            }
        }
        public bool I_36_4
        {
            get
            {
                return effectPart.I_36_4;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_36_4), effectPart, effectPart.I_36_4, value, "Flag36_Unk4"));
                effectPart.I_36_4 = value;
            }
        }
        public bool I_36_5
        {
            get
            {
                return effectPart.I_36_5;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_36_5), effectPart, effectPart.I_36_5, value, "Flag36_Unk5"));
                effectPart.I_36_5 = value;
            }
        }
        public bool I_36_6
        {
            get
            {
                return effectPart.I_36_6;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_36_6), effectPart, effectPart.I_36_6, value, "Flag36_Unk6"));
                effectPart.I_36_6 = value;
            }
        }
        public bool I_36_7
        {
            get
            {
                return effectPart.I_36_7;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_36_7), effectPart, effectPart.I_36_7, value, "Flag36_Unk7"));
                effectPart.I_36_7 = value;
            }
        }
        public bool I_37_0
        {
            get
            {
                return effectPart.I_37_0;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_37_0), effectPart, effectPart.I_37_0, value, "Flag37_Unk0"));
                effectPart.I_37_0 = value;
            }
        }
        public bool I_37_1
        {
            get
            {
                return effectPart.I_37_1;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_37_1), effectPart, effectPart.I_37_1, value, "Flag37_Unk1"));
                effectPart.I_37_1 = value;
            }
        }
        public bool I_37_2
        {
            get
            {
                return effectPart.I_37_2;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_37_2), effectPart, effectPart.I_37_2, value, "Flag37_Unk2"));
                effectPart.I_37_2 = value;
            }
        }
        public bool I_37_3
        {
            get
            {
                return effectPart.I_37_3;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_37_3), effectPart, effectPart.I_37_3, value, "Flag37_Unk3"));
                effectPart.I_37_3 = value;
            }
        }
        public bool I_37_4
        {
            get
            {
                return effectPart.I_37_4;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_37_4), effectPart, effectPart.I_37_4, value, "Flag37_Unk4"));
                effectPart.I_37_4 = value;
            }
        }
        public bool I_37_5
        {
            get
            {
                return effectPart.I_37_5;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_37_5), effectPart, effectPart.I_37_5, value, "Flag37_Unk5"));
                effectPart.I_37_5 = value;
            }
        }
        public bool I_37_6
        {
            get
            {
                return effectPart.I_37_6;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_37_6), effectPart, effectPart.I_37_6, value, "Flag37_Unk6"));
                effectPart.I_37_6 = value;
            }
        }
        public bool I_37_7
        {
            get
            {
                return effectPart.I_37_7;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_37_7), effectPart, effectPart.I_37_7, value, "Flag37_Unk7"));
                effectPart.I_37_7 = value;
            }
        }
        public byte I_38_a
        {
            get
            {
                return effectPart.I_38_a;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_38_a), effectPart, effectPart.I_38_a, value, "I_38_a"));
                effectPart.I_38_a = value;
            }
        }
        public byte I_38_b
        {
            get
            {
                return effectPart.I_38_b;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_38_b), effectPart, effectPart.I_38_b, value, "I_38_b"));
                effectPart.I_38_b = value;
            }
        }
        public bool I_39_0
        {
            get
            {
                return effectPart.NoGlare;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.NoGlare), effectPart, effectPart.NoGlare, value, "NoGlare"));
                effectPart.NoGlare = value;
            }
        }
        public bool I_39_1
        {
            get
            {
                return effectPart.I_39_1;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_39_1), effectPart, effectPart.I_39_1, value, "Flag39_Unk1"));
                effectPart.I_39_1 = value;
            }
        }
        public bool I_39_2
        {
            get
            {
                return effectPart.InverseTransparentDrawOrder;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.InverseTransparentDrawOrder), effectPart, effectPart.InverseTransparentDrawOrder, value, "InverseTransparentDrawOrder"));
                effectPart.InverseTransparentDrawOrder = value;
            }
        }
        public bool I_39_3
        {
            get
            {
                return effectPart.RelativePositionZ_To_AbsolutePositionZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.RelativePositionZ_To_AbsolutePositionZ), effectPart, effectPart.RelativePositionZ_To_AbsolutePositionZ, value, "ScaleZ_To_BonePositionZ"));
                effectPart.RelativePositionZ_To_AbsolutePositionZ = value;
            }
        }
        public bool I_39_4
        {
            get
            {
                return effectPart.ScaleZ_To_BonePositionZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.ScaleZ_To_BonePositionZ), effectPart, effectPart.ScaleZ_To_BonePositionZ, value, "ScaleZ_To_BonePositionZ"));
                effectPart.ScaleZ_To_BonePositionZ = value;
            }
        }
        public bool I_39_5
        {
            get
            {
                return effectPart.I_39_5;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_39_5), effectPart, effectPart.I_39_5, value, "Flag39_Unk5"));
                effectPart.I_39_5 = value;
            }
        }
        public bool I_39_6
        {
            get
            {
                return effectPart.I_39_6;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_39_6), effectPart, effectPart.I_39_6, value, "Flag39_Unk6"));
                effectPart.I_39_6 = value;
            }
        }
        public bool I_39_7
        {
            get
            {
                return effectPart.ObjectOrientation_To_XXXX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.ObjectOrientation_To_XXXX), effectPart, effectPart.ObjectOrientation_To_XXXX, value, "ObjectOrientation_To_XXXX"));
                effectPart.ObjectOrientation_To_XXXX = value;
            }
        }
        public float POSITION_X
        {
            get
            {
                return effectPart.PositionX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.PositionX), effectPart, effectPart.PositionX, value, "Position X"));
                effectPart.PositionX = value;
            }
        }
        public float POSITION_Y
        {
            get
            {
                return effectPart.PositionY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.PositionY), effectPart, effectPart.PositionY, value, "Position Y"));
                effectPart.PositionY = value;
            }
        }
        public float POSITION_Z
        {
            get
            {
                return effectPart.PositionZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.PositionZ), effectPart, effectPart.PositionZ, value, "Position Z"));
                effectPart.PositionZ = value;
            }
        }
        public float F_52
        {
            get
            {
                return effectPart.RotationX_Min;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.RotationX_Min), effectPart, effectPart.RotationX_Min, value, "Rotation X (Min)"));
                effectPart.RotationX_Min = value;
            }
        }
        public float F_56
        {
            get
            {
                return effectPart.RotationX_Max;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.RotationX_Max), effectPart, effectPart.RotationX_Max, value, "Rotation X (Max)"));
                effectPart.RotationX_Max = value;
            }
        }
        public float F_60
        {
            get
            {
                return effectPart.RotationY_Min;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.RotationY_Min), effectPart, effectPart.RotationY_Min, value, "Rotation Y (Min)"));
                effectPart.RotationY_Min = value;
            }
        }
        public float F_64
        {
            get
            {
                return effectPart.RotationY_Max;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.RotationY_Max), effectPart, effectPart.RotationY_Max, value, "Rotation Y (Max)"));
                effectPart.RotationY_Max = value;
            }
        }
        public float F_68
        {
            get
            {
                return effectPart.RotationZ_Min;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.RotationZ_Min), effectPart, effectPart.RotationZ_Min, value, "Rotation Z (Min)"));
                effectPart.RotationZ_Min = value;
            }
        }
        public float F_72
        {
            get
            {
                return effectPart.RotationZ_Max;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.RotationZ_Max), effectPart, effectPart.RotationZ_Max, value, "Rotation Z (Max)"));
                effectPart.RotationZ_Max = value;
            }
        }
        public float SIZE_1
        {
            get
            {
                return effectPart.ScaleMin;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.ScaleMin), effectPart, effectPart.ScaleMin, value, "Scale (Min)"));
                effectPart.ScaleMin = value;
            }
        }
        public float SIZE_2
        {
            get
            {
                return effectPart.ScaleMax;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.ScaleMax), effectPart, effectPart.ScaleMax, value, "Scale (Max)"));
                effectPart.ScaleMax = value;
            }
        }
        public float F_84
        {
            get
            {
                return effectPart.NearFadeDistance;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.NearFadeDistance), effectPart, effectPart.NearFadeDistance, value, "Near Fade Distance"));
                effectPart.NearFadeDistance = value;
            }
        }
        public float F_88
        {
            get
            {
                return effectPart.FarFadeDistance;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.FarFadeDistance), effectPart, effectPart.FarFadeDistance, value, "Far Fade Distance"));
                effectPart.FarFadeDistance = value;
            }
        }
        public ushort I_30
        {
            get
            {
                return effectPart.EMA_AnimationIndex;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.EMA_AnimationIndex), effectPart, effectPart.EMA_AnimationIndex, value, "EMA Animation Index"));
                effectPart.EMA_AnimationIndex = value;
            }
        }
        public ushort I_92
        {
            get
            {
                return effectPart.EMA_LoopStartFrame;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.EMA_LoopStartFrame), effectPart, effectPart.EMA_LoopStartFrame, value, "Loop Start Frame"));
                effectPart.EMA_LoopStartFrame = value;
            }
        }
        public ushort I_94
        {
            get
            {
                return effectPart.EMA_LoopEndFrame;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.EMA_LoopEndFrame), effectPart, effectPart.EMA_LoopEndFrame, value, "Loop End Frame"));
                effectPart.EMA_LoopEndFrame = value;
            }
        }
        public bool I_36_0
        {
            get
            {
                return effectPart.EMA_Loop;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.EMA_Loop), effectPart, effectPart.EMA_Loop, value, "EMA Loop"));
                effectPart.EMA_Loop = value;
            }
        }
        public string ESK
        {
            get
            {
                //WPF hates this because its not an observable collection
                //if (!EffectPart.CommonBones.Contains(effectPart.ESK))
                //    EffectPart.CommonBones.Add(effectPart.ESK);

                return effectPart.ESK;
            }
            set
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                undos.Add(new UndoableProperty<EffectPart>(nameof(effectPart.ESK), effectPart, effectPart.ESK, value));
                undos.Add(new UndoActionDelegate(effectPart, nameof(effectPart.RefreshDetails), true));

                UndoManager.Instance.AddCompositeUndo(undos, "BoneToAttach", UndoGroup.Effect);
                effectPart.ESK = value;

                effectPart.RefreshDetails();
            }
        }

        public bool IsUsingEma => effectPart?.AssetType == AssetType.EMO || effectPart?.AssetType == AssetType.LIGHT;
        public bool IsNotCBIND => effectPart?.AssetType != AssetType.CBIND;

        public EffectPartViewModel(EffectPart _effectPart)
        {
            effectPart = _effectPart;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object sender, System.EventArgs e)
        {
            UpdateProperties();
        }

        public void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => StartTime);
            RaisePropertyChanged(() => Attachment);
            RaisePropertyChanged(() => OrientationType);
            RaisePropertyChanged(() => Deactivation);
            RaisePropertyChanged(() => I_06);
            RaisePropertyChanged(() => I_07);
            RaisePropertyChanged(() => I_08);
            RaisePropertyChanged(() => I_12);
            RaisePropertyChanged(() => I_16);
            RaisePropertyChanged(() => I_20);
            RaisePropertyChanged(() => F_24);
            RaisePropertyChanged(() => I_32_0);
            RaisePropertyChanged(() => I_32_1);
            RaisePropertyChanged(() => I_32_2);
            RaisePropertyChanged(() => I_32_3);
            RaisePropertyChanged(() => I_32_4);
            RaisePropertyChanged(() => I_32_5);
            RaisePropertyChanged(() => I_32_6);
            RaisePropertyChanged(() => I_32_7);
            RaisePropertyChanged(() => I_34);
            RaisePropertyChanged(() => I_36_0);
            RaisePropertyChanged(() => I_36_1);
            RaisePropertyChanged(() => I_36_2);
            RaisePropertyChanged(() => I_36_3);
            RaisePropertyChanged(() => I_36_4);
            RaisePropertyChanged(() => I_36_5);
            RaisePropertyChanged(() => I_36_6);
            RaisePropertyChanged(() => I_36_7);
            RaisePropertyChanged(() => I_37_0);
            RaisePropertyChanged(() => I_37_1);
            RaisePropertyChanged(() => I_37_2);
            RaisePropertyChanged(() => I_37_3);
            RaisePropertyChanged(() => I_37_4);
            RaisePropertyChanged(() => I_37_5);
            RaisePropertyChanged(() => I_37_6);
            RaisePropertyChanged(() => I_37_7);
            RaisePropertyChanged(() => I_38_a);
            RaisePropertyChanged(() => I_38_b);
            RaisePropertyChanged(() => I_39_0);
            RaisePropertyChanged(() => I_39_1);
            RaisePropertyChanged(() => I_39_2);
            RaisePropertyChanged(() => I_39_3);
            RaisePropertyChanged(() => I_39_4);
            RaisePropertyChanged(() => I_39_5);
            RaisePropertyChanged(() => I_39_6);
            RaisePropertyChanged(() => I_39_7);
            RaisePropertyChanged(() => POSITION_X);
            RaisePropertyChanged(() => POSITION_Y);
            RaisePropertyChanged(() => POSITION_Z);
            RaisePropertyChanged(() => F_52);
            RaisePropertyChanged(() => F_56);
            RaisePropertyChanged(() => F_60);
            RaisePropertyChanged(() => F_64);
            RaisePropertyChanged(() => F_68);
            RaisePropertyChanged(() => F_72);
            RaisePropertyChanged(() => SIZE_1);
            RaisePropertyChanged(() => SIZE_2);
            RaisePropertyChanged(() => F_84);
            RaisePropertyChanged(() => F_88);
            RaisePropertyChanged(() => I_92);
            RaisePropertyChanged(() => I_94);
            RaisePropertyChanged(() => ESK);
            RaisePropertyChanged(() => IsNotTBIND);
            RaisePropertyChanged(() => IsNotCBIND);
            RaisePropertyChanged(() => IsUsingEma);
        }

    }
}
