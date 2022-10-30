using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows.Media;

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

        public static readonly DependencyProperty HideUpDownButtonsProperty = DependencyProperty.Register(
            nameof(HideUpDownButtons), typeof(bool), typeof(KeyframedValueView), new PropertyMetadata(false));

        public bool HideUpDownButtons
        {
            get { return (bool)GetValue(HideUpDownButtonsProperty); }
            set { SetValue(HideUpDownButtonsProperty, value); }
        }

        private static void OnDpChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(sender is KeyframedValueView view)
            {
                //Handle event subscriptions
                if(e.NewValue is KeyframedBaseValue newValue)
                {
                    newValue.PropertyChanged += view.KeyframedValue_PropertyChanged;
                }

                if (e.OldValue is KeyframedBaseValue oldValue)
                {
                    oldValue.PropertyChanged -= view.KeyframedValue_PropertyChanged;
                }

                view.UpdateProperties();
            }
        }

        private void KeyframedValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(KeyframedBaseValue.IsAnimated))
            {
                NotifyPropertyChanged(nameof(AnimButtonVisible));
                NotifyPropertyChanged(nameof(NotAnimButtonVisible));
            }
        }


        #endregion

        public Visibility Vector2Visibile => KeyframedValue is KeyframedVector2Value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility Vector3Visibile => KeyframedValue is KeyframedVector3Value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ColorVisibile => KeyframedValue is KeyframedColorValue ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FloatVisibile => KeyframedValue is KeyframedFloatValue ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NotAnimButtonVisible => !IsAnimated() ? Visibility.Visible : Visibility.Collapsed;
        public Visibility AnimButtonVisible => IsAnimated() ? Visibility.Visible : Visibility.Collapsed;

        public KeyframedVector2Value Vector2Value => KeyframedValue as KeyframedVector2Value;
        public KeyframedVector3Value Vector3Value => KeyframedValue as KeyframedVector3Value;
        public KeyframedColorValue ColorValue => KeyframedValue as KeyframedColorValue;
        public KeyframedFloatValue FloatValue => KeyframedValue as KeyframedFloatValue;


        public KeyframedValueView()
        {
            InitializeComponent();
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
            NotifyPropertyChanged(nameof(AnimButtonVisible));
            NotifyPropertyChanged(nameof(NotAnimButtonVisible));
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

        private bool IsAnimated()
        {
            if (KeyframedValue == null) return false;
            return KeyframedValue.IsAnimated;
        }
    }
}
