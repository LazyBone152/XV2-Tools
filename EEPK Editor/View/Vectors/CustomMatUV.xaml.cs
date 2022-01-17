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
    public partial class CustomMatUV : UserControl, INotifyPropertyChanged, IDisposable
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
            "Value", typeof(LB_Common.Numbers.CustomMatUV), typeof(CustomMatUV), new PropertyMetadata(OnDpChanged));

        public LB_Common.Numbers.CustomMatUV Value
        {
            get { return (LB_Common.Numbers.CustomMatUV)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
            "Interval", typeof(float), typeof(CustomMatUV), new PropertyMetadata(0.05f));

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

        public float U
        {
            get => Value != null ? Value.U : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.U));
            }
        }
        public float V
        {
            get => Value != null ? Value.V : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.V));
            }
        }
       
        public CustomMatUV()
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
            NotifyPropertyChanged(nameof(U));
            NotifyPropertyChanged(nameof(V));
        }
    }
}