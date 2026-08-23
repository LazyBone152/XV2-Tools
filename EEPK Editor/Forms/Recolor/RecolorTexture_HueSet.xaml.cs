using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using MahApps.Metro.Controls;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.Resource.Image;

namespace EEPK_Organiser.Forms.Recolor
{
    /// <summary>
    /// Interaction logic for RecolorTexture_HueSet.xaml
    /// </summary>
    public partial class RecolorTexture_HueSet : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public EmbEntry CurrentTexture { get; set; }
        private readonly WriteableBitmapEditOperation EditOperation;
        private bool IsCancelled = true;

        //Parameters
        private int _hueValue = 0;
        public int HueValue
        {
            get
            {
                return this._hueValue;
            }
            set
            {
                if (value != this._hueValue)
                {
                    this._hueValue = value;
                    NotifyPropertyChanged("HueValue");
                }
            }
        }
        

        //Time
        private Stopwatch stopwatch = new Stopwatch();
        private bool isImageProcessing = false;
        private const int previewDelay = 100;
        private const int previewDelayWait = 10;

        public RecolorTexture_HueSet(EmbEntry _texture, Window parent)
        {
            CurrentTexture = _texture;
            InitializeComponent();
            DataContext = this;
            Owner = parent;

            stopwatch.Start();

            EditOperation = new WriteableBitmapEditOperation(CurrentTexture.Texture);
            CurrentTexture.Texture = EditOperation.OutputBitmap;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            IsCancelled = false;
            CurrentTexture.wasEdited = true;
            List<IUndoRedo> undos = new List<IUndoRedo>()
            {
                new UndoableProperty<EmbEntry>(nameof(EmbEntry.Texture), CurrentTexture, EditOperation.SourceBitmap, CurrentTexture.Texture)
            };
            CurrentTexture.SaveDds(true, undos);

            UndoManager.Instance.AddCompositeUndo(undos, "Hue Set");
            UndoManager.Instance.ForceEventCall();

            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ProcessImage();
        }


        private async Task ProcessImage()
        {
            //If we are already waiting on another process request, then stop here.
            if (isImageProcessing) return;
            isImageProcessing = true;

            //If enough time has not passed since the last process then we must enter a waiting state
            if (stopwatch.ElapsedMilliseconds < previewDelay)
            {
                for (int wait = 0; wait < previewDelay; wait += previewDelayWait)
                {
                    if (stopwatch.ElapsedMilliseconds >= previewDelay) break;
                    await Task.Delay(previewDelayWait);
                }
            }

            await EditOperation.ApplyHueSet(HueValue);

            //Restart the timer
            isImageProcessing = false;
            stopwatch.Restart();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (IsCancelled)
            {
                CurrentTexture.Texture = EditOperation.SourceBitmap;
            }
        }

        private void Button_UndoHueChange_Click(object sender, RoutedEventArgs e)
        {
            HueValue = 0;
            ProcessImage();
        }

        private void IntegerUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            ProcessImage();
        }

    }
}