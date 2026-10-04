using LB_Common.Mvvm;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Xv2CoreLib.Color;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.Resource.Image;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.Forms
{
    public partial class RecolorTexture : AutoObservableWindow
    {
        private readonly EMB_TextureEntry CurrentTexture;
        private readonly IList<EMB_TextureEntry> Textures;
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
        private bool ReadyForProcessing = false; //Initialize to false to prevent image processing during the start up phase
        private readonly int PreviewMillisecondDelay;

        //Tooltips
        public string HueRevertTooltip => $"Revert to original value of {initialHue:##0.0#}";


        public RecolorTexture(EMB_TextureEntry primaryTexture, List<EMB_TextureEntry> otherTextures, bool isHueSet)
        {
            IsHueSet = isHueSet;
            CurrentTexture = primaryTexture;
            Textures = otherTextures;
            Textures.Remove(CurrentTexture);
            EditOperation = new WriteableBitmapEditOperation(CurrentTexture.Texture);
            CurrentTexture.Texture = EditOperation.OutputBitmap;
            InitializeComponent();
            DataContext = this;

            stopwatch.Start();

            if (isHueSet)
            {
                helpTextBlock.Text = "Sets the hue value to the desired amount on all pixels, keeping the saturation and lightness values the same. This will result in the texture being different shades of the same color.";
                saturationGrid.Visibility = Visibility.Collapsed;
                lightnessGrid.Visibility = Visibility.Collapsed;
                hueSlider.Minimum = 0;
                hueSlider.Maximum = 359.0;
                hueUpDown.Minimum = 0;
                hueUpDown.Maximum = 359.0;
                Height = 210;
                Title = Textures.Count > 0 ? $"Hue Set ({Textures.Count + 1} textures selected)" : $"Hue Set (1 texture selected)";

                Xv2Color textureColor = EditOperation.CalculateAverageColor();
                textureColor.ToHsl(out double hue, out _, out _);
                initialHue = (int)hue;
                HueValue = initialHue;
            }
            else
            {
                helpTextBlock.Text = "Adjusts the hue, saturation and lightness (HSL) values, shifting them by the desired amount. This recolors the texture while preserving any color variation.";
                Title = Textures.Count > 0 ? $"Hue Adjustment ({Textures.Count + 1} textures selected)" : $"Hue Adjustment (1 texture selected)";
            }

            //Dynamically set the delay time based on thread count
            //WriteableBitmapEditOperation will use every thread available, so generally.... more threads = shorter image processing times
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

            Loaded += RecolorTexture_Loaded;
        }

        private void RecolorTexture_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(HueRevertTooltip));
            ReadyForProcessing = true;
            ProcessImage();
        }

        private async void Ok_Click(object sender, RoutedEventArgs e)
        {
            IsCancelled = false;

            var controller = await this.ShowProgressAsync("Finalizing...", "Applying hue adjustment to other selected textures...", false, DialogSettings.Default);
            controller.SetIndeterminate();

            try
            {
                await FinalizeEdit();
            }
            finally
            {
                await controller.CloseAsync();
            }
            
            Close();
        }

        private async Task FinalizeEdit()
        {
            CurrentTexture.wasEdited = true;

            List<IUndoRedo> undos = new List<IUndoRedo>()
            {
                new UndoableProperty<EMB_TextureEntry>(nameof(EMB_TextureEntry.Texture), CurrentTexture, EditOperation.SourceBitmap, CurrentTexture.Texture)
            };
            CurrentTexture.SaveDds(true, undos);

            foreach(var texture in Textures)
            {
                WriteableBitmapEditOperation editOp = new WriteableBitmapEditOperation(texture.Texture);

                if (IsHueSet)
                {
                    await editOp.AsyncApplyHueSet(HueValue);
                }
                else
                {
                    await editOp.AsyncApplyHueAdjust(HueValue, SaturationValue, LightnessValue);
                }

                undos.Add(new UndoableProperty<EMB_TextureEntry>(nameof(EMB_TextureEntry.Texture), texture, editOp.SourceBitmap, editOp.OutputBitmap));
                texture.Texture = editOp.OutputBitmap;
                texture.SaveDds(true, undos);
            }

            UndoManager.Instance.AddCompositeUndo(undos, IsHueSet ? "Hue Set" : "Hue Adjustment");
            UndoManager.Instance.ForceEventCall();
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
            if (!ReadyForProcessing) return;
            ReadyForProcessing = false;

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
            ReadyForProcessing = true;
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
