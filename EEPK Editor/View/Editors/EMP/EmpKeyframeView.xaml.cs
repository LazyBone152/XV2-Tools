using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using GalaSoft.MvvmLight.CommandWpf;

namespace EEPK_Organiser.View.Editors.EMP
{
    /// <summary>
    /// Interaction logic for EmpKeyframeView.xaml
    /// </summary>
    public partial class EmpKeyframeView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DP
        public static readonly DependencyProperty KeyframeProperty = DependencyProperty.Register(
            nameof(Keyframe), typeof(KeyframeBaseValue), typeof(EmpKeyframeView), new PropertyMetadata(null, ValueInstanceChanged));

        public KeyframeBaseValue Keyframe
        {
            get => (KeyframeBaseValue)GetValue(KeyframeProperty);
            set => SetValue(KeyframeProperty, value);
        }

        private static void ValueInstanceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(sender is EmpKeyframeView view)
            {
                view.UpdateProperties();
            }
        }

        #endregion

        public Visibility Vector2Visibile => Keyframe is KeyframeVector2Value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility Vector3Visibile => Keyframe is KeyframeVector3Value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ColorVisibile => Keyframe is KeyframeColorValue ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FloatVisibile => Keyframe is KeyframeFloatValue ? Visibility.Visible : Visibility.Collapsed;

        public KeyframeVector2Value Vector2Value => Keyframe as KeyframeVector2Value;
        public KeyframeVector3Value Vector3Value => Keyframe as KeyframeVector3Value;
        public KeyframeColorValue ColorValue => Keyframe as KeyframeColorValue;
        public KeyframeFloatValue FloatValue => Keyframe as KeyframeFloatValue;

        public EmpKeyframeView()
        {
            InitializeComponent();
        }

        public void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(Keyframe));
            NotifyPropertyChanged(nameof(Vector2Visibile));
            NotifyPropertyChanged(nameof(Vector3Visibile));
            NotifyPropertyChanged(nameof(ColorVisibile));
            NotifyPropertyChanged(nameof(FloatVisibile));

            NotifyPropertyChanged(nameof(Vector2Value));
            NotifyPropertyChanged(nameof(Vector3Value));
            NotifyPropertyChanged(nameof(ColorValue));
            NotifyPropertyChanged(nameof(FloatValue));
        }
    }
}
