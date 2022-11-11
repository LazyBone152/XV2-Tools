using Xv2CoreLib.ECF;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;
using static Xv2CoreLib.ECF.ECF_Node;

namespace EEPK_Organiser.ViewModel
{
    public class EcfNodeViewModel : ObservableObject
    {
        private ECF_Node node;

        public string Material
        {
            get => node.Material;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.Material), node, node.Material, value, "ECF -> Material"));
                node.Material = value;
                RaisePropertyChanged(nameof(Material));
            }
        }
        public ushort StartTime
        {
            get => node.StartTime;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.StartTime), node, node.StartTime, value, "ECF -> Start Time"));
                node.StartTime = value;
                RaisePropertyChanged(nameof(StartTime));
            }
        }
        public ushort EndTime
        {
            get => node.EndTime;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EndTime), node, node.EndTime, value, "ECF -> End Time"));
                node.EndTime = value;
                RaisePropertyChanged(nameof(EndTime));
            }
        }
        public PlayMode LoopMode
        {
            get => node.LoopMode;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.LoopMode), node, node.LoopMode, value, "ECF -> Loop Mode"));
                node.LoopMode = value;
                RaisePropertyChanged(nameof(LoopMode));
            }
        }
        public KeyframedColorValue MultiColor => node.MultiColor;
        public KeyframedColorValue RimColor => node.RimColor;
        public KeyframedColorValue AmbientColor => node.AddColor;
        public KeyframedFloatValue DiffuseTransparency => node.MultiColor_Transparency;
        public KeyframedFloatValue SpecularTransparency => node.RimColor_Transparency;
        public KeyframedFloatValue AmbientTransparency => node.AddColor_Transparency;
        public KeyframedFloatValue BlendingFactor => node.BlendingFactor;


        public void SetContext(ECF_Node node)
        {
            this.node = node;
            UpdateProperties();
        }

        public void UpdateProperties()
        {
            if (node == null) return;
            RaisePropertyChanged(nameof(Material));
            RaisePropertyChanged(nameof(StartTime));
            RaisePropertyChanged(nameof(EndTime));
            RaisePropertyChanged(nameof(LoopMode));

            RaisePropertyChanged(nameof(MultiColor));
            RaisePropertyChanged(nameof(RimColor));
            RaisePropertyChanged(nameof(AmbientColor));
            RaisePropertyChanged(nameof(DiffuseTransparency));
            RaisePropertyChanged(nameof(SpecularTransparency));
            RaisePropertyChanged(nameof(AmbientTransparency));
            RaisePropertyChanged(nameof(BlendingFactor));
        }
    }
}
