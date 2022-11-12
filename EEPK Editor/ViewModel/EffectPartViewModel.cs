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

        public bool IsNotTBIND { get { return effectPart.I_02 != AssetType.TBIND; } }

        public ushort StartTime
        {
            get
            {
                return effectPart.I_28;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_28), effectPart, effectPart.I_28, value, "Start Time"));
                effectPart.I_28 = value;
            }
        }
        public Attachment Attachment
        {
            get
            {
                return effectPart.I_03;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_03), effectPart, effectPart.I_03, value, "Attachment"));
                effectPart.I_03 = value;
            }
        }
        public byte RotateOnMovement
        {
            get
            {
                return effectPart.I_04;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_04), effectPart, effectPart.I_04, value, "RotateOnMovement"));
                effectPart.I_04 = value;
            }
        }
        public DeactivationMode Deactivation
        {
            get
            {
                return effectPart.Deactivation;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.Deactivation), effectPart, effectPart.Deactivation, value, "Deactivation"));
                effectPart.Deactivation = value;
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
                return effectPart.F_24;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.F_24), effectPart, effectPart.F_24, value, "AvoidSphere"));
                effectPart.F_24 = value;
            }
        }
        public bool I_32_0
        {
            get
            {
                return effectPart.I_32_0;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_32_0), effectPart, effectPart.I_32_0, value, "MoveWithBone"));
                effectPart.I_32_0 = value;
            }
        }
        public bool I_32_1
        {
            get
            {
                return effectPart.I_32_1;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_32_1), effectPart, effectPart.I_32_1, value, "RotateWithBone"));
                effectPart.I_32_1 = value;
            }
        }
        public bool I_32_2
        {
            get
            {
                return effectPart.I_32_2;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_32_2), effectPart, effectPart.I_32_2, value, "InstantMoveAndRotate"));
                effectPart.I_32_2 = value;
            }
        }
        public bool I_32_3
        {
            get
            {
                return effectPart.I_32_3;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_32_3), effectPart, effectPart.I_32_3, value, "OnGroundOnly"));
                effectPart.I_32_3 = value;
            }
        }
        public bool I_32_4
        {
            get
            {
                return effectPart.I_32_4;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_32_4), effectPart, effectPart.I_32_4, value, "UseTimeScale"));
                effectPart.I_32_4 = value;
            }
        }
        public bool I_32_5
        {
            get
            {
                return effectPart.I_32_5;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_32_5), effectPart, effectPart.I_32_5, value, "UseBoneDirection"));
                effectPart.I_32_5 = value;
            }
        }
        public bool I_32_6
        {
            get
            {
                return effectPart.I_32_6;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_32_6), effectPart, effectPart.I_32_6, value, "UseBoneToCameraDirection"));
                effectPart.I_32_6 = value;
            }
        }
        public bool I_32_7
        {
            get
            {
                return effectPart.I_32_7;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_32_7), effectPart, effectPart.I_32_7, value, "UseSceneCenterToBoneDirection"));
                effectPart.I_32_7 = value;
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
        public string I_38_a
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
        public string I_38_b
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
                return effectPart.I_39_0;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_39_0), effectPart, effectPart.I_39_0, value, "NoGlare"));
                effectPart.I_39_0 = value;
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
                return effectPart.I_39_2;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_39_2), effectPart, effectPart.I_39_2, value, "InverseTransparentDrawOrder"));
                effectPart.I_39_2 = value;
            }
        }
        public bool I_39_3
        {
            get
            {
                return effectPart.I_39_3;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_39_3), effectPart, effectPart.I_39_3, value, "ScaleZ_To_BonePositionZ"));
                effectPart.I_39_3 = value;
            }
        }
        public bool I_39_4
        {
            get
            {
                return effectPart.I_39_4;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_39_4), effectPart, effectPart.I_39_4, value, "ScaleZ_To_BonePositionZ"));
                effectPart.I_39_4 = value;
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
                return effectPart.I_39_7;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_39_7), effectPart, effectPart.I_39_7, value, "ObjectOrientation_To_XXXX"));
                effectPart.I_39_7 = value;
            }
        }
        public float POSITION_X
        {
            get
            {
                return effectPart.POSITION_X;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.POSITION_X), effectPart, effectPart.POSITION_X, value, "Position X"));
                effectPart.POSITION_X = value;
            }
        }
        public float POSITION_Y
        {
            get
            {
                return effectPart.POSITION_Y;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.POSITION_Y), effectPart, effectPart.POSITION_Y, value, "Position Y"));
                effectPart.POSITION_Y = value;
            }
        }
        public float POSITION_Z
        {
            get
            {
                return effectPart.POSITION_Z;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.POSITION_Z), effectPart, effectPart.POSITION_Z, value, "Position Z"));
                effectPart.POSITION_Z = value;
            }
        }
        public float F_52
        {
            get
            {
                return effectPart.F_52;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.F_52), effectPart, effectPart.F_52, value, "Rotation X (Min)"));
                effectPart.F_52 = value;
            }
        }
        public float F_56
        {
            get
            {
                return effectPart.F_56;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.F_56), effectPart, effectPart.F_56, value, "Rotation X (Max)"));
                effectPart.F_56 = value;
            }
        }
        public float F_60
        {
            get
            {
                return effectPart.F_60;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.F_60), effectPart, effectPart.F_60, value, "Rotation Y (Min)"));
                effectPart.F_60 = value;
            }
        }
        public float F_64
        {
            get
            {
                return effectPart.F_64;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.F_64), effectPart, effectPart.F_64, value, "Rotation Y (Max)"));
                effectPart.F_64 = value;
            }
        }
        public float F_68
        {
            get
            {
                return effectPart.F_68;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.F_68), effectPart, effectPart.F_68, value, "Rotation Z (Min)"));
                effectPart.F_68 = value;
            }
        }
        public float F_72
        {
            get
            {
                return effectPart.F_72;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.F_72), effectPart, effectPart.F_72, value, "Rotation Z (Max)"));
                effectPart.F_72 = value;
            }
        }
        public float SIZE_1
        {
            get
            {
                return effectPart.SIZE_1;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.SIZE_1), effectPart, effectPart.SIZE_1, value, "Scale (Min)"));
                effectPart.SIZE_1 = value;
            }
        }
        public float SIZE_2
        {
            get
            {
                return effectPart.SIZE_2;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.SIZE_2), effectPart, effectPart.SIZE_2, value, "Scale (Max)"));
                effectPart.SIZE_2 = value;
            }
        }
        public float F_84
        {
            get
            {
                return effectPart.F_84;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.F_84), effectPart, effectPart.F_84, value, "Near Fade Distance"));
                effectPart.F_84 = value;
            }
        }
        public float F_88
        {
            get
            {
                return effectPart.F_88;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.F_88), effectPart, effectPart.F_88, value, "Far Fade Distance"));
                effectPart.F_88 = value;
            }
        }
        public ushort I_30
        {
            get
            {
                return effectPart.I_30;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_30), effectPart, effectPart.I_30, value, "EMA Animation Index"));
                effectPart.I_30 = value;
            }
        }
        public ushort I_92
        {
            get
            {
                return effectPart.I_92;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_92), effectPart, effectPart.I_92, value, "Loop Start Frame"));
                effectPart.I_92 = value;
            }
        }
        public ushort I_94
        {
            get
            {
                return effectPart.I_94;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_94), effectPart, effectPart.I_94, value, "Loop End Frame"));
                effectPart.I_94 = value;
            }
        }
        public bool I_36_0
        {
            get
            {
                return effectPart.I_36_0;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EffectPart>(nameof(effectPart.I_36_0), effectPart, effectPart.I_36_0, value, "EMA Loop"));
                effectPart.I_36_0 = value;
            }
        }
        public string ESK
        {
            get
            {
                if (!EffectPart.CommonBones.Contains(effectPart.ESK))
                    EffectPart.CommonBones.Add(effectPart.ESK);

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
            RaisePropertyChanged(() => RotateOnMovement);
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
        }

    }
}
