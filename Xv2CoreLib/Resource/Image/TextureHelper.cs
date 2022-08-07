using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pfim;
using CSharpImageLibrary;
using System.Drawing;
using System.Windows.Forms;
using System.Text;

namespace Xv2CoreLib.Resource.Image
{
    /// <summary>
    /// Procides functions for loading and saving textures (dds/png).
    /// </summary>
    public class TextureHelper
    {
        //Two libraries are used for texture loading/saving:
        //Pfim: DDS Loading. This is pretty fast but unfortunately is just a decoding library, so something else is needed for saving.
        //CSharpImageLibrary: Saving and a fallback for loading. It has worse DDS support than Pfim, but supports other formats such as png, tiff, bmp...
        //BC6/BC7 DDS files can be loaded, but they can't be saved (not supported by CSharpImageLibrary). So these are currently converted to BC3.


        #region LoadSave
        public static byte[] SaveToBytes(WriteableBitmap bitmap, ImageEngineFormat format)
        {
            ImageEngineImage newImage;
            byte[] data;

            using (MemoryStream image = new MemoryStream())
            {
                bitmap.Save(image);
                newImage = new ImageEngineImage(image);
                data = newImage.Save(format, MipHandling.KeepTopOnly, 0, 0, false);
            }

            return data;
        }

        /// <summary>
        /// Load a texture as a <see cref="WriteableBitmap"/>. Supports DDS and PNG.
        /// </summary>
        public static WriteableBitmap GetWpfBitmap(byte[] bytes, out ImageEngineFormat format)
        {
            if (bytes == null || bytes?.Length < 16)
            {
                format = 0;
                return null;
            }

            //Primary DDS Load using Pfim:

#if !DEBUG
            try
#endif
            {
                if (BitConverter.ToInt32(bytes, 0) == EMB_CLASS.EmbEntry.DDS_SIGNATURE)
                {
                    Dds image;
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        image = Dds.Create(ms, new PfimConfig());
                    }

                    format = GetFormat(image);

                    return new WriteableBitmap(GetWpfBitmap(image));
                }
            }
#if !DEBUG
            catch { }
#endif

            //If not a DDS, or it fails for some reason, then attempt a load with CSharpImageLibrary:

            WriteableBitmap bitmap;

            using(var imageSource = new ImageEngineImage(bytes))
            {
                bitmap = new WriteableBitmap(imageSource.GetWPFBitmap());

                if (imageSource.NumMipMaps > 1)
                    throw new InvalidDataException("More than 1 mipmaps found.");

                if(imageSource != null)
                {
                    format = imageSource.Format.SurfaceFormat;
                }
                else
                {
                    format = 0;
                }
            }

            return bitmap;
        }

        private static BitmapSource GetWpfBitmap(Dds image)
        {
            return BitmapSource.Create(image.Width, image.Height, 96.0, 96.0, GetPixelFormat(image), null, image.Data, image.Stride);
        }


        /// <summary>
        /// Load a texture as a <see cref="Bitmap"/>. Supports DDS and PNG.
        /// </summary>
        public static Bitmap GetGDIBitmap(byte[] bytes)
        {
            if (bytes == null || bytes?.Length < 16)
            {
                return null;
            }

            Bitmap bitmap;

            using (var imageSource = new ImageEngineImage(bytes))
            {
                bitmap = new Bitmap(imageSource.GetGDIBitmap(false, false));

                if (imageSource.NumMipMaps > 1)
                    throw new InvalidDataException("More than 1 mipmaps found.");

            }

            return bitmap;
        }

        public static byte[] SaveToBytes(Bitmap bitmap)
        {
            ImageEngineImage newImage;
            byte[] data;

            using (MemoryStream image = new MemoryStream())
            {
                bitmap.Save(image, System.Drawing.Imaging.ImageFormat.Png);
                newImage = new ImageEngineImage(image);
                data = newImage.Save(ImageEngineFormat.PNG, MipHandling.KeepTopOnly, 0, 0, false);
            }

            return data;
        }
        #endregion

        #region FormatHelper
        private static PixelFormat GetPixelFormat(IImage image)
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
    
        private static ImageEngineFormat GetFormat(Dds dds)
        {
            switch (dds.Header.PixelFormat.FourCC)
            {
                case CompressionAlgorithm.ATI1:
                case CompressionAlgorithm.ATI2:
                case CompressionAlgorithm.D3DFMT_DXT1:
                case CompressionAlgorithm.D3DFMT_DXT2:
                case CompressionAlgorithm.D3DFMT_DXT3:
                case CompressionAlgorithm.D3DFMT_DXT4:
                case CompressionAlgorithm.D3DFMT_DXT5:
                case CompressionAlgorithm.BC4S:
                case CompressionAlgorithm.BC4U:
                case CompressionAlgorithm.BC5S:
                case CompressionAlgorithm.BC5U:
                    return (ImageEngineFormat)dds.Header.PixelFormat.FourCC;
            }

            if (dds.Header10 != null)
            {
                switch (dds.Header10.DxgiFormat)
                {
                    case DxgiFormat.BC1_TYPELESS:
                    case DxgiFormat.BC1_UNORM:
                    case DxgiFormat.BC1_UNORM_SRGB:
                        return ImageEngineFormat.DDS_DXT1;
                    case DxgiFormat.BC2_TYPELESS:
                    case DxgiFormat.BC2_UNORM:
                    case DxgiFormat.BC2_UNORM_SRGB:
                        return ImageEngineFormat.DDS_DXT3;
                    case DxgiFormat.BC3_TYPELESS:
                    case DxgiFormat.BC3_UNORM:
                    case DxgiFormat.BC3_UNORM_SRGB:
                        return ImageEngineFormat.DDS_DXT5;
                    case DxgiFormat.BC4_SNORM:
                    case DxgiFormat.BC4_UNORM:
                    case DxgiFormat.BC4_TYPELESS:
                        return ImageEngineFormat.DDS_ATI1;
                    case DxgiFormat.BC5_SNORM:
                    case DxgiFormat.BC5_UNORM:
                    case DxgiFormat.BC5_TYPELESS:
                        return ImageEngineFormat.DDS_ATI2_3Dc;
                }
            }

            //If format not any of the above, then default to BC3
            return ImageEngineFormat.DDS_DXT5;
        }
        #endregion

        private static Font[] Fonts = null;
        private static SizeF MaxStageSelectorTextSize = new SizeF(325f, 35f);

        public static byte[] WriteStageNames(byte[] textureBytes, string[] names)
        {
            
            //Create fonts if first time this method has been called
            if(Fonts == null)
            {
                Fonts = new Font[10];

                for(int i = 0; i < 10; i++)
                {
                    Fonts[i] = new Font("Calibri", 20 - i);
                }
            }

            //Pad names to always have 21 characters, adding empty spaces in front as required
            for (int i = 0; i < names.Length; i++) 
            {
                int padding = 21 - names[i].Length;

                if (padding <= 0) continue;

                StringBuilder strBuilder = new StringBuilder();

                for(int a = 0; a < padding; a++)
                {
                    strBuilder.Append(" ");
                }

                strBuilder.Append(names[i]);
                names[i] = strBuilder.ToString();
            }

            Bitmap texture = GetGDIBitmap(textureBytes);

            using (Graphics graphics = Graphics.FromImage(texture))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                for (int i = 0; i < 4; i++)
                {
                    int x = 0, y = 0;

                    switch (i)
                    {
                        case 0:
                            x = 546;
                            y = 116;
                            break;
                        case 1:
                            x = 298;
                            y = 51;
                            break;
                        case 2:
                            x = 92;
                            y = 116;
                            break;
                        case 3:
                            x = 285;
                            y = 181;
                            break;
                    }

                    y -= 24;

                    string name = i < names.Length ? names[i] : "-----------------";

                    //Auto-adjust font size based on length
                    int fontIdx = 0;
                    for(int a = 0; a < 10; a++)
                    {
                        SizeF size = graphics.MeasureString(name, Fonts[a]);
                        if (size.Width < MaxStageSelectorTextSize.Width) break;

                        fontIdx = a;
                    }

                    graphics.DrawString(name, Fonts[fontIdx], System.Drawing.Brushes.White, new PointF(x, y + fontIdx));
                }
            }

            return SaveToBytes(texture);

            //byte[] newTexture = SaveToBytes(texture);
            //File.WriteAllBytes("test.png", newTexture);
            //return null;
        }

    }

}
