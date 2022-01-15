using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Vectors
{
    /// <summary>
    /// Interaction logic for MatVector4.xaml
    /// </summary>
    public partial class CustomVector4 : UserControl, INotifyPropertyChanged, IDisposable
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
            "Value", typeof(LB_Common.Numbers.CustomVector4), typeof(CustomVector4), new PropertyMetadata(OnDpChanged));

        public LB_Common.Numbers.CustomVector4 Value
        {
            get { return (LB_Common.Numbers.CustomVector4)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
            "Interval", typeof(float), typeof(CustomVector4), new PropertyMetadata(0.05f));

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
        public float W
        {
            get => Value != null ? Value.W : 0;
            set
            {
                if(Value != null) SetFloatValue(value, nameof(Value.W));
            }
        }

        public CustomVector4()
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
            NotifyPropertyChanged(nameof(X));
            NotifyPropertyChanged(nameof(Y));
            NotifyPropertyChanged(nameof(Z));
            NotifyPropertyChanged(nameof(W));
        }
    }
}