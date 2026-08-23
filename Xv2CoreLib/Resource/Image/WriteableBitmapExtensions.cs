using System.IO;
using Xv2CoreLib.HslColor;

namespace System.Windows.Media.Imaging
{
    public static class WriteableBitmapExtentions
    {
        public static WriteableBitmap HueAdjust(this WriteableBitmap bitmap, int hue, double saturation, double lightness)
        {
            if (hue == 0) return bitmap;

            bitmap.ForEach((x, y, color) => ColorEx.HueAdjust(color.R, color.G, color.B, color.A, hue, saturation, lightness));

            return bitmap;
        }

        public static WriteableBitmap HueSet(this WriteableBitmap bitmap, int hue)
        {
            bitmap.ForEach((x, y, color) => ColorEx.HueSet(color.R, color.G, color.B, color.A, hue));

            return bitmap;
        }

        // Save the WriteableBitmap into a PNG file and writes it to disk.
        public static void Save(this WriteableBitmap wbitmap, string filename)
        {
            // Save the bitmap into a file.
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbitmap));
                encoder.Save(stream);
            }
        }

        // Save the WriteableBitmap into a PNG file and writes it to a MemoryStream.
        public static void Save(this WriteableBitmap wbitmap, MemoryStream stream)
        {
            // Save the bitmap into a file.
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(wbitmap));
            encoder.Save(stream);
        }

        /// <summary>
        /// Gets pixels of image as byte[].
        /// </summary>
        /// <param name="bmp">Image to extract pixels from.</param>
        /// <returns>Pixels of image.</returns>
        public static byte[] GetPixels(this WriteableBitmap bmp, byte[] pixels)
        {
            bool hasAlpha = bmp.Format.ToString().ToLower().Contains("a");
            int size = (int)((hasAlpha ? 4 : 3) * bmp.PixelWidth * bmp.PixelHeight);

            if (pixels.Length != size)
                throw new ArgumentException($"Pixel array size does not match the bitmap size. Expected: {size}, Actual: {pixels.Length}");

            int stride = (int)bmp.PixelWidth * (bmp.Format.BitsPerPixel / 8);
            bmp.CopyPixels(pixels, stride, 0);
            return pixels;
        }

        /// <summary>
        /// Gets pixels of image as byte[].
        /// </summary>
        /// <param name="bmp">Image to extract pixels from.</param>
        /// <returns>Pixels of image.</returns>
        public static byte[] GetPixels(this WriteableBitmap bmp)
        {
            bool hasAlpha = bmp.Format.ToString().ToLower().Contains("a");

            int size = (int)((hasAlpha ? 4 : 3) * bmp.PixelWidth * bmp.PixelHeight);
            byte[] pixels = new byte[size];
            int stride = (int)bmp.PixelWidth * (bmp.Format.BitsPerPixel / 8);
            bmp.CopyPixels(pixels, stride, 0);
            return pixels;
        }

        public static void SetPixels(this WriteableBitmap bmp, byte[] pixels)
        {
            bool hasAlpha = bmp.Format.ToString().ToLower().Contains("a");
            int size = (int)((hasAlpha ? 4 : 3) * bmp.PixelWidth * bmp.PixelHeight);

            if(size != pixels.Length)
            {
                throw new ArgumentException("Pixel array size does not match the bitmap size.");
            }

            int stride = bmp.PixelWidth * (bmp.Format.BitsPerPixel / 8);
            bmp.WritePixels(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight), pixels, stride, 0);
        }
    }
}
