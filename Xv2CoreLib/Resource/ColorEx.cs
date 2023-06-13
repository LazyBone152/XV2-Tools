using LB_Common.Numbers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.HslColor
{
    public class HslColor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public HslColor(double hue, double lightness, double saturation)
        {
            Hue = hue;
            Lightness = lightness;
            Saturation = saturation;
        }

        private double _hueValue = 0.0;
        private double _lightnessValue = 0.0;
        private double _saturationValue = 0.0;

        /// <summary>
        /// Hue represented by a range of 0 to 360.
        /// </summary>
        public double Hue
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
                    NotifyPropertyChanged("Hue");
                }
            }
        }
        public double Lightness
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
                    NotifyPropertyChanged("Lightness");
                }
            }
        }
        public double Saturation
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
                    NotifyPropertyChanged("Saturation");
                }
            }
        }

        /// <summary>
        /// Sets the hue to the specified value.
        /// </summary>
        public void SetHue(double hue, int randomVariance = 0)
        {
            Hue = (randomVariance != 0) ? hue + (double)Random.Range(-randomVariance, randomVariance) : hue;
        }

        /// <summary>
        /// Change the hue.
        /// </summary>
        /// <param name="amount">The amount to change the hue by. Use a value between 0 and 360.</param>
        public void ChangeHue(double amount)
        {
            Hue += amount;

            if (Hue > 360)
            {
                Hue -= 360;
            }
            else if (Hue < 0)
            {
                Hue += 360;
            }
        }

        public void ChangeLightness(double amount)
        {
            Lightness += amount;

            if (Lightness > 1.0)
            {
                Lightness = 1.0;
            }
            else if (Lightness < 0.0)
            {
                Lightness = 0.0;
            }
        }

        public void ChangeSaturation(double amount)
        {
            Saturation += amount;

            if (Saturation > 1.0)
            {
                Saturation = 1.0;
            }
            else if (Saturation < 0.0)
            {
                Saturation = 0.0;
            }
        }

        public void NotifyPropertiesChanged()
        {
            NotifyPropertyChanged();
        }

        public RgbColor ToRgb()
        {
            return ColorEx.HlsToRgb(Hue, Lightness, Saturation);
        }


    }

    public class RgbColor
    {
        /// <summary>
        /// Creates a RgbColor from three floating point values with a range of 0.0 to 10.0.
        /// </summary>
        public RgbColor(double r, double g, double b)
        {
            var _r = ReadFloatColor(r);
            var _g = ReadFloatColor(g);
            var _b = ReadFloatColor(b);

            R = _r[0];
            G = _g[0];
            B = _b[0];
            R_Multi = _r[1];
            G_Multi = _g[1];
            B_Multi = _b[1];
        }

        public RgbColor(byte r, byte g, byte b)
        {
            R_int = r;
            G_int = g;
            B_int = b;
        }

        public RgbColor(CustomColor customColor)
        {
            R = customColor.R;
            G = customColor.G;
            B = customColor.B;


        }

        public double R { get; set; }
        public double G { get; set; }
        public double B { get; set; }

        public double R_Multi { get; set; }
        public double G_Multi { get; set; }
        public double B_Multi { get; set; }

        //Only preserve multi when value is 0.10 or greater. 
        //public double R_WithMulti { get { return (R <= 0.09) ? R : R + R_Multi; } }
        //public double G_WithMulti { get { return (G <= 0.09) ? G : G + G_Multi; } }
        //public double B_WithMulti { get { return (B <= 0.09) ? B : B + B_Multi; } }
        //private float[] ExtraColor = new float[3];

        public byte R_int { get { return (byte)(R * 255.0); } set { R = value / 255.0; } }
        public byte G_int { get { return (byte)(G * 255.0); } set { G = value / 255.0; } }
        public byte B_int { get { return (byte)(B * 255.0); } set { B = value / 255.0; } }

        public bool IsWhiteOrBlack
        {
            get
            {
                if (R == 0.0 && G == 0.0 && B == 0.0) return true;
                if (R == 1.0 && G == 1.0 && B == 1.0) return true;
                return false;
            }
        }

        private double[] ReadFloatColor(double value)
        {
            int asInt = (int)value;

            if (asInt == value)
            {
                //Whole value (0 or 1)
                return new double[2] { value, 0 };
            }
            else
            {
                //Has remainder
                return new double[2] { value - asInt, asInt };
            }

            /*
            //0 = value, 1 = multi
            double floored = Math.Floor(value);

            if(value - 1.0 < 0.0)
            {
                //Color has no multi
                return new double[] { value, 0.0};
            }
            else if(floored == value)
            {
                //Color is 1.0 + multi
                return new double[] { value - (floored - 1.0), floored - 1.0 };
            }
            else if(floored != 0.0)
            {
                //Value has multi
                return new double[] { value - floored, floored };
            }
            else
            {
                //Value does not have multi
                return new double[] { value, floored };
            }
            */
        }

        public HslColor ToHsl()
        {
            return ColorEx.RgbToHls(R, G, B);
        }

        /// <summary>
        /// Inverts the RGB components of this color.
        /// </summary>
        public void Invert()
        {
            R = 1f - R;
            G = 1f - G;
            B = 1f - B;

            R_Multi = 1f - R_Multi;
            G_Multi = 1f - G_Multi;
            B_Multi = 1f - B_Multi;
        }
    }

    public static class ColorEx
    {
        public static void ChangeHue(this CustomColor color, double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet, int variance, bool invertColor = false)
        {
            if (!color.IsWhiteOrBlack())
            {
                RgbColor rgbColor = new RgbColor(color);

                if (invertColor)
                    rgbColor.Invert();

                HslColor newCol = rgbColor.ToHsl();
                RgbColor convertedColor;

                if (hueSet)
                {
                    newCol.SetHue(hue, variance);
                }
                else
                {
                    newCol.ChangeHue(hue);
                    newCol.ChangeSaturation(saturation);
                    newCol.ChangeLightness(lightness);
                }

                convertedColor = newCol.ToRgb();

                if (invertColor)
                    convertedColor.Invert();

                undos.Add(new UndoableProperty<CustomColor>(nameof(color.R), color, color.R, (float)convertedColor.R));
                undos.Add(new UndoableProperty<CustomColor>(nameof(color.G), color, color.G, (float)convertedColor.G));
                undos.Add(new UndoableProperty<CustomColor>(nameof(color.B), color, color.B, (float)convertedColor.B));

                color.R = (float)convertedColor.R;
                color.G = (float)convertedColor.G;
                color.B = (float)convertedColor.B;

            }
        }

        public static WriteableBitmap HueAdjust(WriteableBitmap bitmap, int hue)
        {
            //Does not work for whatever reason...
            if (hue == 0) return bitmap;

            bitmap.ForEach((x, y, color) => Work(color.R, color.G, color.B, color.A, hue));

            return bitmap;
        }

        private static System.Windows.Media.Color Work(byte r, byte g, byte b, byte a, int hue)
        {
            HslColor hslColor = new RgbColor(r, g, b).ToHsl();
            hslColor.ChangeHue(hue);
            var newColor = hslColor.ToRgb();
            return new System.Windows.Media.Color() { R = newColor.R_int, G = newColor.G_int, B = newColor.B_int, A = a };
        }

        public static RgbColor GetAverageColor(List<RgbColor> colors)
        {
            //Total RGB values (no alpha)
            ulong[] totals = new ulong[3];

            foreach (var color in colors)
            {
                totals[0] += color.R_int;
                totals[1] += color.G_int;
                totals[2] += color.B_int;
            }

            return new RgbColor((byte)(totals[0] / (ulong)colors.Count), (byte)(totals[1] / (ulong)colors.Count), (byte)(totals[2] / (ulong)colors.Count));
        }

        public static HslColor RgbToHsl(int r, int g, int b)
        {
            return RgbToHls(GetFloatColor(r), GetFloatColor(g), GetFloatColor(b));
        }

        private static double GetFloatColor(int color)
        {
            return color / 255.0;
        }

        // Convert an RGB value into an HLS value.
        public static HslColor RgbToHls(double r, double g, double b)
        {
            double h;
            double l;
            double s;

            // Get the maximum and minimum RGB components.
            double max = r;
            if (max < g) max = g;
            if (max < b) max = b;

            double min = r;
            if (min > g) min = g;
            if (min > b) min = b;

            double diff = max - min;
            l = (max + min) / 2;
            if (Math.Abs(diff) < 0.00001)
            {
                s = 0;
                h = 0;  // H is really undefined.
            }
            else
            {
                if (l <= 0.5) s = diff / (max + min);
                else s = diff / (2 - max - min);

                double r_dist = (max - r) / diff;
                double g_dist = (max - g) / diff;
                double b_dist = (max - b) / diff;

                if (r == max) h = b_dist - g_dist;
                else if (g == max) h = 2 + r_dist - b_dist;
                else h = 4 + g_dist - r_dist;

                h = h * 60;
                if (h < 0) h += 360;
            }

            return new HslColor(h, l, s);
        }

        // Convert an HLS value into an RGB value.
        public static RgbColor HlsToRgb(double h, double l, double s)
        {
            double p2;
            if (l <= 0.5) p2 = l * (1 + s);
            else p2 = l + s - l * s;

            double p1 = 2 * l - p2;
            double double_r, double_g, double_b;
            if (s == 0)
            {
                double_r = l;
                double_g = l;
                double_b = l;
            }
            else
            {
                double_r = QqhToRgb(p1, p2, h + 120);
                double_g = QqhToRgb(p1, p2, h);
                double_b = QqhToRgb(p1, p2, h - 120);
            }

            //Return color
            return new RgbColor(double_r, double_g, double_b);
        }

        private static double QqhToRgb(double q1, double q2, double hue)
        {
            if (hue > 360) hue -= 360;
            else if (hue < 0) hue += 360;

            if (hue < 60) return q1 + (q2 - q1) * hue / 60;
            if (hue < 180) return q2;
            if (hue < 240) return q1 + (q2 - q1) * (240 - hue) / 60;
            return q1;
        }
    }
}

