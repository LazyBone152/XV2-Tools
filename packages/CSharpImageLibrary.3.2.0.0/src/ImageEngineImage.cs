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

namespace CSharpImageLibrary
{
    /// <summary>
    /// Represents an image. Can use Windows codecs if available.
    /// </summary>
    public class ImageEngineImage : IDisposable
    {
        enum DDSdwFlags
        {
            DDSD_CAPS = 0x1,            // Required
            DDSD_HEIGHT = 0x2,          // Required
            DDSD_WIDTH = 0x4,           // Required
            DDSD_PITCH = 0x8,           // Required when Pitch is specified for uncompressed textures
            DDSD_PIXELFORMAT = 0x1000,  // Required in all DDS
            DDSD_MIPMAPCOUNT = 0x20000, // Required for a Mipmapped texture
            DDSD_LINEARSIZE = 0x80000,  // Required when Pitch is specified
            DDSD_DEPTH = 0x800000       // Required in Depth texture (Volume)
        }

        enum DDSdwCaps
        {
            DDSCAPS_COMPLEX = 0x8,      // Optional, must be specified on image that has more than one surface. (mipmap, cube, volume)
            DDSCAPS_MIPMAP = 0x400000,  // Optional, should be set for mipmapped image
            DDSCAPS_TEXTURE = 0x1000    // Required
        }

        enum DDS_PFdwFlags
        {
            DDPF_ALPHAPIXELS = 0x1,     // Texture has alpha - dwRGBAlphaBitMask has a value
            DDPF_ALPHA = 0x2,           // Older flag indicating alpha channel in uncompressed data. dwRGBBitCount has alpha channel bitcount, dwABitMask has valid data.
            DDPF_FOURCC = 0x4,          // Contains compressed RGB. dwFourCC has a value
            DDPF_RGB = 0x40,            // Contains uncompressed RGB. dwRGBBitCount and RGB bitmasks have a value
            DDPF_YUV = 0x200,           // Older flag indicating things set as YUV
            DDPF_LUMINANCE = 0x20000    // Older flag for single channel uncompressed data
        }

        string EnumFlagStringify(Type enumType)
        {
            string flags = "";
            if (header != null)
            {
                string[] names = Enum.GetNames(enumType);
                int[] values = (int[])Enum.GetValues(enumType);
                for (int i = 0; i < names.Length; i++)
                {
                    if ((header.dwFlags & values[i]) != 0)
                        flags += $"[{names[i]}] ";
                }
            }
            return flags;
        }

        /// <summary>
        /// DDS header if existing.
        /// </summary>
        public DDSGeneral.DDS_HEADER header { get; set; }
        
        public string HeaderdwFlags
        {
            get
            {
                return EnumFlagStringify(typeof(DDSdwFlags));
            }
        }

        public string HeaderdwCaps
        {
            get
            {
                return EnumFlagStringify(typeof(DDSdwCaps));
            }
        }

        public string HeaderPFdwFlags
        {
            get
            {
                return EnumFlagStringify(typeof(DDS_PFdwFlags));
            }
        }

        #region Properties
        /// <summary>
        /// Width of image.
        /// </summary>
        public int Width
        {
            get
            {
                return MipMaps[0].Width;
            }
        }

        /// <summary>
        /// Height of image.
        /// </summary>
        public int Height
        {
            get
            {
                return MipMaps[0].Height;
            }
        }

        /// <summary>
        /// Number of mipmaps present.
        /// </summary>
        public int NumMipMaps
        {
            get
            {
                return MipMaps.Count;
            }
        }

        /// <summary>
        /// Format of image and whether it's mippable.
        /// </summary>
        public Format Format { get; private set; }

        
        /// <summary>
        /// List of mipmaps. Single level images only have one mipmap.
        /// </summary>
        public List<MipMap> MipMaps { get; private set; }

        /// <summary>
        /// Path to file. Null if no file e.g. thumbnail from memory.
        /// </summary>
        public string FilePath { get; private set; }
        #endregion Properties

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"File Path: {this.FilePath}");
            sb.AppendLine($"Format: {this.Format.ToString()}");
            sb.AppendLine($"Width x Height: {this.Width}x{this.Height}");
            sb.AppendLine($"Num Mips: {this.NumMipMaps}");
            sb.AppendLine($"Header: {this.header.ToString()}");

            return sb.ToString();
        }

        /// <summary>
        /// Creates a new ImageEngineImage from file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        public ImageEngineImage(string imagePath)
        {
            LoadFromFile(imagePath);
        }


        /// <summary>
        /// Creates new ImageEngineImage from stream.
        /// Does NOT require that stream remains alive.
        /// </summary>
        /// <param name="stream">Image to load.</param>
        /// <param name="extension">Extension of original file.</param>
        public ImageEngineImage(Stream stream, string extension = null)
        {
            LoadFromStream(stream, extension);
        }


        /// <summary>
        /// Loads an image from a file and scales (aspect safe) to a maximum size.
        /// e.g. 1024x512, desiredMaxDimension = 128 ===> Image is scaled to 128x64.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="desiredMaxDimension">Max dimension to save.</param>
        /// <param name="enforceResize">True = forcibly resizes image. False = attempts to find a suitably sized mipmap, but doesn't resize if none found.</param>
        public ImageEngineImage(string imagePath, int desiredMaxDimension, bool enforceResize)
        {
            LoadFromFile(imagePath, desiredMaxDimension, enforceResize);
        }

        /// <summary>
        /// Loads an image from a stream and scales (aspect safe) to a maximum size. Does NOT require that stream remains alive.
        /// e.g. 1024x512, desiredMaxDimension = 128 ===> Image is scaled to 128x64.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="extension">File extension of original image.</param>
        /// <param name="desiredMaxDimension">Maximum dimension.</param>
        /// <param name="enforceResize">True = forcibly resizes image. False = attempts to find a suitably sized mipmap, but doesn't resize if none found.</param>
        public ImageEngineImage(Stream stream, string extension, int desiredMaxDimension, bool enforceResize)
        {
            LoadFromStream(stream, extension, desiredMaxDimension, enforceResize);
        }


        /// <summary>
        /// Loads an image from a byte array.
        /// </summary>
        /// <param name="imageFileData">Fully formatted image file data</param>
        public ImageEngineImage(byte[] imageFileData)
        {
            using (MemoryStream ms = new MemoryStream(imageFileData))
                LoadFromStream(ms);
        }


        /// <summary>
        /// Loads an image from a byte array and scales (aspect safe) to a maximum size.
        /// e.g. 1024x512, desiredMaxDimension = 128 ===> Image is scaled to 128x64.
        /// </summary>
        /// <param name="imageFileData">Full image file data.</param>
        /// <param name="desiredMaxDimension">Maximum dimension.</param>
        /// <param name="enforceResize">True = forcibly resizes image. False = attempts to find a suitably sized mipmap, but doesn't resize if none found.</param>
        /// <param name="mergeAlpha">ONLY valid when enforeResize = true. True = flattens alpha, directly affecting RGB.</param>
        public ImageEngineImage(byte[] imageFileData, int desiredMaxDimension, bool enforceResize, bool mergeAlpha = false)
        {
            using (MemoryStream ms = new MemoryStream(imageFileData))
                LoadFromStream(ms, desiredMaxDimension: desiredMaxDimension);
        }


        /// <summary>
        /// Loads DDS that has no header - primarily for ME3Explorer. DDS data is standard, just without a header.
        /// ASSUMES VALID DDS DATA. Also, single mipmap only.
        /// </summary>
        /// <param name="rawDDSData">Standard DDS data but lacking header.</param>
        /// <param name="surfaceFormat">Surface format of DDS.</param>
        /// <param name="width">Width of image.</param>
        /// <param name="height">Height of image.</param>
        public ImageEngineImage(byte[] rawDDSData, ImageEngineFormat surfaceFormat, int width, int height)
        {
            Format = new Format(surfaceFormat);
            DDSGeneral.DDS_HEADER tempHeader = null;
            MipMaps = ImageEngine.LoadImage(rawDDSData, surfaceFormat, width, height, out tempHeader);
            header = tempHeader;
        }


        /// <summary>
        /// Builds a DDS image from an existing mipmap.
        /// </summary>
        /// <param name="mip">Mip to base image on.</param>
        /// <param name="DDSFormat">Format of mipmap.</param>
        public ImageEngineImage(MipMap mip, ImageEngineFormat DDSFormat)
        {
            Format = new Format(DDSFormat);
            MipMaps = new List<MipMap>() { mip };
            header = DDSGeneral.Build_DDS_Header(1, mip.Height, mip.Width, DDSFormat);
        }

        private void LoadFromFile(string imagePath, int desiredMaxDimension = 0, bool enforceResize = true)
        {
            Format format = new Format();
            FilePath = imagePath;

            // KFreon: Load image and save useful information including BGRA pixel data - may be processed from original into this form.
            DDSGeneral.DDS_HEADER tempheader = null;
            MipMaps = ImageEngine.LoadImage(imagePath, out format, desiredMaxDimension, enforceResize, out tempheader, false);

            // KFreon: Can't pass properties as out :(
            header = tempheader;
            Format = format;
        }

        
        private void LoadFromStream(Stream stream, string extension = null, int desiredMaxDimension = 0, bool enforceResize = true)
        {
            Format format = new Format();

            // KFreon: Load image and save useful information including BGRA pixel data - may be processed from original into this form.
            DDSGeneral.DDS_HEADER tempheader = null;
            MipMaps = ImageEngine.LoadImage(stream, out format, extension, desiredMaxDimension, enforceResize, out tempheader, false);
            header = tempheader;
            Format = format;
        }


        /// <summary>
        /// Saves image in specified format to file. If file exists, it will be overwritten.
        /// </summary>
        /// <param name="destination">File to save to.</param>
        /// <param name="format">Desired image format.</param>
        /// <param name="GenerateMips">Determines how mipmaps are handled during saving.</param>
        /// <param name="desiredMaxDimension">Maximum size for saved image. Resizes if required, but uses mipmaps if available.</param>
        /// <param name="mergeAlpha">DXT1 only. True = Uses threshold value and alpha values to mask RGB.</param>
        /// <param name="mipToSave">Index of mipmap to save as single image.</param>
        /// <returns>True if success.</returns>
        public bool Save(string destination, ImageEngineFormat format, MipHandling GenerateMips, int desiredMaxDimension = 0, int mipToSave = 0, bool mergeAlpha = false)
        {
            using (FileStream fs = new FileStream(destination, FileMode.Create))
                return Save(fs, format, GenerateMips, desiredMaxDimension, mipToSave, mergeAlpha);
        }


        /// <summary>
        /// Saves fully formatted image in specified format to stream.
        /// </summary>
        /// <param name="destination">Stream to save to.</param>
        /// <param name="format">Format to save as.</param>
        /// <param name="GenerateMips">Determines how mipmaps are handled during saving.</param>
        /// <param name="desiredMaxDimension">Maximum size for saved image. Resizes if required, but uses mipmaps if available.</param>
        /// <param name="mergeAlpha">ONLY valid when desiredMaxDimension != 0. True = alpha flattened, directly affecting RGB.</param>
        /// <param name="mipToSave">Selects a certain mip to save. 0 based.</param>
        /// <returns>True if success</returns>
        public bool Save(Stream destination, ImageEngineFormat format, MipHandling GenerateMips, int desiredMaxDimension = 0, int mipToSave = 0, bool mergeAlpha = false)
        {
            return ImageEngine.Save(MipMaps, format, destination, GenerateMips, mergeAlpha, desiredMaxDimension, mipToSave);
        }


        /// <summary>
        /// Saves fully formatted image in specified format to byte array.
        /// </summary>
        /// <param name="format">Format to save as.</param>
        /// <param name="GenerateMips">Determines how mipmaps are handled during saving.</param>
        /// <param name="desiredMaxDimension">Maximum size for saved image. Resizes if required, but uses mipmaps if available.</param>
        /// <param name="mipToSave">Index of mipmap to save directly.</param>
        /// <param name="mergeAlpha">ONLY valid when desiredMaxDimension != 0. True = alpha flattened, directly affecting RGB.</param>
        /// <returns></returns>
        public byte[] Save(ImageEngineFormat format, MipHandling GenerateMips, int desiredMaxDimension = 0, int mipToSave = 0, bool mergeAlpha = false)
        {
            return ImageEngine.Save(MipMaps, format, GenerateMips, desiredMaxDimension, mipToSave, mergeAlpha);
        }

        /// <summary>
        /// Gets a preview.
        /// </summary>
        /// <param name="ShowAlpha">False = Creates a preview without alpha.</param>
        /// <param name="index">Index of mipmap to preview.</param>
        /// <returns>BitmapImage of image.</returns>
        public BitmapSource GeneratePreview(int index, bool ShowAlpha)
        {
            // KFreon: NOTE: Seems to ignore alpha - pretty much ultra useful since premultiplying alpha often removes most of the image
            MipMap mip = MipMaps[index];

            BitmapSource bmp;
            if (ShowAlpha)
                bmp = mip.BaseImage;
            else
                bmp = new FormatConvertedBitmap(mip.BaseImage, System.Windows.Media.PixelFormats.Bgr32, null, 0);

            if (!bmp.IsFrozen)
                bmp.Freeze();

            return bmp;
        }


        /// <summary>
        /// Releases resources used by mipmap MemoryStreams.
        /// </summary>
        public void Dispose()
        {
            if (MipMaps == null)
                return;
        }


        /// <summary>
        /// Creates a GDI+ bitmap from largest mipmap.
        /// Does NOT require that image remains alive.
        /// </summary>
        /// <param name="ignoreAlpha">True = Previews image without alpha channel.</param>
        /// <param name="maxDimension">Largest size to display.</param>
        /// <param name="mergeAlpha">ONLY valid when maxDimension is set. True = flattens alpha, directly affecting RGB.</param>
        /// <returns>GDI+ bitmap of largest mipmap.</returns>
        public System.Drawing.Bitmap GetGDIBitmap(bool ignoreAlpha, bool mergeAlpha, int maxDimension = 0)
        {
            MipMap mip = MipMaps[0];

            if (maxDimension != 0)
            {
                // Choose a mip of the correct size, if available.
                var sizedMip = MipMaps.Where(m => (m.Height == maxDimension && m.Width <= maxDimension) || (m.Width == maxDimension && m.Height <= maxDimension));
                if (sizedMip.Any())
                    mip = sizedMip.First();
                else
                {
                    double scale = maxDimension * 1f / (Height > Width ? Height : Width);
                    mip = ImageEngine.Resize(mip, scale, mergeAlpha);
                }
            }

            mip.BaseImage.Freeze();
            return UsefulThings.WinForms.Imaging.CreateBitmap(mip.BaseImage, ignoreAlpha);
        }


        /// <summary>
        /// Scales top mipmap and DESTROYS ALL OTHERS.
        /// </summary>
        /// <param name="DesiredDimension">Desired size of image.</param>
        /// <param name="mergeAlpha">True = flattens alpha, directly affecting RGB.</param>
        public void Resize(int DesiredDimension, bool mergeAlpha)
        {
            double scale = (double)DesiredDimension / (double)MipMaps[0].Width;  // TODO Do height too?
            Resize(scale, mergeAlpha);
        }


        /// <summary>
        /// Scales top mipmap and DESTROYS ALL OTHERS.
        /// </summary>
        /// <param name="scale">Scaling factor. </param>
        /// <param name="mergeAlpha">True = flattens alpha, directly affecting RGB.</param>
        public void Resize(double scale, bool mergeAlpha)
        {
            MipMaps[0] = ImageEngine.Resize(MipMaps[0], scale, mergeAlpha);
            MipMaps.RemoveRange(1, NumMipMaps - 1);
        }

        /// <summary>
        /// Creates a WPF Bitmap from largest mipmap.
        /// Does NOT require that image remains alive.
        /// </summary>
        /// <param name="mergeAlpha">Only valid if maxDimension set. True = flattens alpha, directly affecting RGB.</param>
        /// <param name="maxDimension">Resizes image or uses a mipmap if available.</param>
        /// <returns>WPF bitmap of largest mipmap.</returns>
        public BitmapSource GetWPFBitmap(int maxDimension = 0, bool mergeAlpha = false)
        {
            MipMap mip = MipMaps[0];

            if (maxDimension != 0)
            {
                // Choose a mip of the correct size, if available.
                var sizedMip = MipMaps.Where(m => (m.Height == maxDimension && m.Width <= maxDimension) || (m.Width == maxDimension && m.Height <= maxDimension));
                if (sizedMip.Any())
                    mip = sizedMip.First();
                else
                {
                    double scale = maxDimension * 1f / (Height > Width ? Height : Width);
                    mip = ImageEngine.Resize(mip, scale, mergeAlpha);
                }
            }
            mip.BaseImage.Freeze();
            return mip.BaseImage;
        }
    }
}
