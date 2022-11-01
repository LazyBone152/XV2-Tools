using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Xv2CoreLib.EMB_CLASS;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;
using AForge;
using MahApps.Metro.Controls;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.Forms.Recolor
{
    /// <summary>
    /// Interaction logic for RecolorTexture_HueSet.xaml
    /// </summary>
    public partial class RecolorTexture_HueSet : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public EmbEntry CurrentTexture { get; set; }
        private WriteableBitmap OriginalTextureBackup = null;
        private bool cancelled = true;

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

        public bool IsSupportedFormat
        {
            get
            {
                var formats = new AForge.Imaging.Filters.HSLLinear().FormatTranslations;
                return (formats.ContainsKey(((Bitmap)OriginalTextureBackup).PixelFormat));
            }
        }

        public RecolorTexture_HueSet(EmbEntry _texture, Window parent)
        {
            CurrentTexture = _texture;
            InitializeComponent();
            DataContext = this;
            Owner = parent;

            OriginalTextureBackup = CurrentTexture.Texture;
            stopwatch.Start();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            cancelled = false;
            CurrentTexture.wasEdited = true;
            UndoManager.Instance.AddUndo(new UndoableProperty<EmbEntry>(nameof(EmbEntry.Texture), CurrentTexture, OriginalTextureBackup, CurrentTexture.Texture, "Hue Set"));
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

            //Create System.Drawing.Bitmap
            Bitmap bitmap = (Bitmap)OriginalTextureBackup;
            // Apply filters
            var hueFilter = new AForge.Imaging.Filters.HueModifier(HueValue);


            bitmap = hueFilter.Apply(bitmap);

            //Convert back to WPF Bitmap
            CurrentTexture.Texture = (WriteableBitmap)bitmap;

            //Restart the timer
            isImageProcessing = false;
            stopwatch.Restart();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (cancelled)
            {
                CurrentTexture.Texture = OriginalTextureBackup;
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
