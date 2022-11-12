using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Vectors
{
    public partial class CustomRgbColor : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DP
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(LB_Common.Numbers.CustomColor), typeof(CustomRgbColor), new PropertyMetadata(ValueChangedCallback));

        public LB_Common.Numbers.CustomColor Value
        {
            get => (LB_Common.Numbers.CustomColor)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
            nameof(Interval), typeof(float), typeof(CustomRgbColor), new PropertyMetadata(0.05f));

        public float Interval
        {
            get => (float)GetValue(IntervalProperty);
            set => SetValue(IntervalProperty, value);
        }

        //Hide UpDownButtons
        public static readonly DependencyProperty HideUpDownButtonsProperty = DependencyProperty.Register(
            nameof(HideUpDownButtons), typeof(bool), typeof(CustomRgbColor), new PropertyMetadata(false));

        public bool HideUpDownButtons
        {
            get => (bool)GetValue(HideUpDownButtonsProperty);
            set => SetValue(HideUpDownButtonsProperty, value);
        }

        //Allow Undo
        public static readonly DependencyProperty AllowUndoProperty = DependencyProperty.Register(
            nameof(AllowUndo), typeof(bool), typeof(CustomRgbColor), new PropertyMetadata(true));

        public bool AllowUndo
        {
            get => (bool)GetValue(AllowUndoProperty);
            set => SetValue(AllowUndoProperty, value);
        }

        //TextAlignment
        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
            nameof(TextAlignment), typeof(TextAlignment), typeof(CustomRgbColor), new PropertyMetadata(TextAlignment.Right));

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        //IsReadOnly
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(CustomRgbColor), new PropertyMetadata(false));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        private static void ValueChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(sender is CustomRgbColor view)
            {
                view.UpdateProperties();
                view.UpdateEvents(e.NewValue, e.OldValue);
            }
        }


        #endregion

        public event EventHandler ColorChangedEvent;

        public float R
        {
            get => Value != null ? Value.R : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.R));
                NotifyPropertyChanged(nameof(Preview));
                NotifyPropertyChanged(nameof(CurrentColor));
            }
        }
        public float G
        {
            get => Value != null ? Value.G : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.G));
                NotifyPropertyChanged(nameof(Preview));
                NotifyPropertyChanged(nameof(CurrentColor));
            }
        }
        public float B
        {
            get => Value != null ? Value.B : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.B));
                NotifyPropertyChanged(nameof(Preview));
                NotifyPropertyChanged(nameof(CurrentColor));
            }
        }

        public string R_Preview => $"{R.ToString("0.0###")}";
        public string G_Preview => $"{G.ToString("0.0###")}";
        public string B_Preview => $"{B.ToString("0.0###")}";

        public Color? CurrentColor
        {
            get => Color.FromScRgb(1f, NormalizedFloatColor(R), NormalizedFloatColor(G), NormalizedFloatColor(B));
            set => SetColorValue(value);
        }

        public Brush Preview => CalculateColorPreview();

        public Visibility EditableVisibility => !IsReadOnly ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ReadOnlyVisibility => IsReadOnly ? Visibility.Visible : Visibility.Collapsed;

        public CustomRgbColor()
        {
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Loaded += CustomRgbColor_Loaded;
            Unloaded += CustomRgbColor_Unloaded;
        }

        private void CustomRgbColor_Unloaded(object sender, RoutedEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;

            if(Value != null)
                Value.PropertyChanged -= Color_PropertyChanged;
        }

        private void CustomRgbColor_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(EditableVisibility));
            NotifyPropertyChanged(nameof(ReadOnlyVisibility));
        }

        private void Instance_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            UpdateProperties();
        }

        private IUndoRedo SetFloatValue(float newValue, string propName, bool addUndo = true)
        {
            float original = (float)Value.GetType().GetProperty(propName).GetValue(Value);

            if (original != newValue)
            {
                Value.GetType().GetProperty(propName).SetValue(Value, newValue);

                if (addUndo && AllowUndo)
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propName, Value, original, newValue, $"{propName}"));

                ColorChangedEvent?.Invoke(this, EventArgs.Empty);
            }
            return (!addUndo) ? new UndoablePropertyGeneric(propName, Value, original, newValue, $"{propName}") : null;
        }

        private void SetColorValue(Color? newValue)
        {
            if (newValue.Value == null || Value == null) return;

            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(SetFloatValue(newValue.Value.ScR, "R", false));
            undos.Add(SetFloatValue(newValue.Value.ScG, "G", false));
            undos.Add(SetFloatValue(newValue.Value.ScB, "B", false));

            if(AllowUndo)
                UndoManager.Instance.AddCompositeUndo(undos, "Color");

            UpdateProperties();
        }

        private void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(R));
            NotifyPropertyChanged(nameof(G));
            NotifyPropertyChanged(nameof(B));
            NotifyPropertyChanged(nameof(R_Preview));
            NotifyPropertyChanged(nameof(G_Preview));
            NotifyPropertyChanged(nameof(B_Preview));
            NotifyPropertyChanged(nameof(CurrentColor));
            NotifyPropertyChanged(nameof(Preview));
        }

        private Brush CalculateColorPreview()
        {
            float r = NormalizedFloatColor(R);
            float g = NormalizedFloatColor(G);
            float b = NormalizedFloatColor(B);

            return new SolidColorBrush(Color.FromArgb(255, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255)));
        }

        private float NormalizedFloatColor(float value)
        {
            int asInt = (int)value;
            if (asInt == value) return value;
            return value - asInt;
        }

        private void UpdateEvents(object newValue, object oldValue)
        {
            if (oldValue is LB_Common.Numbers.CustomColor oldColor)
            {
                oldColor.PropertyChanged -= Color_PropertyChanged;
            }

            if (newValue is LB_Common.Numbers.CustomColor newColor)
            {
                newColor.PropertyChanged += Color_PropertyChanged;
            }
        }

        private void Color_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateProperties();
        }
    }
}