using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pfim;

namespace Xv2CoreLib.Resource.Image
{
    public class WpfBitmapConvert
    {
        /*
        private static List<GCHandle> handles = new List<GCHandle>();

        private static IEnumerable<Image> WpfImage(IImage image)
        {
            var pinnedArray = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            var addr = pinnedArray.AddrOfPinnedObject();
            var bsource = BitmapSource.Create(image.Width, image.Height, 96.0, 96.0,
                PixelFormat(image), null, addr, image.DataLen, image.Stride);

            handles.Add(pinnedArray);
            yield return new Image
            {
                Source = bsource,
                Width = image.Width,
                Height = image.Height,
                MaxHeight = image.Height,
                MaxWidth = image.Width,
                Margin = new Thickness(4)
            };

            foreach (var mip in image.MipMaps)
            {
                var mipAddr = addr + mip.DataOffset;
                var mipSource = BitmapSource.Create(mip.Width, mip.Height, 96.0, 96.0,
                    PixelFormat(image), null, mipAddr, mip.DataLen, mip.Stride);
                yield return new Image
                {
                    Source = mipSource,
                    Width = mip.Width,
                    Height = mip.Height,
                    MaxHeight = mip.Height,
                    MaxWidth = mip.Width,
                    Margin = new Thickness(4)
                };
            }
        }

        private static PixelFormat PixelFormat(IImage image)
        {
            switch (image.Format)
            {
                case ImageFormat.Rgb24:
                    return PixelFormats.Bgr24;
                case ImageFormat.Rgba32:
                    return PixelFormats.Bgra32;
                case ImageFormat.Rgb8:
                    return PixelFormats.Gray8;
                case ImageFormat.R5g5b5a1:
                case ImageFormat.R5g5b5:
                    return PixelFormats.Bgr555;
                case ImageFormat.R5g6b5:
                    return PixelFormats.Bgr565;
                default:
                    throw new Exception($"Unable to convert {image.Format} to WPF PixelFormat");
            }
        }
        */
    }
}
