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

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for TextureEditHueChange.xaml
    /// </summary>
    public partial class TextureEditHueChange : MetroWindow, INotifyPropertyChanged
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
        private double _saturationValue = 0;
        public double SaturationValue
        {
            get
            {
                return this._saturationValue;
            }
            set
            {
                if (value != this._saturationValue)
                {
                    this._saturationValue = value;
                    NotifyPropertyChanged("SaturationValue");
                }
            }
        }
        private double _lightnessValue = 0;
        public double LightnessValue
        {
            get
            {
                return this._lightnessValue;
            }
            set
            {
                if (value != this._lightnessValue)
                {
                    this._lightnessValue = value;
                    NotifyPropertyChanged("LightnessValue");
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

        public TextureEditHueChange(EmbEntry _texture, Window parent)
        {
            CurrentTexture = _texture;
            InitializeComponent();
            DataContext = this;
            Owner = parent;

            OriginalTextureBackup = CurrentTexture.DdsImage;
            stopwatch.Start();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            cancelled = false;
            CurrentTexture.wasEdited = true;
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
                //For loop is safer than while.
                for(int wait = 0; wait < previewDelay; wait += previewDelayWait)
                {
                    if (stopwatch.ElapsedMilliseconds >= previewDelay) break;
                    await Task.Delay(previewDelayWait);
                }

                //while(stopwatch.ElapsedMilliseconds < previewDelay)
                //{
                //    await Task.Delay(10);
                //}
            }

            //Create System.Drawing.Bitmap
            Bitmap bitmap = (Bitmap)OriginalTextureBackup;
            // Apply filters
            var hueFilter = new AForge.Imaging.Filters.HueAdjustment(HueValue);
            var satFilter = new AForge.Imaging.Filters.SaturationCorrection((float)SaturationValue);
            var hslFilter = new AForge.Imaging.Filters.HSLLinear();

            float brightness = (float)LightnessValue;

            if (brightness > 0)
            {
                hslFilter.InLuminance = new Range(0.0f, 1.0f - brightness); //TODO - isn't it better not to truncate, but compress?
                hslFilter.OutLuminance = new Range(brightness, 1.0f);
            }
            else
            {
                hslFilter.InLuminance = new Range(-brightness, 1.0f);
                hslFilter.OutLuminance = new Range(0.0f, 1.0f + brightness);
            }
            // create saturation filter
            float saturation = (float)SaturationValue;
            if (saturation > 0)
            {
                hslFilter.InSaturation = new Range(0.0f, 1.0f - saturation); //Ditto?
                hslFilter.OutSaturation = new Range(saturation, 1.0f);
            }
            else
            {
                hslFilter.InSaturation = new Range(-saturation, 1.0f);
                hslFilter.OutSaturation = new Range(0.0f, 1.0f + saturation);
            }

            bitmap = hueFilter.Apply(bitmap);
            hslFilter.ApplyInPlace(bitmap);

            //Convert back to WPF Bitmap
            CurrentTexture.DdsImage = (WriteableBitmap)bitmap;

            //Restart the timer
            isImageProcessing = false;
            stopwatch.Restart();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (cancelled)
            {
                CurrentTexture.DdsImage = OriginalTextureBackup;
            }
        }


        private void Button_UndoHueChange_Click(object sender, RoutedEventArgs e)
        {
            HueValue = 0;
            ProcessImage();
        }

        private void Button_UndoSaturationChange_Click(object sender, RoutedEventArgs e)
        {
            SaturationValue = 0;
            ProcessImage();
        }

        private void Button_UndoLightnessChange_Click(object sender, RoutedEventArgs e)
        {
            LightnessValue = 0;
            ProcessImage();
        }

        private void IntegerUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            ProcessImage();
        }
    }
}
