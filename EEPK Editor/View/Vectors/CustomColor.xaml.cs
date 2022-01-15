using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Vectors
{
    /// <summary>
    /// Interaction logic for MatVector4.xaml
    /// </summary>
    public partial class CustomColor : UserControl, INotifyPropertyChanged, IDisposable
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region DP
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(LB_Common.Numbers.CustomColor), typeof(CustomColor), new PropertyMetadata(OnDpChanged));

        public LB_Common.Numbers.CustomColor Value
        {
            get { return (LB_Common.Numbers.CustomColor)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
            "Interval", typeof(float), typeof(CustomColor), new PropertyMetadata(0.05f));

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

        public float R
        {
            get => Value != null ? Value.R : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.R));
                NotifyPropertyChanged(nameof(Preview));
            }
        }
        public float G
        {
            get => Value != null ? Value.G : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.G));
                NotifyPropertyChanged(nameof(Preview));
            }
        }
        public float B
        {
            get => Value != null ? Value.B : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.B));
                NotifyPropertyChanged(nameof(Preview));
            }
        }
        public float A
        {
            get => Value != null ? Value.A : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.A));
            }
        }

        public Brush Preview => CalculateColorPreview();

        public CustomColor()
        {
            InitializeComponent();
            DpChanged += ValueInstanceChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object sender, EventArgs e)
        {
            UpdateProperties();
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private void SetFloatValue(float newValue, string propName)
        {
            float original = (float)Value.GetType().GetProperty(propName).GetValue(Value);

            if (original != newValue)
            {
                Value.GetType().GetProperty(propName).SetValue(Value, newValue);

                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propName, Value, original, newValue, $"{propName}"));
            }
        }

        private void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(R));
            NotifyPropertyChanged(nameof(G));
            NotifyPropertyChanged(nameof(B));
            NotifyPropertyChanged(nameof(A));
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