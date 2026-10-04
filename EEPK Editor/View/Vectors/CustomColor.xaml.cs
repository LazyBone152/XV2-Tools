using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Color;
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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


        public static readonly DependencyProperty ParticleNodeProperty = DependencyProperty.Register(
            "ParticleNode", typeof(ParticleNode), typeof(CustomColor), new PropertyMetadata(null));

        public ParticleNode ParticleNode
        {
            get { return (ParticleNode)GetValue(ParticleNodeProperty); }
            set { SetValue(ParticleNodeProperty, value); }
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
        public float A
        {
            get => Value != null ? Value.A : 0;
            set
            {
                if (Value != null) SetFloatValue(value, nameof(Value.A));
                NotifyPropertyChanged(nameof(CurrentColor));
            }
        }

        public Color? CurrentColor
        {
            get
            {
                Xv2Color color = new Xv2Color(R, G, B, A);
                return Color.FromScRgb(color.A_Preview_Float, color.R_Preview_Float, color.G_Preview_Float, color.B_Preview_Float);
            }
            set
            {
                SetColorValue(value);
            }
        }

        public Brush Preview => CalculateColorPreview();

        public CustomColor()
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
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propName, Value, original, newValue, $"{propName}"), UndoGroup.ColorControl, undoContext: ParticleNode);

                ColorChangedEvent?.Invoke(this, EventArgs.Empty);
            }
            return (!addUndo) ? new UndoablePropertyGeneric(propName, Value, original, newValue, $"{propName}") : null;
        }

        private void SetColorValue(Color? newValue)
        {
            if (newValue == null || Value == null) return;

            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(SetFloatValue(newValue.Value.ScR, "R", false));
            undos.Add(SetFloatValue(newValue.Value.ScG, "G", false));
            undos.Add(SetFloatValue(newValue.Value.ScB, "B", false));
            undos.Add(SetFloatValue(newValue.Value.ScA, "A", false));

            UndoManager.Instance.AddCompositeUndo(undos, "Color", UndoGroup.ColorControl, undoContext: ParticleNode);
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(R));
            NotifyPropertyChanged(nameof(G));
            NotifyPropertyChanged(nameof(B));
            NotifyPropertyChanged(nameof(A));
            NotifyPropertyChanged(nameof(CurrentColor));
            NotifyPropertyChanged(nameof(Preview));
        }
    
        private SolidColorBrush CalculateColorPreview()
        {
            Xv2Color color = new Xv2Color(R, G, B, A);

            return new SolidColorBrush(Color.FromArgb(255, color.R_Preview, color.G_Preview, color.B_Preview));
        }
    }
}