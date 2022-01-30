using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pfim;
using CSharpImageLibrary;

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

    }

}
