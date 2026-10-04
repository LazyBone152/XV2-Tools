using LB_Common.Numbers;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Xv2CoreLib.Color
{
    /// <summary>
    /// Accumulates RGB channel totals and a count of colors to compute an average color.
    /// </summary>
    /// <remarks>Call <see cref="AddColor"/> to add a color. <see cref="AverageColor"/> returns the computed average color from the accumulated values.</remarks>
    public sealed class Xv2ColorAccumulator
    {
        public long BlueSum { get; private set; }
        public long GreenSum { get; private set; }
        public long RedSum { get; private set; }
        public long NumColors { get; private set; }

        /// <summary>
        /// Returns the average color computed from the accumulated RGB channel totals and the number of colors added. If no colors have been added, returns <see cref="Xv2Color.Transparent"/>.
        /// </summary>
        public Xv2Color AverageColor => ToXv2Color();

        /// <summary>
        /// Adds the RGB channel values of the specified <see cref="Xv2Color"/> to the accumulated totals.
        /// </summary>
        public void AddColor(Xv2Color color)
        {
            RedSum += color.R_Preview;
            GreenSum += color.G_Preview;
            BlueSum += color.B_Preview;
            NumColors++;
        }

        public void AddColor(CustomColor color)
        {
            RedSum += (long)(color.R * 255);
            GreenSum += (long)(color.G * 255);
            BlueSum += (long)(color.B * 255);
            NumColors++;
        }

        public void AddColor(System.Windows.Media.Color color)
        {
            RedSum += color.R;
            GreenSum += color.G;
            BlueSum += color.B;
            NumColors++;
        }

        public void AddColor(float r, float g, float b)
        {
            RedSum += (long)(r * 255);
            GreenSum += (long)(g * 255);
            BlueSum += (long)(b * 255);
            NumColors++;
        }

        public void AddColor(byte r, byte g, byte b)
        {
            RedSum += r;
            GreenSum += g;
            BlueSum += b;
            NumColors++;
        }

        public void AddBitmap(WriteableBitmap bitmap)
        {
            if (bitmap is null) throw new ArgumentNullException(nameof(bitmap));

            //Borrowed from WriteableBitmapEditOp
            int bitsPerPixel = bitmap.Format.BitsPerPixel;
            int _pixelSize = (bitsPerPixel + 7) / 8;
            int size = _pixelSize * bitmap.PixelWidth * bitmap.PixelHeight;
            byte[] pixels = new byte[size];

            bitmap.GetPixels(pixels);

            AddBitmap(pixels, bitmap.Format, bitmap.PixelHeight, bitmap.PixelWidth);
        }

        public void AddBitmap(byte[] pixels, PixelFormat pixelFormat, int height, int width)
        {
            if (pixels is null) throw new ArgumentNullException(nameof(pixels));

            bool _isPremultiplied = pixelFormat == PixelFormats.Pbgra32;
            int _pixelSize = (pixelFormat.BitsPerPixel + 7) / 8;
            bool hasAlpha = _pixelSize >= 4;
            int size = _pixelSize * width * height;

            if (pixels.Length < size)
                throw new ArgumentException($"Xv2ColorAccumulator.AddBitmap: The input pixel array is too small for the bitmap size.");

            int xStrideBase = width >= 1024 ? 128 : 64;
            int yStrideBase = height >= 1024 ? 128 : 64;
            int xStride = Math.Max(1, width / xStrideBase);
            int yStride = Math.Max(1, height / yStrideBase);

            for (int x = 0; x < width; x += xStride)
            {
                for (int y = 0; y < height; y += yStride)
                {
                    int i = (y * width + x) * _pixelSize;

                    byte b = pixels[i];
                    byte g = pixels[i + 1];
                    byte r = pixels[i + 2];
                    byte a = hasAlpha ? pixels[i + 3] : byte.MaxValue;

                    int ur = r;
                    int ug = g;
                    int ub = b;

                    if (_isPremultiplied && a != 0)
                    {
                        ur = (r * 255 + (a / 2)) / a;
                        ug = (g * 255 + (a / 2)) / a;
                        ub = (b * 255 + (a / 2)) / a;
                    }

                    bool isSame = ur == ug && ug == ub;

                    if (!isSame && a > 0)
                    {
                        RedSum += ur;
                        GreenSum += ug;
                        BlueSum += ub;
                        NumColors++;
                    }
                }
            }

        }

        public void Clear()
        {
            BlueSum = 0;
            GreenSum = 0;
            RedSum = 0;
            NumColors = 0;
        }

        private Xv2Color ToXv2Color()
        {
            if (NumColors == 0)
                return Xv2Color.Transparent;

            return new Xv2Color((byte)(RedSum / NumColors), (byte)(GreenSum / NumColors), (byte)(BlueSum / NumColors), (byte)255);
        }

        public CustomColor ToCustomColor()
        {
            if (NumColors == 0)
                return new CustomColor(0,0,0,1);

            return new CustomColor((RedSum / NumColors) / 255f, (GreenSum / NumColors) / 255f, (BlueSum / NumColors) / 255f, 1f);
        }
    }
}
