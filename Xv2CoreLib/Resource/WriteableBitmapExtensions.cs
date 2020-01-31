using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace System.Windows.Media.Imaging
{
    public static class WriteableBitmapExtentions
    {
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
    }
}
