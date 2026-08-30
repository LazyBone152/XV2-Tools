using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.Resource.Image;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.Forms
{
    public partial class RecolorTexture : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public EmbEntry CurrentTexture { get; set; }
        private readonly WriteableBitmapEditOperation EditOperation;
        private bool IsCancelled = true;

        //Parameters
        private readonly bool IsHueSet = false;
        private int initialHue = 0;
        private int _hueValue = 0;
        public int HueValue
        {
            get
            {
                return _hueValue;
            }
            set
            {
                if (value != _hueValue)
                {
                    _hueValue = value;
                    NotifyPropertyChanged(nameof(HueValue));
                }
            }
        }
        private double _saturationValue = 0;
        public double SaturationValue
        {
            get
            {
                return _saturationValue;
            }
            set
            {
                if (value != _saturationValue)
                {
                    _saturationValue = value;
                    NotifyPropertyChanged(nameof(SaturationValue));
                }
            }
        }
        private double _lightnessValue = 0;
        public double LightnessValue
        {
            get
            {
                return _lightnessValue;
            }
            set
            {
                if (value != _lightnessValue)
                {
                    _lightnessValue = value;
                    NotifyPropertyChanged(nameof(LightnessValue));
                }
            }
        }

        
        //Time
        private readonly Stopwatch stopwatch = new Stopwatch();
        private bool IsImageProcessing = false;
        private readonly int PreviewMillisecondDelay;

        //Tooltips
        public string HueRevertTooltip => string.Format("Revert to original value of {0}", initialHue);


        public RecolorTexture(EmbEntry texture, bool isHueSet, Window parent)
        {
            IsHueSet = isHueSet;
            CurrentTexture = texture;
            EditOperation = new WriteableBitmapEditOperation(CurrentTexture.Texture);
            CurrentTexture.Texture = EditOperation.OutputBitmap;

            InitializeComponent();
            DataContext = this;
            Owner = parent;
            stopwatch.Start();

            if (isHueSet)
            {
                var initial = CurrentTexture.GetDdsColor().ToHsl();
                initialHue = (int)initial.Hue;

                helpTextBlock.Text = "Sets the hue value to the desired amount on all pixels, keeping the saturation and lightness values the same. This will result in the texture being different shades of the same color.";
                saturationGrid.Visibility = Visibility.Collapsed;
                lightnessGrid.Visibility = Visibility.Collapsed;
                Height = 210;
                Title = "Hue Set";
            }
            else
            {
                helpTextBlock.Text = "Adjusts the hue, saturation and lightness (HSL) values, shifting them by the desired amount. This recolors the texture while preserving any color variation.";
            }

            //Dynamically set the delay time based on thread count
            //WriteableBitmapEditOperation will use every thread available, so more generally.... more threads = shorter image processing times
            if (Environment.ProcessorCount >= 16)
            {
                PreviewMillisecondDelay = 50;
            }
            else if(Environment.ProcessorCount >= 8)
            {
                PreviewMillisecondDelay = 100;
            }
            else
            {
                PreviewMillisecondDelay = 250;
            }
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

            UndoManager.Instance.AddCompositeUndo(undos, "Hue Adjustment");
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
            if (IsImageProcessing) return;
            IsImageProcessing = true;

            //If enough time has not passed since the last process then we must enter a waiting state
            if (stopwatch.ElapsedMilliseconds < PreviewMillisecondDelay)
            {
                await Task.Delay(PreviewMillisecondDelay - (int)stopwatch.ElapsedMilliseconds);
            }

            if (IsHueSet)
            {
                await EditOperation.AsyncApplyHueSet(HueValue);
            }
            else
            {
                await EditOperation.AsyncApplyHueAdjust(HueValue, SaturationValue, LightnessValue);
            }
            
            //Restart the timer
            IsImageProcessing = false;
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
            HueValue = initialHue;
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
