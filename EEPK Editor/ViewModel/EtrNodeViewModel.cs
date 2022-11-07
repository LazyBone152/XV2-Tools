using GalaSoft.MvvmLight;
using LB_Common.Numbers;
using Xv2CoreLib;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.ETR;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.ViewModel
{
    public class EtrNodeViewModel : ObservableObject
    {
        private ETR_Node node;

        public string AttachBone
        {
            get => node.AttachBone;
            set
            {
                if (value?.Length <= 32)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.AttachBone), node, node.AttachBone, value, "ETR -> Attach Bone"));
                    node.AttachBone = value;
                }
                RaisePropertyChanged(nameof(AttachBone));
            }
        }
        public string AttachBone2
        {
            get => node.AttachBone2;
            set
            {
                if(value?.Length <= 32)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.AttachBone2), node, node.AttachBone2, value, "ETR -> Attach Bone 2"));
                    node.AttachBone2 = value;
                }
                RaisePropertyChanged(nameof(AttachBone2));
            }
        }
        public CustomVector4 Position => node.Position;
        public CustomVector4 Rotation => node.Rotation;
        public ushort StartTime
        {
            get => node.StartTime;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.StartTime), node, node.StartTime, value, "ETR -> Start Time"));
                node.StartTime = value;
                RaisePropertyChanged(nameof(StartTime));
            }
        }
        public byte SegementFrameStep
        {
            get => node.SegementFrameSize;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.SegementFrameSize), node, node.SegementFrameSize, value, "ETR -> Segement Frame Step"));
                node.SegementFrameSize = value;
                RaisePropertyChanged(nameof(SegementFrameStep));
            }
        }
        public short ExtrudeDuration
        {
            get => node.ExtrudeDuration;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.ExtrudeDuration), node, node.ExtrudeDuration, value, "ETR -> Extrude Duration"));
                node.ExtrudeDuration = value;
                RaisePropertyChanged(nameof(ExtrudeDuration));
            }
        }
        public ushort HoldDuration
        {
            get => node.HoldDuration;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.HoldDuration), node, node.HoldDuration, value, "ETR -> Hold Duration"));
                node.HoldDuration = value;
                RaisePropertyChanged(nameof(HoldDuration));
            }
        }
        public float PositionExtrudeZ
        {
            get => node.PositionExtrudeZ;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.PositionExtrudeZ), node, node.PositionExtrudeZ, value, "ETR -> Position Extrude Z"));
                node.PositionExtrudeZ = value;
                RaisePropertyChanged(nameof(PositionExtrudeZ));
            }
        }
        public byte I_92
        {
            get => node.I_92;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.I_92), node, node.I_92, value, "ETR -> I_92"));
                node.I_92 = value;
                RaisePropertyChanged(nameof(I_92));
            }
        }
        public byte I_88
        {
            get => node.I_88;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.I_88), node, node.I_88, value, "ETR -> I_88"));
                node.I_88 = value;
                RaisePropertyChanged(nameof(I_88));
            }
        }
        public int I_100
        {
            get => node.I_100;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.I_100), node, node.I_100, value, "ETR -> I_100"));
                node.I_100 = value;
                RaisePropertyChanged(nameof(I_100));
            }
        }
        
        //Keyframed values
        public KeyframedFloatValue Scale => node.Scale;
        public KeyframedColorValue Color1 => node.Color1;
        public KeyframedColorValue Color2 => node.Color2;
        public KeyframedFloatValue Color1_Alpha => node.Color1_Transparency;
        public KeyframedFloatValue Color2_Alpha => node.Color2_Transparency;

        //Flags
        public bool Flag_AutoOrientation
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.AutoOrientation);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.AutoOrientation, value);
                RaisePropertyChanged(() => Flag_AutoOrientation);
            }
        }
        public bool Flag_Unk2
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk2);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk2, value);
                RaisePropertyChanged(() => Flag_Unk2);
            }
        }
        public bool Flag_UseColor2
        {
            get => !node.Flags.HasFlag(ETR_Node.ExtrudeFlags.NoDegrade);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.NoDegrade, !value);
                RaisePropertyChanged(() => Flag_UseColor2);
            }
        }
        public bool Flag_DoublePointOfMiddleSection
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.DoublePointOfMiddleSection);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.DoublePointOfMiddleSection, value);
                RaisePropertyChanged(() => Flag_DoublePointOfMiddleSection);
            }
        }
        public bool Flag_Unk5
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk5);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk5, value);
                RaisePropertyChanged(() => Flag_Unk5);
            }
        }
        public bool Flag_Unk6
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk6);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk6, value);
                RaisePropertyChanged(() => Flag_Unk6);
            }
        }
        public bool Flag_Unk7
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk7);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk7, value);
                RaisePropertyChanged(() => Flag_Unk7);
            }
        }
        public bool Flag_Unk8
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk8);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk8, value);
                RaisePropertyChanged(() => Flag_Unk8);
            }
        }
        public bool Flag_DisplayMiddleSection
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.DisplayMiddleSection);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.DisplayMiddleSection, value);
                RaisePropertyChanged(() => Flag_DisplayMiddleSection);
            }
        }
        public bool Flag_UVPauseOnExtrude
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.UVPauseOnExtrude);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.UVPauseOnExtrude, value);
                RaisePropertyChanged(() => Flag_UVPauseOnExtrude);
            }
        }
        public bool Flag_Unk11
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk11);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk11, value);
                RaisePropertyChanged(() => Flag_Unk11);
            }
        }
        public bool Flag_Unk12
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk12);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk12, value);
                RaisePropertyChanged(() => Flag_Unk12);
            }
        }
        public bool Flag_Unk13
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk13);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk13, value);
                RaisePropertyChanged(() => Flag_Unk13);
            }
        }
        public bool Flag_Unk14
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk14);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk14, value);
                RaisePropertyChanged(() => Flag_Unk14);
            }
        }
        public bool Flag_Unk15
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk15);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk15, value);
                RaisePropertyChanged(() => Flag_Unk15);
            }
        }
        public bool Flag_UVPauseOnHold
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.UVPauseOnHold);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.UVPauseOnHold, value);
                RaisePropertyChanged(() => Flag_UVPauseOnHold);
            }
        }
        public bool Flag_Unk17
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk17);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk17, value);
                RaisePropertyChanged(() => Flag_Unk17);
            }
        }
        public bool Flag_Unk18
        {
            get => node.Flags.HasFlag(ETR_Node.ExtrudeFlags.Unk18);
            set
            {
                SetNodeFlags(ETR_Node.ExtrudeFlags.Unk18, value);
                RaisePropertyChanged(() => Flag_Unk18);
            }
        }

        public EmmMaterial MaterialRef
        {
            get => node.MaterialRef;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.MaterialRef), node, node.MaterialRef, value, "ETR -> Material"));
                node.MaterialRef = value;
                RaisePropertyChanged(nameof(MaterialRef));
            }
        }


        public void SetContext(ETR_Node node)
        {
            this.node = node;
            UpdateProperties();
        }

        public void UpdateProperties()
        {
            if (node == null) return;
            RaisePropertyChanged(nameof(AttachBone));
            RaisePropertyChanged(nameof(AttachBone2));
            RaisePropertyChanged(nameof(Position));
            RaisePropertyChanged(nameof(Rotation));
            RaisePropertyChanged(nameof(StartTime));
            RaisePropertyChanged(nameof(SegementFrameStep));
            RaisePropertyChanged(nameof(ExtrudeDuration));
            RaisePropertyChanged(nameof(HoldDuration));
            RaisePropertyChanged(nameof(PositionExtrudeZ));
            RaisePropertyChanged(nameof(I_92));
            RaisePropertyChanged(nameof(I_88));
            RaisePropertyChanged(nameof(I_100));
            RaisePropertyChanged(nameof(Scale));
            RaisePropertyChanged(nameof(Color1));
            RaisePropertyChanged(nameof(Color2));
            RaisePropertyChanged(nameof(Color1_Alpha));
            RaisePropertyChanged(nameof(Color2_Alpha));
            RaisePropertyChanged(nameof(MaterialRef));

            RaisePropertyChanged(nameof(Flag_AutoOrientation));
            RaisePropertyChanged(nameof(Flag_Unk2));
            RaisePropertyChanged(nameof(Flag_UseColor2));
            RaisePropertyChanged(nameof(Flag_DoublePointOfMiddleSection));
            RaisePropertyChanged(nameof(Flag_Unk5));
            RaisePropertyChanged(nameof(Flag_Unk6));
            RaisePropertyChanged(nameof(Flag_Unk7));
            RaisePropertyChanged(nameof(Flag_Unk8));
            RaisePropertyChanged(nameof(Flag_DisplayMiddleSection));
            RaisePropertyChanged(nameof(Flag_UVPauseOnExtrude));
            RaisePropertyChanged(nameof(Flag_Unk11));
            RaisePropertyChanged(nameof(Flag_Unk12));
            RaisePropertyChanged(nameof(Flag_Unk13));
            RaisePropertyChanged(nameof(Flag_Unk14));
            RaisePropertyChanged(nameof(Flag_Unk15));
            RaisePropertyChanged(nameof(Flag_UVPauseOnHold));
            RaisePropertyChanged(nameof(Flag_Unk17));
            RaisePropertyChanged(nameof(Flag_Unk18));
        }

        private void SetNodeFlags(ETR_Node.ExtrudeFlags flag, bool state)
        {
            ETR_Node.ExtrudeFlags newFlag = node.Flags.SetFlag(flag, state);

            if (node.Flags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<ETR_Node>(nameof(ETR_Node.Flags), node, node.Flags, newFlag, "ETR -> Flags"));
                node.Flags = newFlag;
            }
        }

    }
}
