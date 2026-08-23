using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Xv2CoreLib.HslColor;

namespace Xv2CoreLib.Resource.Image
{
    /// <summary>
    /// Encapsulates a WriteableBitmap edit operation, allowing for hue adjustments and hue setting on a source bitmap while preserving the original pixel data. The source bitmap is not modified directly; instead, an output bitmap is created to hold the edited result. 
    /// This class will use multiple threads to process the image in parallel, improving performance on larger images. The number of threads used is determined by the number of available threads and the size of the image.
    /// </summary>
    public class WriteableBitmapEditOperation
    {
        private readonly WriteableBitmap _sourceBitmap;
        private readonly WriteableBitmap _outputBitmap;
        private readonly byte[] _workPixels;
        private readonly int _pixelSize;
        private readonly bool hasAlpha;
        private readonly bool _isPremultiplied;
        private readonly int _width;
        private readonly int _height;

        public WriteableBitmap SourceBitmap => _sourceBitmap;
        public WriteableBitmap OutputBitmap => _outputBitmap;

        private readonly int _numThreads;
        private readonly Task[] _tasks;

        public WriteableBitmapEditOperation(WriteableBitmap sourceBitmap, bool useMultiThreading = true)
        {
            _sourceBitmap = sourceBitmap;
            _outputBitmap = new WriteableBitmap(_sourceBitmap.PixelWidth, _sourceBitmap.PixelHeight, _sourceBitmap.DpiX, _sourceBitmap.DpiY, _sourceBitmap.Format, null);
            _width = _sourceBitmap.PixelWidth;
            _height = _sourceBitmap.PixelHeight;
            _isPremultiplied = _sourceBitmap.Format == System.Windows.Media.PixelFormats.Pbgra32;

            //Determine bytes-per-pixel from format and compute stride (scanline size), accounting for 4-byte alignment.
            int bitsPerPixel = _sourceBitmap.Format.BitsPerPixel;
            _pixelSize = (bitsPerPixel + 7) / 8;
            hasAlpha = _pixelSize >= 4;

            //Use tightly-packed pixel buffer (no per-scanline padding) to match GetPixels/SetPixels helpers
            int size = _pixelSize * _sourceBitmap.PixelWidth * _sourceBitmap.PixelHeight;
            _workPixels = new byte[size];

            //Create threads. Ensure that the number of threads does not exceed the width or height of the source bitmap to avoid idle threads
            _numThreads = Math.Min(Math.Max(1, Environment.ProcessorCount), _sourceBitmap.PixelHeight);
            _tasks = _numThreads > 1 && useMultiThreading ? new Task[_numThreads] : null;
        }

        /// <summary>
        /// Applies a specific hue to every pixel in the bitmap. This method performs the operation using a single thread.
        /// </summary>
        /// <param name="hue">A hue value (0-360)</param>
        public void ApplyHueSet(int hue)
        {
            ResetWorkPixels();
            ApplyHueAdjust(true, hue, 0, 0);
            _outputBitmap.SetPixels(_workPixels);
        }

        /// <summary>
        /// Applies a specific hue, saturation and lightness adjustment to every pixel in the bitmap. This method performs the operation using a single thread.
        /// </summary>
        /// <param name="hue">A hue value (0-360)</param>
        /// <param name="saturation">A saturation value (-1.0 to 1.0)</param>
        /// <param name="lightness">A lightness value (-1.0 to 1.0)</param>
        public void ApplyHueAdjust(int hue, double saturation, double lightness)
        {
            ResetWorkPixels();
            ApplyHueAdjust(false, hue, saturation, lightness);
            _outputBitmap.SetPixels(_workPixels);
        }

        /// <summary>
        /// Applies a specific hue to every pixel in the bitmap.
        /// </summary>
        /// <param name="hue">A hue value (0-360)</param>
        public async Task AsyncApplyHueSet(int hue)
        {
            ResetWorkPixels();

            if(_numThreads > 1)
            {
                for (int i = 0; i < _numThreads; i++)
                {
                    int threadIndex = i;
                    _tasks[threadIndex] = Task.Run(() => ApplyHueAdjust(true, hue, 0, 0, threadIndex, _numThreads));
                }

                await Task.WhenAll(_tasks);
            }
            else
            {
                ApplyHueAdjust(true, hue, 0, 0);
            }

            _outputBitmap.SetPixels(_workPixels);
        }

        /// <summary>
        /// Applies a specific hue, saturation and lightness adjustment to every pixel in the bitmap.
        /// </summary>
        /// <param name="hue">A hue value (0-360)</param>
        /// <param name="saturation">A saturation value (-1.0 to 1.0)</param>
        /// <param name="lightness">A lightness value (-1.0 to 1.0)</param>
        public async Task AsyncApplyHueAdjust(int hue, double saturation, double lightness)
        {
            ResetWorkPixels();

            if(_numThreads > 1)
            {
                for (int i = 0; i < _numThreads; i++)
                {
                    int threadIndex = i;
                    _tasks[threadIndex] = Task.Run(() => ApplyHueAdjust(false, hue, saturation, lightness, threadIndex, _numThreads));
                }

                await Task.WhenAll(_tasks);
            }
            else
            {
                ApplyHueAdjust(false, hue, saturation, lightness);
            }

            _outputBitmap.SetPixels(_workPixels);
        }

        private void ApplyHueAdjust(bool isHueSet, int hue, double saturation, double lightness, int offset = 0, int increment = 1)
        {
            if (_pixelSize < 3 || increment < 1 || offset < 0) return;
            saturation += 1.0;
            lightness += 1.0;

            for (int y = offset; y < _height; y += increment)
            {
                for (int x = 0; x < _width; x++)
                {
                    int i = (y * _width + x) * _pixelSize;

                    byte b = _workPixels[i];
                    byte g = _workPixels[i + 1];
                    byte r = _workPixels[i + 2];
                    byte a = hasAlpha ? _workPixels[i + 3] : byte.MaxValue;

                    int ur = r;
                    int ug = g;
                    int ub = b;

                    // If pixels are premultiplied we need to un-premultiply before color operations
                    if (_isPremultiplied && a != 0)
                    {
                        ur = (r * 255 + (a / 2)) / a;
                        ug = (g * 255 + (a / 2)) / a;
                        ub = (b * 255 + (a / 2)) / a;
                    }
                    
                    var color = isHueSet ? ColorEx.HueSet((byte)ur, (byte)ug, (byte)ub, a, hue) : ColorEx.HueAdjust((byte)ur, (byte)ug, (byte)ub, a, hue, saturation, lightness);

                    // If original format was premultiplied, re-multiply channels by alpha
                    if (_isPremultiplied && a != 0)
                    {
                        _workPixels[i] = (byte)((color.B * a) / 255);
                        _workPixels[i + 1] = (byte)((color.G * a) / 255);
                        _workPixels[i + 2] = (byte)((color.R * a) / 255);
                    }
                    else
                    {
                        _workPixels[i] = color.B;
                        _workPixels[i + 1] = color.G;
                        _workPixels[i + 2] = color.R;
                    }

                }
            }

        }

        private void ResetWorkPixels()
        {
            _sourceBitmap.GetPixels(_workPixels);
        }
    }
}