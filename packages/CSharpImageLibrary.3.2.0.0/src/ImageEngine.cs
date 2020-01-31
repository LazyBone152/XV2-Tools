using CSharpImageLibrary.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsefulThings;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Determines how Mipmaps are handled.
    /// </summary>
    public enum MipHandling
    {
        /// <summary>
        /// If mips are present, they are used, otherwise regenerated.
        /// </summary>
        [Description("If mips are present, they are used, otherwise regenerated.")]
        Default,

        /// <summary>
        /// Keeps existing mips if existing. Doesn't generate new ones either way.
        /// </summary>
        [Description("Keeps existing mips if existing. Doesn't generate new ones either way.")]
        KeepExisting,

        /// <summary>
        /// Removes old mips and generates new ones.
        /// </summary>
        [Description("Removes old mips and generates new ones.")]
        GenerateNew,

        /// <summary>
        /// Removes all but the top mip. Used for single mip formats.
        /// </summary>
        [Description("Removes all but the top mip. Used for single mip formats.")]
        KeepTopOnly
    }

    /// <summary>
    /// Provides main image functions
    /// </summary>
    public static class ImageEngine
    {
         /// <summary>
        /// True = Windows WIC Codecs are present (8+)
        /// </summary>
        public static bool WindowsWICCodecsAvailable
        {
            get; private set;
        }

        /// <summary>
        /// True = Loading and saving operations are threaded.
        /// </summary>
        public static bool EnableThreading { get; set; } = true;

        /// <summary>
        /// Constructor. Checks WIC status before any other operation.
        /// </summary>
        static ImageEngine()
        {
            WindowsWICCodecsAvailable = WIC_Codecs.WindowsCodecsPresent();
            //WindowsWICCodecsAvailable = false;
        }


        #region Loading
        /// <summary>
        /// Loads image from file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="Format">Detected format.</param>
        /// <param name="enforceResize">True = image resized to desiredMaxDimension if no suitable mipmap.</param>
        /// <param name="header">DDS header of image.</param>
        /// <param name="desiredMaxDimension">Largest dimension to load as.</param>
        /// <param name="mergeAlpha">True = Flattens alpha down, directly affecting RGB.</param>
        /// <returns>List of Mipmaps.</returns>
        internal static List<MipMap> LoadImage(string imagePath, out Format Format, int desiredMaxDimension, bool enforceResize, out DDSGeneral.DDS_HEADER header, bool mergeAlpha)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadImage(fs, out Format, Path.GetExtension(imagePath), desiredMaxDimension, enforceResize, out header, mergeAlpha);
        }


        /// <summary>
        /// Loads image from stream.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="Format">Detected format.</param>
        /// <param name="extension">File extension. Used to determine format more easily.</param>
        /// <param name="mergeAlpha">ONLY valid when enforceResize is true. True = Flattens alpha down, directly affecting RGB.</param>
        /// <param name="enforceResize">True = image resized to desiredMaxDimension if no suitable mipmap.</param>
        /// <param name="header">DDS header of image.</param>
        /// <param name="desiredMaxDimension">Largest dimension to load as. ASSUMES SQUARE.</param>
        /// <returns>List of Mipmaps.</returns>
        internal static List<MipMap> LoadImage(Stream stream, out Format Format, string extension, int desiredMaxDimension, bool enforceResize, out DDSGeneral.DDS_HEADER header, bool mergeAlpha)
        {
            return LoadImage(stream, out Format, extension, desiredMaxDimension, desiredMaxDimension, enforceResize, out header, mergeAlpha);
        }


        /// <summary>
        /// Loads image from stream.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="Format">Detected Format.</param>
        /// <param name="extension">File Extension. Used to determine format more easily.</param>
        /// <param name="maxWidth">Maximum width to allow when loading. Resized if enforceResize = true.</param>
        /// <param name="maxHeight">Maximum height to allow when loading. Resized if enforceResize = true.</param>
        /// <param name="enforceResize">True = Resizes image to match either maxWidth or maxHeight.</param>
        /// <param name="header">DDS header of image.</param>
        /// <param name="mergeAlpha">ONLY valid when enforceResize is true. True = Flattens alpha down, directly affecting RGB.</param>
        /// <returns>List of Mipmaps.</returns>
        internal static List<MipMap> LoadImage(Stream stream, out Format Format, string extension, int maxWidth, int maxHeight, bool enforceResize, out DDSGeneral.DDS_HEADER header, bool mergeAlpha)
        {
            // KFreon: See if image is built-in codec agnostic.
            header = null;
            Format = ImageFormats.ParseFormat(stream, extension, ref header);
            List<MipMap> MipMaps = null;

            switch (Format.SurfaceFormat)
            {
                case ImageEngineFormat.BMP:
                case ImageEngineFormat.JPG:
                case ImageEngineFormat.PNG:
                    MipMaps = WIC_Codecs.LoadWithCodecs(stream, maxWidth, maxHeight, false);
                    break;
                case ImageEngineFormat.DDS_DXT1:
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    if (WindowsWICCodecsAvailable)
                        MipMaps = WIC_Codecs.LoadWithCodecs(stream, maxWidth, maxHeight, true);
                    else
                        MipMaps = DDSGeneral.LoadDDS(stream, header, Format, maxHeight > maxWidth ? maxHeight : maxWidth);
                    break;
                case ImageEngineFormat.DDS_ARGB:
                case ImageEngineFormat.DDS_A8L8:
                case ImageEngineFormat.DDS_RGB:
                case ImageEngineFormat.DDS_ATI1:
                case ImageEngineFormat.DDS_ATI2_3Dc:
                case ImageEngineFormat.DDS_G8_L8:
                case ImageEngineFormat.DDS_V8U8:
                    MipMaps = DDSGeneral.LoadDDS(stream, header, Format, maxHeight > maxWidth ? maxHeight : maxWidth);
                    break;
                case ImageEngineFormat.TGA:
                    var img = new TargaImage(stream);
                    byte[] pixels = UsefulThings.WinForms.Imaging.GetPixelDataFromBitmap(img.Image);
                    WriteableBitmap wbmp = UsefulThings.WPF.Images.CreateWriteableBitmap(pixels, img.Image.Width, img.Image.Height);
                    var mip1 = new MipMap(wbmp);
                    MipMaps = new List<MipMap>() { mip1 };
                    img.Dispose();
                    break;
                default:
                    throw new InvalidDataException("Image format is unknown.");
            }

            if (MipMaps == null || MipMaps.Count == 0)
                throw new InvalidDataException("No mipmaps loaded.");


            // KFreon: No resizing requested
            if (maxHeight == 0 && maxWidth == 0)
                return MipMaps;

            // KFreon: Test if we need to resize
            var top = MipMaps.First();
            if (top.Width == maxWidth || top.Height == maxHeight)
                return MipMaps;

            int max = maxWidth > maxHeight ? maxWidth : maxHeight;

            // KFreon: Attempt to resize
            var sizedMips = MipMaps.Where(m => m.Width > m.Height ? m.Width <= max : m.Height <= max);
            if (sizedMips != null && sizedMips.Any())  // KFreon: If there's already a mip, return that.
                MipMaps = sizedMips.ToList();
            else if (enforceResize)
            {
                // Get top mip and clear others.
                var mip = MipMaps[0];
                MipMaps.Clear();
                MipMap output = null;

                int divisor = mip.Width > mip.Height ? mip.Width / max : mip.Height / max;

                output = Resize(mip, 1f / divisor, mergeAlpha);

                MipMaps.Add(output);
            }
            return MipMaps;
        }

        internal static List<MipMap> LoadImage(byte[] rawDDSData, ImageEngineFormat surfaceFormat, int width, int height, out DDSGeneral.DDS_HEADER header)
        {
            header = DDSGeneral.Build_DDS_Header(1, height, width, surfaceFormat);
            List<MipMap> MipMaps = null;

            // Create new fully formatted DDS i.e. one with a header.
            MemoryStream stream = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(stream);
            DDSGeneral.Write_DDS_Header(header, bw);
            bw.Write(rawDDSData);

            switch (surfaceFormat)
            {
                case ImageEngineFormat.DDS_DXT1:
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    if (WindowsWICCodecsAvailable)
                        MipMaps = WIC_Codecs.LoadWithCodecs(stream, 0, 0, true);
                    else
                        MipMaps = DDSGeneral.LoadDDS(stream, header, new Format(surfaceFormat), 0);
                    break;
                case ImageEngineFormat.DDS_ARGB:
                case ImageEngineFormat.DDS_A8L8:
                case ImageEngineFormat.DDS_RGB:
                case ImageEngineFormat.DDS_ATI1:
                case ImageEngineFormat.DDS_ATI2_3Dc:
                case ImageEngineFormat.DDS_G8_L8:
                case ImageEngineFormat.DDS_V8U8:
                    MipMaps = DDSGeneral.LoadDDS(stream, header, new Format(surfaceFormat), 0);
                    break;
                default:
                    throw new InvalidDataException("Image format is unknown.");
            }
            bw.Dispose(); // Also disposes MemoryStream
            return MipMaps;
        }
        #endregion Loading



        /// <summary>
        /// Save mipmaps as given format to stream.
        /// </summary>
        /// <param name="MipMaps">List of Mips to save.</param>
        /// <param name="format">Desired format.</param>
        /// <param name="destination">Stream to save to.</param>
        /// <param name="mipChoice">Determines how to handle mipmaps.</param>
        /// <param name="maxDimension">Maximum value for either image dimension.</param>
        /// <param name="mergeAlpha">True = alpha flattened down, directly affecting RGB.</param>
        /// <param name="mipToSave">0 based index on which mipmap to make top of saved image.</param>
        /// <returns>True on success.</returns>
        internal static bool Save(List<MipMap> MipMaps, ImageEngineFormat format, Stream destination, MipHandling mipChoice, bool mergeAlpha, int maxDimension = 0, int mipToSave = 0)
        {
            Format temp = new Format(format);
            List<MipMap> newMips = new List<MipMap>(MipMaps);

            if ((temp.IsMippable && mipChoice == MipHandling.GenerateNew) || (temp.IsMippable && newMips.Count == 1 && mipChoice == MipHandling.Default))
                DDSGeneral.BuildMipMaps(newMips, mergeAlpha);

            // KFreon: Resize if asked
            if (maxDimension != 0 && maxDimension < newMips[0].Width && maxDimension < newMips[0].Height) 
            {
                if (!UsefulThings.General.IsPowerOfTwo(maxDimension))
                    throw new ArgumentException($"{nameof(maxDimension)} must be a power of 2. Got {nameof(maxDimension)} = {maxDimension}");


                // KFreon: Check if there's a mipmap suitable, removes all larger mipmaps
                var validMipmap = newMips.Where(img => (img.Width == maxDimension && img.Height <= maxDimension) || (img.Height == maxDimension && img.Width <=maxDimension));  // Check if a mip dimension is maxDimension and that the other dimension is equal or smaller
                if (validMipmap?.Count() != 0)
                {
                    int index = newMips.IndexOf(validMipmap.First());
                    newMips.RemoveRange(0, index);
                }
                else
                {
                    // KFreon: Get the amount the image needs to be scaled. Find largest dimension and get it's scale.
                    double scale = maxDimension * 1f / (newMips[0].Width > newMips[0].Height ? newMips[0].Width: newMips[0].Height);

                    // KFreon: No mip. Resize.
                    newMips[0] = Resize(newMips[0], scale, mergeAlpha);
                }
            }

            // KFreon: Ensure we have a power of two for dimensions
            double fixScale = 0;
            if (!UsefulThings.General.IsPowerOfTwo(newMips[0].Width) || !UsefulThings.General.IsPowerOfTwo(newMips[0].Height))
            {
                int newWidth = UsefulThings.General.RoundToNearestPowerOfTwo(newMips[0].Width);
                int newHeigh = UsefulThings.General.RoundToNearestPowerOfTwo(newMips[0].Height);

                // KFreon: Assuming same scale in both dimensions...
                fixScale = 1.0*newWidth / newMips[0].Width;

                newMips[0] = Resize(newMips[0], fixScale, mergeAlpha);

            }


            if (fixScale != 0 || mipChoice == MipHandling.KeepTopOnly)
                DestroyMipMaps(newMips, mipToSave);

            if (fixScale != 0 && temp.IsMippable && mipChoice != MipHandling.KeepTopOnly)
                DDSGeneral.BuildMipMaps(newMips, mergeAlpha);


            bool result = false;
            if (temp.SurfaceFormat.ToString().Contains("DDS"))
                result = DDSGeneral.Save(newMips, destination, temp);
            else
            {
                // KFreon: Try saving with built in codecs
                var mip = newMips[0];
                if (WindowsWICCodecsAvailable)
                    result = WIC_Codecs.SaveWithCodecs(mip.BaseImage, destination, format);
            }

            if (mipChoice != MipHandling.KeepTopOnly && temp.IsMippable)
            {
                // KFreon: Necessary. Must be how I handle the lowest mip levels. i.e. WRONGLY :(
                // Figure out how big the file should be and make it that size

                int size = 0;
                int width = newMips[0].Width;
                int height = newMips[0].Height;

                int divisor = 1;
                if (temp.IsBlockCompressed)
                    divisor = 4;

                while(width >= 1 && height >= 1)
                {
                    int tempWidth = width;
                    int tempHeight = height;

                    if (temp.IsBlockCompressed)
                    {
                        if (tempWidth < 4)
                            tempWidth = 4;
                        if (tempHeight < 4)
                            tempHeight = 4;
                    }
                    

                    size += tempWidth / divisor * tempHeight / divisor * temp.BlockSize;
                    width /= 2;
                    height /= 2;
                }

                if (size > destination.Length - 128)
                {
                    byte[] blanks = new byte[size - (destination.Length - 128)];
                    destination.Write(blanks, 0, blanks.Length);
                }
            }

            return result;
        }


        /// <summary>
        /// Saves image to byte[].
        /// </summary>
        /// <param name="MipMaps">Mipmaps to save.</param>
        /// <param name="format">Format to save image as.</param>
        /// <param name="generateMips">Determines how to handle mipmaps.</param>
        /// <param name="desiredMaxDimension">Maximum dimension to allow. Resizes if required.</param>
        /// <param name="mipToSave">Mipmap to save. If > 0, all other mipmaps removed, and this mipmap saved.</param>
        /// <param name="mergeAlpha">True = Flattens alpha into RGB.</param>
        /// <returns>Byte[] containing fully formatted image.</returns>
        internal static byte[] Save(List<MipMap> MipMaps, ImageEngineFormat format, MipHandling generateMips, int desiredMaxDimension, int mipToSave, bool mergeAlpha)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Save(MipMaps, format, ms, generateMips, mergeAlpha, desiredMaxDimension, mipToSave);
                return ms.ToArray();
            }
        }

        internal static double ExpectedImageSize(double mipIndex, Format format, int baseWidth, int baseHeight)
        {
            /*
                Mipmapping halves both dimensions per mip down. Dimensions are then divided by 4 if block compressed as a texel is 4x4 pixels.
                e.g. 4096 x 4096 block compressed texture with 8 byte blocks e.g. DXT1
                Sizes of mipmaps:
                    4096 / 4 x 4096 / 4 x 8
                    (4096 / 4 / 2) x (4096 / 4 / 2) x 8
                    (4096 / 4 / 2 / 2) x (4096 / 4 / 2 / 2) x 8

                Pattern: Each dimension divided by 2 per mip size decreased.
                Thus, total is divided by 4.
                    Size of any mip = Sum(1/4^n) x divWidth x divHeight x blockSize,  
                        where n is the desired mip (0 based), 
                        divWidth and divHeight are the block compress adjusted dimensions (uncompressed textures lead to just original dimensions, block compressed are divided by 4)

                Turns out the partial sum of the infinite sum: Sum(1/4^n) = 1/3 x (4 - 4^-n). Who knew right?
            */

            int divisor = 1;
            if (format.IsBlockCompressed)
                divisor = 4;

            double sumPart = mipIndex == -1 ? 0 :
                (1 / 3f) * (4 - Math.Pow(4, -mipIndex));

            double totalSize = 128 + (sumPart * format.BlockSize * (baseWidth / divisor) * (baseHeight / divisor));


            return totalSize;
        }
        

        internal static MipMap Resize(MipMap mipMap, double scale, bool mergeAlpha)
        {
            WriteableBitmap bmp = mipMap.BaseImage;
            int origWidth = bmp.PixelWidth;
            int origHeight = bmp.PixelHeight;
            int origStride = origWidth * 4;
            int newWidth = (int)(origWidth * scale);
            int newHeight = (int)(origHeight * scale);
            int newStride = newWidth * 4;



            WriteableBitmap alpha = new WriteableBitmap(origWidth, origHeight, 96, 96, PixelFormats.Bgr32, null);
            if (!mergeAlpha)
            {
                // Pull out alpha since scaling with alpha doesn't work properly for some reason
                try
                {
                    unsafe
                    {
                        int index = 3;
                        byte* alphaPtr = (byte*)alpha.BackBuffer.ToPointer();
                        byte* mainPtr = (byte*)bmp.BackBuffer.ToPointer();
                        for (int i = 0; i < origWidth * origHeight * 4; i += 4)
                        {
                            // Set all pixels in alpha to value of alpha from original image - otherwise scaling will interpolate colours
                            alphaPtr[i] = mainPtr[index];
                            alphaPtr[i + 1] = mainPtr[index];
                            alphaPtr[i + 2] = mainPtr[index];
                            alphaPtr[i + 3] = mainPtr[index];
                            index += 4;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    throw;
                }
            }
            
            FormatConvertedBitmap main = new FormatConvertedBitmap(bmp, PixelFormats.Bgr32, null, 0);

            

            // Scale RGB
            ScaleTransform scaletransform = new ScaleTransform(scale, scale);
            TransformedBitmap scaledMain = new TransformedBitmap(main, scaletransform);


            // Put alpha back in
            FormatConvertedBitmap newConv = new FormatConvertedBitmap(scaledMain, PixelFormats.Bgra32, null, 0);
            WriteableBitmap resized = new WriteableBitmap(newConv);

            if (!mergeAlpha)
            {
                TransformedBitmap scaledAlpha = new TransformedBitmap(alpha, scaletransform);
                WriteableBitmap newAlpha = new WriteableBitmap(scaledAlpha);

                try
                {
                    unsafe
                    {
                        byte* resizedPtr = (byte*)resized.BackBuffer.ToPointer();
                        byte* alphaPtr = (byte*)newAlpha.BackBuffer.ToPointer();
                        for (int i = 3; i < newStride * newHeight; i += 4)
                            resizedPtr[i] = alphaPtr[i];
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    throw;
                }
            }
            
            

            return new MipMap(resized);
        }


        /// <summary>
        /// Destroys mipmaps. Expects at least one mipmap in given list.
        /// </summary>
        /// <param name="MipMaps">List of Mipmaps.</param>
        /// <returns>Number of mips present.</returns>
        private static int DestroyMipMaps(List<MipMap> MipMaps, int mipToSave)
        {
            MipMaps.RemoveRange(mipToSave + 1, MipMaps.Count - 1);  // +1 because mipToSave is 0 based and we want to keep it
            return 1;
        }

        /// <summary>
        /// Generates a thumbnail image as quickly and efficiently as possible.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="maxHeight">Max height to decode at. 0 means ignored, and aspect respected.</param>
        /// <param name="maxWidth">Max width to decode at. 0 means ignored, and aspect respected.</param>
        /// <param name="mergeAlpha">DXT1 only. True = Flatten alpha into RGB.</param>
        /// <param name="requireTransparency">True = uses PNG compression instead of JPG.</param>
        public static MemoryStream GenerateThumbnailToStream(Stream stream, int maxWidth, int maxHeight, bool mergeAlpha = false, bool requireTransparency = false)
        {
            Format format = new Format();
            DDSGeneral.DDS_HEADER header = null;
            var mipmaps = LoadImage(stream, out format, null, maxWidth, maxHeight, true, out header, mergeAlpha);

            MemoryStream ms = new MemoryStream();
            bool result = Save(mipmaps, requireTransparency ? ImageEngineFormat.PNG : ImageEngineFormat.JPG, ms, MipHandling.KeepTopOnly, mergeAlpha, maxHeight > maxWidth ? maxHeight : maxWidth);
            if (!result)
                ms = null;

            return ms;
        }


        /// <summary>
        /// Generates a thumbnail of image and saves it to a file.
        /// </summary>
        /// <param name="stream">Fully formatted image stream.</param>
        /// <param name="destination">File path to save to.</param>
        /// <param name="maxDimension">Maximum value for either image dimension.</param>
        /// <param name="mergeAlpha">DXT1 only. True = Flatten alpha into RGB.</param>
        /// <returns>True on success.</returns>
        public static bool GenerateThumbnailToFile(Stream stream, string destination, int maxDimension, bool mergeAlpha = false)
        {
            using (ImageEngineImage img = new ImageEngineImage(stream, null, maxDimension, true))
            {
                bool success = false;
                using (FileStream fs = new FileStream(destination, FileMode.Create))
                    success = img.Save(fs, ImageEngineFormat.JPG, MipHandling.KeepTopOnly, mergeAlpha: mergeAlpha, desiredMaxDimension: maxDimension);

                return success;
            }                
        }


        /// <summary>
        /// Parses a string to an ImageEngineFormat.
        /// </summary>
        /// <param name="format">String representation of ImageEngineFormat.</param>
        /// <returns>ImageEngineFormat of format.</returns>
        public static ImageEngineFormat ParseFromString(string format)
        {
            ImageEngineFormat parsedFormat = ImageEngineFormat.Unknown;

            if (format.Contains("dxt1", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT1;
            else if (format.Contains("dxt2", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT2;
            else if (format.Contains("dxt3", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT3;
            else if (format.Contains("dxt4", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT4;
            else if (format.Contains("dxt5", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT5;
            else if (format.Contains("bmp", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.BMP;
            else if (format.Contains("argb", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_ARGB;
            else if (format.Contains("ati1", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_ATI1;
            else if (format.Contains("ati2", StringComparison.OrdinalIgnoreCase) || format.Contains("3dc", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_ATI2_3Dc;
            else if (format.Contains("l8", StringComparison.OrdinalIgnoreCase) || format.Contains("g8", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_G8_L8;
            else if (format.Contains("v8u8", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_V8U8;
            else if (format.Contains("jpg", StringComparison.OrdinalIgnoreCase) || format.Contains("jpeg", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.JPG;
            else if (format.Contains("png", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.PNG;


            return parsedFormat;
        }
    }
}
