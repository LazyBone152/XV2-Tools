using LB_Common.Mvvm;
using System;
using System.Collections.Generic;
using System.Windows;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.EEPK.EffectPart;

namespace EEPK_Organiser.ViewModel
{
    public class EffectPartViewModel : AutoObservableObject
    {

        private readonly EffectPart defaultModel = new EffectPart();
        private EffectPart effectPart;

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
        public DeactivationMode Deactivation
        {
            get => effectPart.Deactivation;
            set
            {
                DeactivationMode newValue = value;

                if(newValue != effectPart.Deactivation)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.Deactivation), effectPart, effectPart.Deactivation, newValue, nameof(effectPart.Deactivation)));
                    effectPart.Deactivation = newValue;
                }
            }
        }
        public byte DirectionAxis
        {
            get
            {
                return effectPart.I_06;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_06), effectPart, effectPart.I_06, value, "Direction Axis"));
                effectPart.I_06 = value;
            }
        }
        public float DepthBias
        {
            get
            {
                return effectPart.DepthBias;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.DepthBias), effectPart, effectPart.DepthBias, value, nameof(effectPart.DepthBias)));
                effectPart.DepthBias = value;
            }
        }
        public bool I_32_0
        {
            get => effectPart.HasFlag1(EepkEffectPartFlags1.PositionUpdate);
            set => SetFlags1(EepkEffectPartFlags1.PositionUpdate, value);
        }
        public bool I_32_1
        {
            get => effectPart.HasFlag1(EepkEffectPartFlags1.RotateUpdate);
            set => SetFlags1(EepkEffectPartFlags1.RotateUpdate, value);
        }
        public bool I_32_2
        {
            get => effectPart.HasFlag1(EepkEffectPartFlags1.InstantUpdate);
            set => SetFlags1(EepkEffectPartFlags1.InstantUpdate, value);
        }
        public bool I_32_3
        {
            get => effectPart.HasFlag1(EepkEffectPartFlags1.OnGroundOnly);
            set => SetFlags1(EepkEffectPartFlags1.OnGroundOnly, value);
        }
        public bool I_32_4
        {
            get => effectPart.HasFlag1(EepkEffectPartFlags1.UseTimeScale);
            set => SetFlags1(EepkEffectPartFlags1.UseTimeScale, value);
        }
        public bool I_32_5
        {
            get => effectPart.HasFlag1(EepkEffectPartFlags1.UseBoneDirection);
            set => SetFlags1(EepkEffectPartFlags1.UseBoneDirection, value);
        }
        public bool I_32_6
        {
            get => effectPart.HasFlag1(EepkEffectPartFlags1.EnableRotationValues);
            set => SetFlags1(EepkEffectPartFlags1.EnableRotationValues, value);
        }
        public bool I_32_7
        {
            get => effectPart.HasFlag1(EepkEffectPartFlags1.UseScreenCenterToBoneDirection);
            set => SetFlags1(EepkEffectPartFlags1.UseScreenCenterToBoneDirection, value);
        }
        public bool I_36_1
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_36_1);
            set => SetFlags2(EepkEffectPartFlags2.I_36_1, value);
        }
        public bool I_36_2
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_36_2);
            set => SetFlags2(EepkEffectPartFlags2.I_36_2, value);
        }
        public bool I_36_3
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_36_3);
            set => SetFlags2(EepkEffectPartFlags2.I_36_3, value);
        }
        public bool I_36_4
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.LightApplyToCharacter);
            set => SetFlags2(EepkEffectPartFlags2.LightApplyToCharacter, value);
        }
        public bool I_36_5
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.LightApplyToStage);
            set => SetFlags2(EepkEffectPartFlags2.LightApplyToStage, value);
        }
        public bool I_36_6
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.LightUnknown);
            set => SetFlags2(EepkEffectPartFlags2.LightUnknown, value);
        }
        public bool I_36_7
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_36_7);
            set => SetFlags2(EepkEffectPartFlags2.I_36_7, value);
        }
        public bool I_37_0
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_37_0);
            set => SetFlags2(EepkEffectPartFlags2.I_37_0, value);
        }
        public bool I_37_1
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_37_1);
            set => SetFlags2(EepkEffectPartFlags2.I_37_1, value);
        }
        public bool I_37_2
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_37_2);
            set => SetFlags2(EepkEffectPartFlags2.I_37_2, value);
        }
        public bool I_37_3
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_37_3);
            set => SetFlags2(EepkEffectPartFlags2.I_37_3, value);
        }
        public bool I_37_4
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_37_4);
            set => SetFlags2(EepkEffectPartFlags2.I_37_4, value);
        }
        public bool I_37_5
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_37_5);
            set => SetFlags2(EepkEffectPartFlags2.I_37_5, value);
        }
        public bool I_37_6
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_37_6);
            set => SetFlags2(EepkEffectPartFlags2.I_37_6, value);
        }
        public bool I_37_7
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_37_7);
            set => SetFlags2(EepkEffectPartFlags2.I_37_7, value);
        }
        public bool EnableDistanceCulling
        {
            get => !effectPart.HasFlag2(EepkEffectPartFlags2.DisableDistanceCulling);
            set
            {
                SetFlags2(EepkEffectPartFlags2.DisableDistanceCulling, !value);
                NotifyPropertyChanged(nameof(EnableDistanceCulling));
            }
        }
        public bool I_39_1
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_39_1);
            set => SetFlags2(EepkEffectPartFlags2.I_39_1, value);
        }
        public bool I_39_3
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.PositionScaleUnk);
            set => SetFlags2(EepkEffectPartFlags2.PositionScaleUnk, value);
        }
        public bool InheritUsersScaleOnStart
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.InheritUsersScaleOnStart);
            set => SetFlags2(EepkEffectPartFlags2.InheritUsersScaleOnStart, value);
        }
        public bool InheritBoneScaleOnStart
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.InheritNodeScaleOnStart);
            set => SetFlags2(EepkEffectPartFlags2.InheritNodeScaleOnStart, value);
        }
        public bool InheritUsersScaleOnUpdate
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.InheritUsersScaleOnUpdate);
            set => SetFlags2(EepkEffectPartFlags2.InheritUsersScaleOnUpdate, value);
        }
        public bool InheritBoneScaleOnUpdate
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.InheritNodeScaleOnUpdate);
            set => SetFlags2(EepkEffectPartFlags2.InheritNodeScaleOnUpdate, value);
        }
        public bool I_39_6
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.I_39_6);
            set => SetFlags2(EepkEffectPartFlags2.I_39_6, value);
        }
        public bool I_39_7
        {
            get => effectPart.HasFlag2(EepkEffectPartFlags2.ObjectOrientation_To_XXXX);
            set => SetFlags2(EepkEffectPartFlags2.ObjectOrientation_To_XXXX, value);
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
            get => effectPart.HasFlag2(EepkEffectPartFlags2.EMA_Loop);
            set => SetFlags2(EepkEffectPartFlags2.EMA_Loop, value);
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

        public EmoDrawPass EmoDrawPass
        {
            get => GetDrawPass();
            set => SetDrawPass(value);
        }

        public bool IsUsingEma => effectPart?.AssetType == AssetType.EMO || effectPart?.AssetType == AssetType.LIGHT;
        public bool IsEmoOrPbind => effectPart?.AssetType == AssetType.EMO || effectPart?.AssetType == AssetType.PBIND;
        public bool IsNotCBIND => effectPart?.AssetType != AssetType.CBIND;
        public bool IsNotTBIND => effectPart?.AssetType != AssetType.TBIND;
        public bool IsNotLIGHT => effectPart?.AssetType != AssetType.LIGHT;
        public bool IsLIGHT => effectPart?.AssetType == AssetType.LIGHT;
        public bool IsEMO => effectPart?.AssetType == AssetType.EMO;

        public EffectPartViewModel()
        {
            effectPart = defaultModel;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public void ChangeModel(EffectPart effectPart)
        {
            this.effectPart = effectPart ?? defaultModel;
            UpdateProperties();
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
            RaisePropertyChanged(() => DirectionAxis);
            RaisePropertyChanged(() => DepthBias);
            RaisePropertyChanged(() => I_32_0);
            RaisePropertyChanged(() => I_32_1);
            RaisePropertyChanged(() => I_32_2);
            RaisePropertyChanged(() => I_32_3);
            RaisePropertyChanged(() => I_32_4);
            RaisePropertyChanged(() => I_32_5);
            RaisePropertyChanged(() => I_32_6);
            RaisePropertyChanged(() => I_32_7);
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
            RaisePropertyChanged(() => I_39_1);
            RaisePropertyChanged(() => I_39_3);
            RaisePropertyChanged(() => InheritUsersScaleOnStart);
            RaisePropertyChanged(() => InheritBoneScaleOnStart);
            RaisePropertyChanged(() => InheritUsersScaleOnUpdate);
            RaisePropertyChanged(() => InheritBoneScaleOnUpdate);
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
            RaisePropertyChanged(() => IsNotLIGHT);
            RaisePropertyChanged(() => IsLIGHT);
            RaisePropertyChanged(() => IsEMO);
            RaisePropertyChanged(() => IsUsingEma);
            RaisePropertyChanged(() => IsEmoOrPbind);

            RaisePropertyChanged(() => EnableDistanceCulling);
            RaisePropertyChanged(() => EmoDrawPass);
        }

        private void SetFlags1(EepkEffectPartFlags1 flag, bool state)
        {
            byte mask = (byte)flag;
            byte flag1 = (byte)effectPart.Flags1;
            EepkEffectPartFlags1 originalValue = effectPart.Flags1;

            if (state)
            {
                flag1 |= mask;
            }
            else
            {
                flag1 &= (byte)~mask;
            }

            effectPart.Flags1 = (EepkEffectPartFlags1)flag1;

            if(effectPart.Flags1 != originalValue)
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(effectPart.Flags1), effectPart, originalValue, effectPart.Flags1, "EEPK Flags"));
        }

        private void SetFlags2(EepkEffectPartFlags2 flag, bool state)
        {
            uint mask = (uint)flag;
            uint flag2 = (uint)effectPart.Flags2;
            EepkEffectPartFlags2 originalValue = effectPart.Flags2;

            if (state)
            {
                flag2 |= mask;
            }
            else
            {
                flag2 &= ~mask;
            }

            effectPart.Flags2 = (EepkEffectPartFlags2)flag2;

            if (effectPart.Flags2 != originalValue)
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(effectPart.Flags2), effectPart, originalValue, effectPart.Flags2, "EEPK Flags"));
        }
    
        private EmoDrawPass GetDrawPass()
        {
            if (effectPart == null) return 0;

            if (effectPart.HasFlag2(EepkEffectPartFlags2.IsGrassDrawPass))
                return EmoDrawPass.Grass;

            if (effectPart.HasFlag2(EepkEffectPartFlags2.IsDecal))
                return EmoDrawPass.Decal;

            if (effectPart.HasFlag2(EepkEffectPartFlags2.PriorityDrawPassA))
                return EmoDrawPass.PriorityA;

            if (effectPart.HasFlag2(EepkEffectPartFlags2.PriorityDrawPassB))
                return EmoDrawPass.PriorityB;

            return EmoDrawPass.Default;
        }

        private void SetDrawPass(EmoDrawPass drawPass)
        {
            uint flag2 = ((uint)effectPart.Flags2) &~(uint)(EepkEffectPartFlags2.IsGrassDrawPass | EepkEffectPartFlags2.IsDecal | EepkEffectPartFlags2.PriorityDrawPassA | EepkEffectPartFlags2.PriorityDrawPassB);
            EepkEffectPartFlags2 originalValue = effectPart.Flags2;

            switch (drawPass)
            {
                case EmoDrawPass.Grass:
                    flag2 |= (uint)EepkEffectPartFlags2.IsGrassDrawPass;
                    break;
                case EmoDrawPass.Decal:
                    flag2 |= (uint)EepkEffectPartFlags2.IsDecal;
                    break;
                case EmoDrawPass.PriorityA:
                    flag2 |= (uint)EepkEffectPartFlags2.PriorityDrawPassA;
                    break;
                case EmoDrawPass.PriorityB:
                    flag2 |= (uint)EepkEffectPartFlags2.PriorityDrawPassB;
                    break;
            }

            effectPart.Flags2 = (EepkEffectPartFlags2)flag2;

            if (effectPart.Flags2 != originalValue)
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(effectPart.Flags2), effectPart, originalValue, effectPart.Flags2, "EMO Draw Pass"));
        }
    }

    public enum EmoDrawPass
    {
        Default,
        Grass,
        Decal,
        PriorityA,
        PriorityB
    }
}
