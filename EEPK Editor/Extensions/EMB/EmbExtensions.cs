using AForge;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
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
            }
        }

        //Make this an extension method so there's not an AForge dependence wherever EMB is used
        public static void ChangeHue(this EmbEntry entry, double hue, double _saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {

            float brightness = (float)lightness / 5f;
            float saturation = (float)_saturation;

            WriteableBitmap oldBitmap = entry.DdsImage;
            Bitmap bitmap = (Bitmap)entry.DdsImage;

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
            entry.DdsImage = (WriteableBitmap)bitmap;
            entry.wasEdited = true;

            if(undos != null)
                undos.Add(new UndoableProperty<EmbEntry>(nameof(EmbEntry.DdsImage), entry, oldBitmap, entry.DdsImage));
        }
    
        public static WriteableBitmap Test(WriteableBitmap bitmap1, WriteableBitmap bitmap2)
        {
            var bitmap = new WriteableBitmap(4096, 4096, 96, 96, PixelFormats.Pbgra32, null);

            Rect sourceRect1 = new Rect(0,0, bitmap1.Width, bitmap1.Height);
            Rect sourceRect2 = new Rect(0, 0, bitmap2.Width, bitmap2.Height);
            Rect destRect1 = new Rect(0, 0, bitmap1.Width, bitmap1.Height);
            Rect destRect2 = new Rect(bitmap1.Width * 2, 0, bitmap2.Width, bitmap2.Height);

            bitmap.Blit(destRect1, bitmap1, sourceRect1);
            bitmap.Blit(destRect2, bitmap2, sourceRect2);

            return bitmap;
        }

        public static WriteableBitmap MergeIntoSuperTexture(List<WriteableBitmap> bitmaps)
        {
            double segementSize = HighestDimension(bitmaps);
            const int textureSize = 4096;
            var superTexture = new WriteableBitmap(textureSize, textureSize, 96, 96, PixelFormats.Bgra32, null);

            for (int i = 0; i < bitmaps.Count; i++)
            {
                double position = segementSize * i / textureSize;
                int row = (int)position;
                int x = (int)((position - row) * textureSize);
                int y = (int)segementSize * row;

                Rect sourceRect = new Rect(0, 0, bitmaps[i].Width, bitmaps[i].Height);
                Rect destRect = new Rect(x, y, bitmaps[i].Width, bitmaps[i].Height);

                superTexture.Blit(destRect, bitmaps[i], sourceRect);
            }

            return superTexture;
        }

        public static double HighestDimension(List<WriteableBitmap> bitmaps)
        {
            double dimension = 0;

            foreach(var bitmap in bitmaps)
            {
                if (bitmap.Width > dimension) dimension = bitmap.Width;
                if (bitmap.Height > dimension) dimension = bitmap.Height;
            }

            return dimension;
        }
    
    }
}
