using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;
using Microsoft.IO;
using System.IO;
using System.Windows.Media.Imaging;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Represents a mipmap of an image.
    /// </summary>
    public class MipMap
    {
        /// <summary>
        /// Pixels in bitmap image.
        /// </summary>
        public WriteableBitmap BaseImage
        {
            get; set;
        }

        /// <summary>
        /// Mipmap width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Mipmap height.
        /// </summary>
        public int Height { get; set; }



        public MipMap(BitmapSource baseimage)
        {
            BaseImage = new WriteableBitmap(baseimage);
            BaseImage.Freeze();
            Width = BaseImage.PixelWidth;
            Height = BaseImage.PixelHeight;
        }
    }
}
