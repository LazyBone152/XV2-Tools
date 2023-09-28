using MahApps.Metro.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for KeyframeScale.xaml
    /// </summary>
    public partial class KeyframeScale : MetroWindow, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private KeyframedFloatValue keyframedValue = null;

        public float ScaleFactor { get; set; } = 1.0f;
        private float OriginalScale;

        private bool useFactor = false;
        public bool UseFactor
        {
            get => useFactor;
            set
            {
                useFactor = value;

                if (useFactor)
                {
                    valueLabel.Content = "Factor";
                    ScaleFactor = 1f;
                }
                else
                {
                    valueLabel.Content = "Avg Value";
                    ScaleFactor = OriginalScale;
                }

                NotifyPropertyChanged(nameof(ScaleFactor));
            }
        }

        public KeyframeScale(KeyframedFloatValue value, Window parent = null)
        {
            Owner = parent;
            keyframedValue = value;
            InitializeComponent();
            DataContext = this;

            OriginalScale = keyframedValue.GetAverageValue();
            ScaleFactor = OriginalScale;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            if (ScaleFactor != OriginalScale)
            {
                float factor = useFactor ? ScaleFactor : ScaleFactor / OriginalScale;
                List<IUndoRedo> undos = keyframedValue.RescaleValue(factor);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Scale Keyframes"));
                UndoManager.Instance.ForceEventCall();
            }

            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
