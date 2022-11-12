using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Vectors
{
    public partial class CustomVector2 : UserControl, INotifyPropertyChanged
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
            nameof(Value), typeof(LB_Common.Numbers.CustomVector4), typeof(CustomVector2), new PropertyMetadata(ValueChangedCallback));

        public LB_Common.Numbers.CustomVector4 Value
        {
            get { return (LB_Common.Numbers.CustomVector4)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
            nameof(Interval), typeof(float), typeof(CustomVector2), new PropertyMetadata(0.05f));

        public float Interval
        {
            get { return (float)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }

        public static readonly DependencyProperty HideUpDownButtonsProperty = DependencyProperty.Register(
            nameof(HideUpDownButtons), typeof(bool), typeof(CustomVector2), new PropertyMetadata(false));

        public bool HideUpDownButtons
        {
            get { return (bool)GetValue(HideUpDownButtonsProperty); }
            set { SetValue(HideUpDownButtonsProperty, value); }
        }

        public static readonly DependencyProperty AllowUndoProperty = DependencyProperty.Register(
            nameof(AllowUndo), typeof(bool), typeof(CustomVector2), new PropertyMetadata(true));

        public bool AllowUndo
        {
            get { return (bool)GetValue(AllowUndoProperty); }
            set { SetValue(AllowUndoProperty, value); }
        }

        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
            nameof(TextAlignment), typeof(TextAlignment), typeof(CustomVector2), new PropertyMetadata(TextAlignment.Right));

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        //IsReadOnly
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(CustomVector2), new PropertyMetadata(false));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        private static void ValueChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(sender is CustomVector2 view)
            {
                view.UpdateProperties();
                view.UpdateEvents(e.NewValue, e.OldValue);
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

        public string X_Preview => $"{X.ToString("0.0###")}";
        public string Y_Preview => $"{Y.ToString("0.0###")}";

        public Visibility EditableVisibility => !IsReadOnly ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ReadOnlyVisibility => IsReadOnly ? Visibility.Visible : Visibility.Collapsed;

        public CustomVector2()
        {
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Loaded += CustomVector3_Loaded;
            Unloaded += CustomVector3_Unloaded;
        }
        private void CustomVector3_Unloaded(object sender, RoutedEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;

            if (Value != null)
                Value.PropertyChanged -= Vector_PropertyChanged;
        }

        private void CustomVector3_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(EditableVisibility));
            NotifyPropertyChanged(nameof(ReadOnlyVisibility));
        }

        private void Instance_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            UpdateProperties();
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
            NotifyPropertyChanged(nameof(X_Preview));
            NotifyPropertyChanged(nameof(Y_Preview));
        }

        private void UpdateEvents(object newValue, object oldValue)
        {
            if (oldValue is LB_Common.Numbers.CustomVector4 oldVector)
            {
                oldVector.PropertyChanged -= Vector_PropertyChanged;
            }

            if (newValue is LB_Common.Numbers.CustomVector4 newVector)
            {
                newVector.PropertyChanged += Vector_PropertyChanged;
            }
        }

        private void Vector_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateProperties();
        }
    }
}