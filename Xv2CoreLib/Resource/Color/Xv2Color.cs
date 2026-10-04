using LB_Common.Numbers;
using System;
using System.Collections.Generic;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.Color
{
    /// <summary>
    /// Represents an arbitary color (integer, float, or HDR).
    /// </summary>
    public readonly struct Xv2Color
    {
        public static readonly Xv2Color White = new Xv2Color(1f, 1f, 1f, 1f);
        public static readonly Xv2Color Transparent = new Xv2Color(0f, 0f, 0f, 0f);

        public readonly float R;
        public readonly float G;
        public readonly float B;
        public readonly float A;

        public byte R_Byte => (byte)(R * 255f);
        public byte G_Byte => (byte)(G * 255f);
        public byte B_Byte => (byte)(B * 255f);
        public byte A_Byte => (byte)(MathHelpers.Clamp(0f, 1f, A) * 255.0); //Some alpha values can be above 1 (unsure why), so to prevent overflow the value is clamped to 1.0 before converting to byte

        /// <summary>
        /// Red component of the color with HDR color range stripped out. Intended for UI preview purposes. Rgb color format (0 - 255).
        /// </summary>
        public byte R_Preview => (byte)(CalculatePreviewValue(R) * 255f);
        /// <summary>
        /// Green component of the color with HDR color range stripped out. Intended for UI preview purposes. Rgb color format (0 - 255).
        /// </summary>
        public byte G_Preview => (byte)(CalculatePreviewValue(G) * 255f);
        /// <summary>
        /// Blue component of the color with HDR color range stripped out. Intended for UI preview purposes. Rgb color format (0 - 255).
        /// </summary>
        public byte B_Preview => (byte)(CalculatePreviewValue(B) * 255f);
        public byte A_Preview => A_Byte;

        /// <summary>
        /// Red component of the color with HDR color range stripped out. Intended for UI preview purposes. ScRgb color format (0 - 1.0).
        /// </summary>
        public float R_Preview_Float => CalculatePreviewValue(R);
        /// <summary>
        /// Green component of the color with HDR color range stripped out. Intended for UI preview purposes. ScRgb color format (0 - 1.0).
        /// </summary>
        public float G_Preview_Float => CalculatePreviewValue(G);
        /// <summary>
        /// Blue component of the color with HDR color range stripped out. Intended for UI preview purposes. ScRgb color format (0 - 1.0).
        /// </summary>
        public float B_Preview_Float => CalculatePreviewValue(B);
        /// <summary>
        /// Alpha component of the color in ScRgb color format (0 - 1.0). Intended for UI preview purposes.
        /// </summary>
        public float A_Preview_Float => MathHelpers.Clamp(0f, 1f, A);

        public bool IsHDR => Math.Max(R, Math.Max(G, B)) > 1f;
        public bool IsTransparent => R == 0f && G == 0f && B == 0f && A == 0f;
        public bool IsWhiteOrBlack => (R == 1f && G == 1f && B == 1f) || (R == 0f && G == 0f && B == 0f);

        #region Constructor
        public Xv2Color(CustomColor color)
        {
            R = color.R;
            G = color.G;
            B = color.B;
            A = color.A;
        }

        /// <summary>
        /// Construct a <see cref="Xv2Color"/> from standard floating point color format RGB components (0 - 1.0) or HDR color format RGB components (1.0+).
        /// </summary>
        public Xv2Color(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
            A = 1f;
        }

        /// <summary>
        /// Construct a <see cref="Xv2Color"/> from standard floating point color format RGBA components (0 - 1.0) or HDR color format RGBA components (1.0+).
        /// </summary>
        public Xv2Color(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        
        /// <summary>
        /// Construct a <see cref="Xv2Color"/> out of byte format RGB components (0 - 255).
        /// </summary>
        public Xv2Color(byte r, byte g, byte b)
        {
            R = r / 255f;
            G = g / 255f;
            B = b / 255f;
            A = 1f;
        }

        /// <summary>
        /// Construct a <see cref="Xv2Color"/> out of byte format RGBA components (0 - 255).
        /// </summary>
        public Xv2Color(byte r, byte g, byte b, byte a)
        {
            R = r / 255f;
            G = g / 255f;
            B = b / 255f;
            A = a / 255f;
        }

        #endregion

        #region Methods
        /// <summary>
        /// Applies a hue adjustment to the color and returns a new <see cref="Xv2Color"/> instance with the adjusted color.
        /// </summary>
        public Xv2Color ApplyHueAdjustment(int hue, float saturation, float lightness, bool invert = false)
        {
            float max = Math.Max(R, Math.Max(G, B));
            float scale = max > 1f ? max : 1f;

            float normalizedR = R / scale;
            float normalizedG = G / scale;
            float normalizedB = B / scale;

            if (invert)
            {
                normalizedR = 1f - normalizedR;
                normalizedG = 1f - normalizedG;
                normalizedB = 1f - normalizedB;
            }

            ColorEx.HueAdjust(normalizedR, normalizedG, normalizedB, hue, saturation, lightness, out float newNormalizedR, out float newNormalizedG, out float newNormalizedB);

            if (invert)
            {
                return new Xv2Color((1f - newNormalizedR) * scale, (1f - newNormalizedG) * scale, (1f - newNormalizedB) * scale, A);
            }
            else
            {
                return new Xv2Color(newNormalizedR * scale, newNormalizedG * scale, newNormalizedB * scale, A);
            }
        }

        /// <summary>
        /// Applies a hue set to the color and returns a new <see cref="Xv2Color"/> instance with the adjusted color.
        /// </summary>
        public Xv2Color ApplyHueSet(int hue, int variance = 0, bool invert = false)
        {
            if (variance != 0)
                hue += Random.Range(-variance, variance);

            float max = Math.Max(R, Math.Max(G, B));
            float scale = max > 1f ? max : 1f;

            float normalizedR = R / scale;
            float normalizedG = G / scale;
            float normalizedB = B / scale;

            if (invert)
            {
                normalizedR = 1f - normalizedR;
                normalizedG = 1f - normalizedG;
                normalizedB = 1f - normalizedB;
            }

            ColorEx.HueSet(normalizedR, normalizedG, normalizedB, hue, out float newNormalizedR, out float newNormalizedG, out float newNormalizedB);

            if (invert)
            {
                return new Xv2Color((1f - newNormalizedR) * scale, (1f - newNormalizedG) * scale, (1f - newNormalizedB) * scale, A);
            }
            else
            {
                return new Xv2Color(newNormalizedR * scale, newNormalizedG * scale, newNormalizedB * scale, A);
            }
        }
        
        public static Xv2Color FromHsl(double hue, double saturation, double lightness)
        {
            ColorEx.HslToRgb(hue, saturation, lightness, out double r, out double g, out double b);
            return new Xv2Color((float)r, (float)g, (float)b, 1f);
        }

        public void ToHsl(out double hue, out double saturation, out double lightness)
        {
            ColorEx.RgbToHsl(R_Preview_Float, G_Preview_Float, B_Preview_Float, out hue, out saturation, out lightness);
        }

        /// <summary>
        /// Inverts the color and returns a new <see cref="Xv2Color"/> instance with the inverted color.
        /// </summary>
        /// <remarks>If the color has HDR values, they will be stripped out before being inverted.</remarks>
        public Xv2Color Invert()
        {
            return new Xv2Color((byte)(255 - R_Preview), (byte)(255 - G_Preview), (byte)(255 - B_Preview), A_Preview);
        }

        public System.Windows.Media.Color ToWpfColor(bool invert = false)
        {
            return invert ? System.Windows.Media.Color.FromRgb((byte)(255 - R_Preview), (byte)(255 - G_Preview), (byte)(255 - B_Preview)) : System.Windows.Media.Color.FromRgb(R_Preview, G_Preview, B_Preview);
        }

        /// <summary>
        /// DeHDRify a color value. Intended only for generating a preview of a color value.
        /// </summary>
        /// <returns>A float value between 0 and 1.0 representing the color value without HDR.</returns>
        internal static float CalculatePreviewValue(float value)
        {
            if (value <= 1f)
                return value;

            float floor = (float)Math.Floor(value);

            if (MathHelpers.FloatEquals(value, floor))
                return 1f;

            return value - floor;
        }

        public static Xv2Color GetAverageColor(IReadOnlyList<Xv2Color> colors)
        {
            if (colors is null)
                throw new ArgumentNullException(nameof(colors));

            if (colors.Count == 0)
                throw new ArgumentException("At least one color is required.", nameof(colors));

            long totalR = 0;
            long totalG = 0;
            long totalB = 0;
            long totalA = 0;

            foreach (var color in colors)
            {
                totalR += color.R_Preview;
                totalG += color.G_Preview;
                totalB += color.B_Preview;
                totalA += color.A_Preview;
            }

            return new Xv2Color((byte)Math.Round((double)totalR / colors.Count), (byte)Math.Round((double)totalG / colors.Count),
                    (byte)Math.Round((double)totalB / colors.Count), (byte)Math.Round((double)totalA / colors.Count));
        }

        #endregion

        #region Operators
        public static bool operator ==(Xv2Color left, Xv2Color right)
        {
            return left.R == right.R && left.G == right.G && left.B == right.B && left.A == right.A;
        }

        public static bool operator !=(Xv2Color left, Xv2Color right)
        {
            return !(left == right);
        }

        public static Xv2Color operator +(Xv2Color left, Xv2Color right)
        {
            return new Xv2Color(left.R + right.R, left.G + right.G, left.B + right.B, left.A + right.A);
        }

        public override bool Equals(object obj)
        {
            return obj is Xv2Color color && color == this;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 1960784236;
                hashCode *= -1521134295 + R.GetHashCode();
                hashCode *= -1521134295 + G.GetHashCode();
                hashCode *= -1521134295 + B.GetHashCode();
                hashCode *= -1521134295 + A.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"R: {R}, G: {G}, B: {B}, A: {A}\nR: {R_Preview}, G: {G_Preview}, B: {B_Preview}, A: {A_Preview}";
        }
        #endregion
    }
}
