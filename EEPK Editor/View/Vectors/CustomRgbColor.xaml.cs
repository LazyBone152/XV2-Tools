using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Vectors
{
    public partial class CustomRgbColor : UserControl, INotifyPropertyChanged, IDisposable
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
            "Value", typeof(LB_Common.Numbers.CustomColor), typeof(CustomRgbColor), new PropertyMetadata(OnDpChanged));

        public LB_Common.Numbers.CustomColor Value
        {
            get { return (LB_Common.Numbers.CustomColor)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
            "Interval", typeof(float), typeof(CustomRgbColor), new PropertyMetadata(0.05f));

        public float Interval
        {
            get { return (float)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }


        private static DependencyPropertyChangedEventHandler DpChanged;

        private static void OnDpChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (DpChanged != null)
                DpChanged.Invoke(sender, e);
        }

        private void ValueInstanceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateProperties();
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

        public Color? CurrentColor
        {
            get => Color.FromScRgb(1f, NormalizedFloatColor(R), NormalizedFloatColor(G), NormalizedFloatColor(B));
            set => SetColorValue(value);
        }

        public Brush Preview => CalculateColorPreview();

        public CustomRgbColor()
        {
            InitializeComponent();
            DpChanged += ValueInstanceChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            UpdateProperties();
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private IUndoRedo SetFloatValue(float newValue, string propName, bool addUndo = true)
        {
            float original = (float)Value.GetType().GetProperty(propName).GetValue(Value);

            if (original != newValue)
            {
                Value.GetType().GetProperty(propName).SetValue(Value, newValue);

                if (addUndo)
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

            UndoManager.Instance.AddCompositeUndo(undos, "Color");
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(R));
            NotifyPropertyChanged(nameof(G));
            NotifyPropertyChanged(nameof(B));
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
    }
}