using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;

namespace EEPK_Organiser.View.Editors.EMP
{
    /// <summary>
    /// Interaction logic for KeyframedValueView.xaml
    /// </summary>
    public partial class KeyframedValueView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DP
        public static readonly DependencyProperty KeyframedValueProperty = DependencyProperty.Register(
            nameof(KeyframedValue), typeof(KeyframedBaseValue), typeof(KeyframedValueView), new PropertyMetadata(OnDpChanged));

        public KeyframedBaseValue KeyframedValue
        {
            get { return (KeyframedBaseValue)GetValue(KeyframedValueProperty); }
            set { SetValue(KeyframedValueProperty, value); }
        }

        public static readonly DependencyProperty NodeProperty = DependencyProperty.Register(
            nameof(Node), typeof(ParticleNode), typeof(KeyframedValueView), new PropertyMetadata(null));

        public ParticleNode Node
        {
            get { return (ParticleNode)GetValue(NodeProperty); }
            set { SetValue(NodeProperty, value); }
        }


        public static readonly DependencyProperty ValueNameProperty = DependencyProperty.Register(
            nameof(ValueName), typeof(string), typeof(KeyframedValueView), new PropertyMetadata(null));

        public string ValueName
        {
            get { return (string)GetValue(ValueNameProperty); }
            set { SetValue(ValueNameProperty, value); }
        }

        public static readonly DependencyProperty IsOnAnimTabProperty = DependencyProperty.Register(
            nameof(IsOnAnimTab), typeof(bool), typeof(KeyframedValueView), new PropertyMetadata(null));

        public bool IsOnAnimTab
        {
            get { return (bool)GetValue(IsOnAnimTabProperty); }
            set { SetValue(IsOnAnimTabProperty, value); }
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

        public Visibility Vector2Visibile => KeyframedValue is KeyframedVector2Value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility Vector3Visibile => KeyframedValue is KeyframedVector3Value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ColorVisibile => KeyframedValue is KeyframedColorValue ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FloatVisibile => KeyframedValue is KeyframedFloatValue ? Visibility.Visible : Visibility.Collapsed;
        public Visibility AnimButtonVisible => !IsOnAnimTab ? Visibility.Visible : Visibility.Collapsed;
        public Visibility AnimCheckBoxVisible => IsOnAnimTab ? Visibility.Visible : Visibility.Collapsed;

        public KeyframedVector2Value Vector2Value => KeyframedValue as KeyframedVector2Value;
        public KeyframedVector3Value Vector3Value => KeyframedValue as KeyframedVector3Value;
        public KeyframedColorValue ColorValue => KeyframedValue as KeyframedColorValue;
        public KeyframedFloatValue FloatValue => KeyframedValue as KeyframedFloatValue;

        public string ValueToolTip
        {
            get
            {
                if (KeyframedValue == null) return null;

                if (KeyframedValue.IsAnimated && !IsOnAnimTab)
                    return "This value is currently keyframed and can only be edited from the Animation Tab.";

                if (!KeyframedValue.IsAnimated && IsOnAnimTab)
                    return "This value is not currently keyframed. Click the checkbox if you wish to enable keyframing.";

                return null;
            }
        }

        public KeyframedValueView()
        {
            InitializeComponent();
            DpChanged += ValueInstanceChanged;
        }

        public void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(KeyframedValue));
            NotifyPropertyChanged(nameof(Node));
            NotifyPropertyChanged(nameof(Vector2Visibile));
            NotifyPropertyChanged(nameof(Vector3Visibile));
            NotifyPropertyChanged(nameof(ColorVisibile));
            NotifyPropertyChanged(nameof(FloatVisibile));

            NotifyPropertyChanged(nameof(Vector2Value));
            NotifyPropertyChanged(nameof(Vector3Value));
            NotifyPropertyChanged(nameof(ColorValue));
            NotifyPropertyChanged(nameof(FloatValue));
        }

        public RelayCommand GoToKeyframedValueCommand => new RelayCommand(GoToKeyframedValue, IsNodeSelected);
        private void GoToKeyframedValue()
        {
            Node.SelectedKeyframedValue = KeyframedValue;
        }

        private bool IsNodeSelected()
        {
            return Node != null;
        }
    }
}
