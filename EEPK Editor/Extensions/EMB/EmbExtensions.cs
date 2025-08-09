using AForge;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EMB_CLASS
{
    public static class EmbExtensions
    {
        //AFORGE Extensions:
        public static void ChangeHue(this EMB_File embFile, double hue, double saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {
            if (embFile.Entry == null) return;

            foreach(var entry in embFile.Entry)
            {
                entry.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
                entry.SaveDds(true, undos);
            }
        }

        //Make this an extension method so there's not an AForge dependence wherever EMB is used
        public static void ChangeHue(this EmbEntry entry, double hue, double _saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {

            float brightness = (float)lightness / 5f;
            float saturation = (float)_saturation;

            WriteableBitmap oldBitmap = entry.Texture;
            Bitmap bitmap = (Bitmap)entry.Texture;

            if (hueSet)
            {
                if (variance != 0)
                    hue += Random.Range(-variance, variance);

                var hueFilter = new AForge.Imaging.Filters.HueModifier((int)hue);
                hueFilter.ApplyInPlace(bitmap);
            }
            else
            {
                //Hue Adjustment (shifting)

                // Apply filters
                var hueFilter = new AForge.Imaging.Filters.HueAdjustment((int)hue);
                var hslFilter = new AForge.Imaging.Filters.HSLLinear();

                //Apply filters
                hueFilter.ApplyInPlace(bitmap);

                if (lightness > 0 || saturation > 0)
                {
                    //Taken from: https://csharp.hotexamples.com/examples/AForge.Imaging.Filters/HSLLinear/-/php-hsllinear-class-examples.html
                    // create luminance filter
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

                    if (hslFilter.FormatTranslations.ContainsKey(bitmap.PixelFormat))
                        hslFilter.ApplyInPlace(bitmap);
                }
            }

            //Convert back to WPF Bitmap
            entry.Texture = (WriteableBitmap)bitmap;
            entry.wasEdited = true;

            if (undos != null)
                undos.Add(new UndoableProperty<EmbEntry>(nameof(EmbEntry.Texture), entry, oldBitmap, entry.Texture));
        }
    }
}
