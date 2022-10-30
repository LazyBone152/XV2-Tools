using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight;
using System.Windows;
using LB_Common.Numbers;
using Xv2CoreLib.Resource.UndoRedo;
using System.Windows.Controls;

namespace EEPK_Organiser.ViewModel
{
    public class EmpKeyframesViewModel : ObservableObject
    {
        private KeyframedBaseValue KeyframedValue = null;
        public DataGrid FloatKeyframeDataGrid = null;
        public DataGrid ColorKeyframeDataGrid = null;
        public DataGrid Vector2KeyframeDataGrid = null;
        public DataGrid Vector3KeyframeDataGrid = null;

        #region Visibilities
        public Visibility Vector2Visibile => KeyframedValue is KeyframedVector2Value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility Vector3Visibile => KeyframedValue is KeyframedVector3Value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ColorVisibile => KeyframedValue is KeyframedColorValue ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FloatVisibile => KeyframedValue is KeyframedFloatValue ? Visibility.Visible : Visibility.Collapsed;

        public KeyframedVector2Value Vector2Value => KeyframedValue as KeyframedVector2Value;
        public KeyframedVector3Value Vector3Value => KeyframedValue as KeyframedVector3Value;
        public KeyframedColorValue ColorValue => KeyframedValue as KeyframedColorValue;
        public KeyframedFloatValue FloatValue => KeyframedValue as KeyframedFloatValue;
        #endregion

        //Selected Keyframes
        private KeyframeVector2Value _selectedVector2 = null;
        private KeyframeVector3Value _selectedVector3 = null;
        private KeyframeColorValue _selectedColor = null;
        private KeyframeFloatValue _selectedFloat = null;

        public KeyframeVector2Value SelectedVector2
        {
            get => _selectedVector2;
            set
            {
                if (value != _selectedVector2)
                {
                    _selectedVector2 = value;
                    RaisePropertyChanged(nameof(SelectedVector2));
                }
            }
        }
        public KeyframeVector3Value SelectedVector3
        {
            get => _selectedVector3;
            set
            {
                if (value != _selectedVector3)
                {
                    _selectedVector3 = value;
                    RaisePropertyChanged(nameof(SelectedVector3));
                }
            }
        }
        public KeyframeColorValue SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (value != _selectedColor)
                {
                    _selectedColor = value;
                    RaisePropertyChanged(nameof(SelectedColor));
                }
            }
        }
        public KeyframeFloatValue SelectedFloat
        {
            get => _selectedFloat;
            set
            {
                if (value != _selectedFloat)
                {
                    _selectedFloat = value;
                    RaisePropertyChanged(nameof(SelectedFloat));
                }
            }
        }
       

        //Add Keyframe
        public float NewTime { get; set; }
        public float NewFloat { get; set; }
        public CustomVector4 NewVector { get; set; } = new CustomVector4();
        public CustomColor NewColor { get; set; } = new CustomColor();

        //Options
        public bool Loop
        {
            get => KeyframedValue.Loop;
            set
            {
                if(KeyframedValue.Loop != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(KeyframedValue.Loop), KeyframedValue, KeyframedValue.Loop, value, "Keyframed Value -> Loop"));
                    KeyframedValue.Loop = value;
                }
            }
        }
        public bool Interpolate
        {
            get => KeyframedValue.Interpolate;
            set
            {
                if (KeyframedValue.Interpolate != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(KeyframedValue.Interpolate), KeyframedValue, KeyframedValue.Interpolate, value, "Keyframed Value -> Interpolate"));
                    KeyframedValue.Interpolate = value;
                }
            }
        }
        public bool IsAnimated
        {
            get => KeyframedValue.IsAnimated;
            set
            {
                if (KeyframedValue.IsAnimated != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(KeyframedValue.IsAnimated), KeyframedValue, KeyframedValue.IsAnimated, value, "Keyframed Value -> Is Animated"));
                    KeyframedValue.IsAnimated = value;
                    RaisePropertyChanged(nameof(IsAnimated));
                }
            }
        }

        public void SetContext(KeyframedBaseValue value)
        {
            KeyframedValue = value;

            if(value != null)
                UpdateProperties();
        }

        public void UpdateProperties()
        {
            RaisePropertyChanged(nameof(Vector2Visibile));
            RaisePropertyChanged(nameof(Vector3Visibile));
            RaisePropertyChanged(nameof(ColorVisibile));
            RaisePropertyChanged(nameof(FloatVisibile));
            RaisePropertyChanged(nameof(Vector2Value));
            RaisePropertyChanged(nameof(Vector3Value));
            RaisePropertyChanged(nameof(ColorValue));
            RaisePropertyChanged(nameof(FloatValue));

            RaisePropertyChanged(nameof(Loop));
            RaisePropertyChanged(nameof(Interpolate));
            RaisePropertyChanged(nameof(IsAnimated));
        }


        #region FloatCommands
        public RelayCommand AddFloatKeyframeCommand => new RelayCommand(AddFloatKeyframe, IsFloatValue);
        private void AddFloatKeyframe()
        {
            UndoManager.Instance.AddUndo(FloatValue.AddKeyframe(NewTime, NewFloat));
        }

        public RelayCommand DeleteFloatKeyframeCommand => new RelayCommand(DeleteFloatKeyframe, IsFloatSelected);
        private void DeleteFloatKeyframe()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<KeyframeFloatValue> selectedFloatKeyframes = FloatKeyframeDataGrid.SelectedItems.Cast<KeyframeFloatValue>().ToList();

            foreach (var keyframe in selectedFloatKeyframes)
            {
                undos.Add(FloatValue.RemoveKeyframe(keyframe));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "EMP -> Remove Keyframes");
        }

        public RelayCommand CopyFloatKeyframeCommand => new RelayCommand(CopyFloatKeyframe, IsFloatSelected);
        private void CopyFloatKeyframe()
        {
            List<KeyframeFloatValue> selectedFloatKeyframes = FloatKeyframeDataGrid.SelectedItems.Cast<KeyframeFloatValue>().ToList();

            if(selectedFloatKeyframes.Count > 0)
            {
                Clipboard.SetData(KeyframedFloatValue.CLIPBOARD_ID, selectedFloatKeyframes);
            }
        }

        public RelayCommand PasteFloatKeyframeCommand => new RelayCommand(PasteFloatKeyframe, () => Clipboard.ContainsData(KeyframedFloatValue.CLIPBOARD_ID) && IsFloatValue());
        private void PasteFloatKeyframe()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<KeyframeFloatValue> keyframes = (List<KeyframeFloatValue>)Clipboard.GetData(KeyframedFloatValue.CLIPBOARD_ID);

            foreach(var keyframe in keyframes)
            {
                undos.Add(FloatValue.AddKeyframe(keyframe.Time, keyframe.Value));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "EMP -> Paste Keyframe");
        }


        private bool IsFloatValue()
        {
            return KeyframedValue is KeyframedFloatValue;
        }

        private bool IsFloatSelected()
        {
            return SelectedFloat != null;
        }

        #endregion

        #region ColorCommands
        public RelayCommand AddColorKeyframeCommand => new RelayCommand(AddColorKeyframe, IsColorValue);
        private void AddColorKeyframe()
        {
            UndoManager.Instance.AddUndo(ColorValue.AddKeyframe(NewTime, NewColor.R, NewColor.G, NewColor.B));
        }

        public RelayCommand DeleteColorKeyframeCommand => new RelayCommand(DeleteColorKeyframe, IsColorSelected);
        private void DeleteColorKeyframe()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<KeyframeColorValue> selectedKeyframes = ColorKeyframeDataGrid.SelectedItems.Cast<KeyframeColorValue>().ToList();

            foreach (var keyframe in selectedKeyframes)
            {
                undos.Add(ColorValue.RemoveKeyframe(keyframe));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Remove Keyframes");
        }

        public RelayCommand CopyColorKeyframeCommand => new RelayCommand(CopyColorKeyframe, IsColorSelected);
        private void CopyColorKeyframe()
        {
            List<KeyframeColorValue> selectedKeyframes = ColorKeyframeDataGrid.SelectedItems.Cast<KeyframeColorValue>().ToList();

            if (selectedKeyframes.Count > 0)
            {
                Clipboard.SetData(KeyframedColorValue.CLIPBOARD_ID, selectedKeyframes);
            }
        }

        public RelayCommand PasteColorKeyframeCommand => new RelayCommand(PasteColorKeyframe, () => Clipboard.ContainsData(KeyframedColorValue.CLIPBOARD_ID) && IsColorValue());
        private void PasteColorKeyframe()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<KeyframeColorValue> keyframes = (List<KeyframeColorValue>)Clipboard.GetData(KeyframedColorValue.CLIPBOARD_ID);

            foreach (var keyframe in keyframes)
            {
                undos.Add(ColorValue.AddKeyframe(keyframe.Time, keyframe.Value.R, keyframe.Value.G, keyframe.Value.B));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "EMP -> Paste Keyframe");
        }

        private bool IsColorValue()
        {
            return KeyframedValue is KeyframedColorValue;
        }

        private bool IsColorSelected()
        {
            return SelectedColor != null;
        }
        #endregion

        #region Vector2Commands
        public RelayCommand AddVector2KeyframeCommand => new RelayCommand(AddVector2Keyframe, IsVector2Value);
        private void AddVector2Keyframe()
        {
            UndoManager.Instance.AddUndo(Vector2Value.AddKeyframe(NewTime, NewVector.X, NewVector.Y));
        }

        public RelayCommand DeleteVector2KeyframeCommand => new RelayCommand(DeleteVector2Keyframe, IsVector2Selected);
        private void DeleteVector2Keyframe()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<KeyframeVector2Value> selectedKeyframes = Vector2KeyframeDataGrid.SelectedItems.Cast<KeyframeVector2Value>().ToList();

            foreach (var keyframe in selectedKeyframes)
            {
                undos.Add(Vector2Value.RemoveKeyframe(keyframe));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Remove Keyframes");
        }

        public RelayCommand CopyVector2KeyframeCommand => new RelayCommand(CopyVector2Keyframe, IsVector2Selected);
        private void CopyVector2Keyframe()
        {
            List<KeyframeVector2Value> selectedKeyframes = Vector2KeyframeDataGrid.SelectedItems.Cast<KeyframeVector2Value>().ToList();

            if (selectedKeyframes.Count > 0)
            {
                Clipboard.SetData(KeyframedVector2Value.CLIPBOARD_ID, selectedKeyframes);
            }
        }

        public RelayCommand PasteVector2KeyframeCommand => new RelayCommand(PasteVector2Keyframe, () => Clipboard.ContainsData(KeyframedVector2Value.CLIPBOARD_ID) && IsVector2Value());
        private void PasteVector2Keyframe()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<KeyframeVector2Value> keyframes = (List<KeyframeVector2Value>)Clipboard.GetData(KeyframedVector2Value.CLIPBOARD_ID);

            foreach (var keyframe in keyframes)
            {
                undos.Add(Vector2Value.AddKeyframe(keyframe.Time, keyframe.Value.X, keyframe.Value.Y));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "EMP -> Paste Keyframe");
        }

        private bool IsVector2Value()
        {
            return KeyframedValue is KeyframedVector2Value;
        }

        private bool IsVector2Selected()
        {
            return SelectedVector2 != null;
        }
        #endregion

        #region Vector3Commands
        public RelayCommand AddVector3KeyframeCommand => new RelayCommand(AddVector3Keyframe, IsVector3Value);
        private void AddVector3Keyframe()
        {
            UndoManager.Instance.AddUndo(Vector3Value.AddKeyframe(NewTime, NewVector.X, NewVector.Y, NewVector.Z));
        }

        public RelayCommand DeleteVector3KeyframeCommand => new RelayCommand(DeleteVector3Keyframe, IsVector3Selected);
        private void DeleteVector3Keyframe()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<KeyframeVector3Value> selectedKeyframes = Vector3KeyframeDataGrid.SelectedItems.Cast<KeyframeVector3Value>().ToList();

            foreach (var keyframe in selectedKeyframes)
            {
                undos.Add(Vector3Value.RemoveKeyframe(keyframe));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Remove Keyframes");
        }

        public RelayCommand CopyVector3KeyframeCommand => new RelayCommand(CopyVector3Keyframe, IsVector3Selected);
        private void CopyVector3Keyframe()
        {
            List<KeyframeVector3Value> selectedKeyframes = Vector3KeyframeDataGrid.SelectedItems.Cast<KeyframeVector3Value>().ToList();

            if (selectedKeyframes.Count > 0)
            {
                Clipboard.SetData(KeyframedVector3Value.CLIPBOARD_ID, selectedKeyframes);
            }
        }

        public RelayCommand PasteVector3KeyframeCommand => new RelayCommand(PasteVector3Keyframe, () => Clipboard.ContainsData(KeyframedVector3Value.CLIPBOARD_ID) && IsVector3Value());
        private void PasteVector3Keyframe()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<KeyframeVector3Value> keyframes = (List<KeyframeVector3Value>)Clipboard.GetData(KeyframedVector3Value.CLIPBOARD_ID);

            foreach (var keyframe in keyframes)
            {
                undos.Add(Vector3Value.AddKeyframe(keyframe.Time, keyframe.Value.X, keyframe.Value.Y, NewVector.Z));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "EMP -> Paste Keyframe");
        }

        private bool IsVector3Value()
        {
            return KeyframedValue is KeyframedVector3Value;
        }

        private bool IsVector3Selected()
        {
            return SelectedVector3 != null;
        }
        #endregion

    }
}
