using LB_Common.Numbers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.Color
{
    public static class ColorEx
    {
        #region Hue Adjustment
        public static bool ChangeHue(this CustomColor color, double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet, int variance, bool invertColor = false)
        {
            if (!color.IsWhiteOrBlack() && !color.IsSameChannels())
            {
                Xv2Color xv2Color = new Xv2Color(color);

                if (invertColor)
                    xv2Color = xv2Color.Invert();

                if (hueSet)
                {
                    xv2Color = xv2Color.ApplyHueSet((int)hue, variance);
                }
                else
                {
                    xv2Color = xv2Color.ApplyHueAdjustment((int)hue, (float)saturation, (float)lightness);
                }

                if (invertColor)
                    xv2Color = xv2Color.Invert();

                undos?.Add(new UndoableProperty<CustomColor>(nameof(color.R), color, color.R, xv2Color.R));
                undos?.Add(new UndoableProperty<CustomColor>(nameof(color.G), color, color.G, xv2Color.G));
                undos?.Add(new UndoableProperty<CustomColor>(nameof(color.B), color, color.B, xv2Color.B));

                color.R = xv2Color.R;
                color.G = xv2Color.G;
                color.B = xv2Color.B;

                return true;
            }
            else
            {
                return false;
            }
        }

        public static System.Windows.Media.Color HueAdjust(byte r, byte g, byte b, byte a, int hue, double saturation, double lightness)
        {
            RgbToHsl(r / 255.0, g / 255.0, b / 255.0, out double h, out double s, out double l);

            h = (h + hue) % 360.0;
            if (h < 0)
                h += 360.0;

            // Apply saturation and lightness adjustments.
            // Treat saturation and lightness as multipliers (1.0 = no change).
            s = Math.Min(1.0, Math.Max(0.0, s * saturation));
            l = Math.Min(1.0, Math.Max(0.0, l * lightness));

            HslToRgb(h, s, l, out double newR, out double newG, out double newB);
            return new System.Windows.Media.Color() { R = (byte)(newR * 255.0), G = (byte)(newG * 255.0), B = (byte)(newB * 255.0), A = a };
        }

        public static System.Windows.Media.Color HueSet(byte r, byte g, byte b, byte a, int hue)
        {
            RgbToHsl(r / 255.0, g / 255.0, b / 255.0, out double h, out double s, out double l);

            h = hue % 360.0;
            if (h < 0)
                h += 360.0;

            HslToRgb(h, s, l, out double newR, out double newG, out double newB);
            return new System.Windows.Media.Color() { R = (byte)(newR * 255.0), G = (byte)(newG * 255.0), B = (byte)(newB * 255.0), A = a };
        }

        public static void HueAdjust(float r, float g, float b, int hue, float saturation, float lightness, out float newR, out float newG, out float newB)
        {
            RgbToHsl(r, g, b, out double h, out double s, out double l);

            h = (h + hue) % 360.0;
            if (h < 0)
                h += 360.0;

            // Apply saturation and lightness adjustments.
            // Incoming saturation and lightness values is the relative difference
            s = Math.Min(1.0, Math.Max(0.0, s + saturation));
            l = Math.Min(1.0, Math.Max(0.0, l + lightness));

            HslToRgb(h, s, l, out double rOut, out double gOut, out double bOut);
            newR = (float)rOut;
            newG = (float)gOut;
            newB = (float)bOut;
        }

        public static void HueSet(float r, float g, float b, int hue, out float newR, out float newG, out float newB)
        {
            RgbToHsl(r, g, b, out double h, out double s, out double l);

            h = hue % 360.0;
            if (h < 0)
                h += 360.0;

            HslToRgb(h, s, l, out double rOut, out double gOut, out double bOut);

            newR = (float)rOut;
            newG = (float)gOut;
            newB = (float)bOut;
        }

        #endregion

        #region RGB > HSL
        public static void RgbToHsl(double r, double g, double b, out double hue, out double saturation, out double lightness)
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

            hue = h;
            lightness = l;
            saturation = s;
        }

        public static void ExtractHsl(this CustomColor color, out double hue, out double saturation, out double lightness)
        {
            RgbToHsl(color.R, color.G, color.B, out hue, out saturation, out lightness);
        }

        #endregion

        #region HSL > RGB

        public static void HslToRgb(double h, double s, double l, out double r, out double g, out double b)
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
            r = double_r;
            g = double_g;
            b = double_b;
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

        public static void SetHsl(this CustomColor color, double hue, double saturation, double lightness)
        {
            HslToRgb(hue, saturation, lightness, out double r, out double g, out double b);
            color.R = (float)r;
            color.G = (float)g;
            color.B = (float)b;
        }

        #endregion
    }

    [Obsolete("Replaced by Xv2Color")]
    public class HslColor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
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
            ColorEx.HslToRgb(Hue, Saturation, Lightness, out double r, out double g, out double b);
            return new RgbColor(r, g, b);
        }
    }

    [Obsolete("Replaced by Xv2Color and Xv2ColorAccumulator")]
    public class RgbColor
    {
        public RgbColor(double r, double g, double b)
        {
            R = Xv2Color.CalculatePreviewValue((float)r);
            G = Xv2Color.CalculatePreviewValue((float)g);
            B = Xv2Color.CalculatePreviewValue((float)b);
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

        public HslColor ToHsl()
        {
            ColorEx.RgbToHsl(R, G, B, out double h, out double s, out double l);
            return new HslColor(h, l, s);
        }

        /// <summary>
        /// Inverts the RGB components of this color.
        /// </summary>
        public void Invert()
        {
            R = 1f - R;
            G = 1f - G;
            B = 1f - B;
        }
    }
}