using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;

namespace EEPK_Organiser.ViewModel
{
    public class EmpTextureViewModel : ObservableObject
    {
        private EMP_TextureSamplerDef texture;

        private EMP_ScrollKeyframe _selectedKeyframe = null;
        public EMP_ScrollKeyframe SelectedKeyframe
        {
            get
            {
                if (texture.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.Static) return texture.ScrollState.Keyframes[0];
                return _selectedKeyframe;
            }
            set
            {
                _selectedKeyframe = value;
                RaisePropertyChanged(nameof(SelectedKeyframe));
            }
        }

        public EmbEntry SelectedEmbEntry
        {
            get => texture.TextureRef;
            set
            {
                if(value != texture.TextureRef)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.TextureRef), texture, texture.TextureRef, value, "Change Texture"));
                    texture.TextureRef = value;
                    RaisePropertyChanged(nameof(SelectedEmbEntry));
                }
            }
        }
        public byte I_00
        {
            get => texture.I_00;
            set
            {
                if (value != texture.I_00)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.I_00), texture, texture.I_00, value, "EMP Texture -> I_00"));
                    texture.I_00 = value;
                    RaisePropertyChanged(nameof(I_00));
                }
            }
        }
        public byte I_02
        {
            get => texture.I_02;
            set
            {
                if (value != texture.I_02)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.I_02), texture, texture.I_02, value, "EMP Texture -> I_02"));
                    texture.I_02 = value;
                    RaisePropertyChanged(nameof(I_02));
                }
            }
        }
        public byte I_03
        {
            get => texture.I_03;
            set
            {
                if (value != texture.I_03)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.I_03), texture, texture.I_03, value, "EMP Texture -> I_03"));
                    texture.I_03 = value;
                    RaisePropertyChanged(nameof(I_03));
                }
            }
        }
        public EMP_TextureSamplerDef.TextureFiltering FilteringMin
        {
            get => texture.FilteringMin;
            set
            {
                if (value != texture.FilteringMin)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.FilteringMin), texture, texture.FilteringMin, value, "EMP Texture -> Filtering Min"));
                    texture.FilteringMin = value;
                    RaisePropertyChanged(nameof(FilteringMin));
                }
            }
        }
        public EMP_TextureSamplerDef.TextureFiltering FilteringMag
        {
            get => texture.FilteringMag;
            set
            {
                if (value != texture.FilteringMag)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.FilteringMag), texture, texture.FilteringMag, value, "EMP Texture -> Filtering Mag"));
                    texture.FilteringMag = value;
                    RaisePropertyChanged(nameof(FilteringMag));
                }
            }
        }
        public EMP_TextureSamplerDef.TextureRepitition RepetitionU
        {
            get => texture.RepetitionU;
            set
            {
                if (value != texture.RepetitionU)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.RepetitionU), texture, texture.RepetitionU, value, "EMP Texture -> Repetition U"));
                    texture.RepetitionU = value;
                    RaisePropertyChanged(nameof(RepetitionU));
                }
            }
        }
        public EMP_TextureSamplerDef.TextureRepitition RepetitionV
        {
            get => texture.RepetitionV;
            set
            {
                if (value != texture.RepetitionV)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.RepetitionV), texture, texture.RepetitionV, value, "EMP Texture -> Repetition V"));
                    texture.RepetitionV = value;
                    RaisePropertyChanged(nameof(RepetitionV));
                }
            }
        }
        public byte RandomSymetryU
        {
            get => texture.RandomSymetryU;
            set
            {
                if (value != texture.RandomSymetryU)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.RandomSymetryU), texture, texture.RandomSymetryU, value, "EMP Texture -> RandomSymetry U"));
                    texture.RandomSymetryU = value;
                    RaisePropertyChanged(nameof(RandomSymetryU));
                }
            }
        }
        public byte RandomSymetryV
        {
            get => texture.RandomSymetryV;
            set
            {
                if (value != texture.RandomSymetryV)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.RandomSymetryV), texture, texture.RandomSymetryV, value, "EMP Texture -> RandomSymetry V"));
                    texture.RandomSymetryV = value;
                    RaisePropertyChanged(nameof(RandomSymetryV));
                }
            }
        }

        public float ScrollSpeedU
        {
            get => texture.ScrollState.ScrollSpeed_U;
            set
            {
                if (value != texture.ScrollState.ScrollSpeed_U)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.ScrollState.ScrollSpeed_U), texture.ScrollState, texture.ScrollState.ScrollSpeed_U, value, "EMP Texture -> ScrollSpeed U"));
                    texture.ScrollState.ScrollSpeed_U = value;
                    RaisePropertyChanged(nameof(ScrollSpeedU));
                }
            }
        }
        public float ScrollSpeedV
        {
            get => texture.ScrollState.ScrollSpeed_V;
            set
            {
                if (value != texture.ScrollState.ScrollSpeed_V)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.ScrollState.ScrollSpeed_V), texture.ScrollState, texture.ScrollState.ScrollSpeed_V, value, "EMP Texture -> ScrollSpeed V"));
                    texture.ScrollState.ScrollSpeed_V = value;
                    RaisePropertyChanged(nameof(ScrollSpeedV));
                }
            }
        }

        public int KeyframeTime
        {
            get => SelectedKeyframe != null ? SelectedKeyframe.Time : 0;
            set
            {
                if (SelectedKeyframe == null) return;

                if (value != SelectedKeyframe.Time)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedKeyframe.Time), SelectedKeyframe, SelectedKeyframe.Time, value, "EMP Texture -> Time"));
                    SelectedKeyframe.Time = value;
                    RaisePropertyChanged(nameof(KeyframeTime));
                }
            }
        }
        public float ScrollU
        {
            get => SelectedKeyframe != null ? SelectedKeyframe.ScrollU : 0f;
            set
            {
                if (SelectedKeyframe == null) return;

                if (value != SelectedKeyframe.ScrollU)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedKeyframe.ScrollU), SelectedKeyframe, SelectedKeyframe.ScrollU, value, "EMP Texture -> Scroll U"));
                    SelectedKeyframe.ScrollU = value;
                    RaisePropertyChanged(nameof(ScrollU));
                }
            }
        }
        public float ScrollV
        {
            get => SelectedKeyframe != null ? SelectedKeyframe.ScrollV : 0f;
            set
            {
                if (SelectedKeyframe == null) return;

                if (value != SelectedKeyframe.ScrollV)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedKeyframe.ScrollV), SelectedKeyframe, SelectedKeyframe.ScrollV, value, "EMP Texture -> Scroll V"));
                    SelectedKeyframe.ScrollV = value;
                    RaisePropertyChanged(nameof(ScrollV));
                }
            }
        }
        public float ScaleU
        {
            get => SelectedKeyframe != null ? SelectedKeyframe.ScaleU : 0f;
            set
            {
                if (SelectedKeyframe == null) return;

                if (value != SelectedKeyframe.ScaleU)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedKeyframe.ScaleU), SelectedKeyframe, SelectedKeyframe.ScaleU, value, "EMP Texture -> Scale U"));
                    SelectedKeyframe.ScaleU = value;
                    RaisePropertyChanged(nameof(ScaleU));
                }
            }
        }
        public float ScaleV
        {
            get => SelectedKeyframe != null ? SelectedKeyframe.ScaleV : 0f;
            set
            {
                if (SelectedKeyframe == null) return;

                if (value != SelectedKeyframe.ScaleV)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedKeyframe.ScaleV), SelectedKeyframe, SelectedKeyframe.ScaleV, value, "EMP Texture -> Scale V"));
                    SelectedKeyframe.ScaleV = value;
                    RaisePropertyChanged(nameof(ScaleV));
                }
            }
        }
        
        public EMP_ScrollState.ScrollTypeEnum ScrollType
        {
            get => texture.ScrollState.ScrollType;
            set
            {
                if (value != texture.ScrollState.ScrollType)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(texture.ScrollState.ScrollType), texture.ScrollState, texture.ScrollState.ScrollType, value, "EMP Texture -> Scroll Type"));
                    texture.ScrollState.ScrollType = value;
                    RaisePropertyChanged(nameof(ScrollType));
                }
            }
        }


        public void SetContext(EMP_TextureSamplerDef texture)
        {
            this.texture = texture;
        }

        public void UpdateProperties()
        {
            RaisePropertyChanged(nameof(SelectedKeyframe));
            RaisePropertyChanged(nameof(SelectedEmbEntry));
            RaisePropertyChanged(nameof(I_00));
            RaisePropertyChanged(nameof(I_02));
            RaisePropertyChanged(nameof(I_03));
            RaisePropertyChanged(nameof(FilteringMin));
            RaisePropertyChanged(nameof(FilteringMag));
            RaisePropertyChanged(nameof(RepetitionU));
            RaisePropertyChanged(nameof(RepetitionV));
            RaisePropertyChanged(nameof(RandomSymetryU));
            RaisePropertyChanged(nameof(RandomSymetryV));
            RaisePropertyChanged(nameof(ScrollSpeedU));
            RaisePropertyChanged(nameof(ScrollSpeedV));
            RaisePropertyChanged(nameof(ScrollU));
            RaisePropertyChanged(nameof(ScrollV));
            RaisePropertyChanged(nameof(ScaleU));
            RaisePropertyChanged(nameof(ScaleV));
            RaisePropertyChanged(nameof(ScrollType));
            RaisePropertyChanged(nameof(KeyframeTime));

        }

    }
}
