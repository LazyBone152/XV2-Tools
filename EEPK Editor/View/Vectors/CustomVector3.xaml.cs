using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Vectors
{
    public partial class CustomVector3 : UserControl, INotifyPropertyChanged, IDisposable
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
            nameof(Value), typeof(LB_Common.Numbers.CustomVector4), typeof(CustomVector3), new PropertyMetadata(ValueChangedCallback));

        public LB_Common.Numbers.CustomVector4 Value
        {
            get => (LB_Common.Numbers.CustomVector4)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
            nameof(Interval), typeof(float), typeof(CustomVector3), new PropertyMetadata(0.05f));

        public float Interval
        {
            get => (float)GetValue(IntervalProperty);
            set => SetValue(IntervalProperty, value);
        }

        public static readonly DependencyProperty HideUpDownButtonsProperty = DependencyProperty.Register(
            nameof(HideUpDownButtons), typeof(bool), typeof(CustomVector3), new PropertyMetadata(false));

        public bool HideUpDownButtons
        {
            get => (bool)GetValue(HideUpDownButtonsProperty);
            set => SetValue(HideUpDownButtonsProperty, value);
        }

        public static readonly DependencyProperty AllowUndoProperty = DependencyProperty.Register(
            nameof(AllowUndo), typeof(bool), typeof(CustomVector3), new PropertyMetadata(true));

        public bool AllowUndo
        {
            get => (bool)GetValue(AllowUndoProperty);
            set => SetValue(AllowUndoProperty, value);
        }

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            nameof(MinValue), typeof(double), typeof(CustomVector3), new PropertyMetadata(double.MinValue));

        public double MinValue
        {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            nameof(MaxValue), typeof(double), typeof(CustomVector3), new PropertyMetadata(double.MaxValue));

        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        private static void ValueChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(sender is CustomVector3 view)
            {
                view.UpdateProperties();
            }
        }
        #endregion

        public float X
        {
            get => Value != null ? Value.X : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.X));
            }
        }
        public float Y
        {
            get => Value != null ? Value.Y : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.Y));
            }
        }
        public float Z
        {
            get => Value != null ? Value.Z : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.Z));
            }
        }
        
        public CustomVector3()
        {
            InitializeComponent();
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

        private void SetFloatValue(float newValue, string propName)
        {
            float original = (float)Value.GetType().GetProperty(propName).GetValue(Value);

            if (original != newValue)
            {
                Value.GetType().GetProperty(propName).SetValue(Value, newValue);

                if(AllowUndo)
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propName, Value, original, newValue, $"{propName}"));
            }
        }

        private void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(X));
            NotifyPropertyChanged(nameof(Y));
            NotifyPropertyChanged(nameof(Z));
        }
    }
}