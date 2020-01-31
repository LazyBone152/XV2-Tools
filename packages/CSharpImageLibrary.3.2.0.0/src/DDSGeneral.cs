using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides general functions specific to DDS format
    /// </summary>
    public static class DDSGeneral
    {
        static byte V8U8Adjust = 128;  // KFreon: This is for adjusting out of signed land.  This gets removed on load and re-added on save.

        /// <summary>
        /// Value at which alpha is included in DXT1 conversions. i.e. pixels lower than this threshold are made 100% transparent, and pixels higher are made 100% opaque.
        /// </summary>
        public static float DXT1AlphaThreshold = 0.2f;

        #region Header Stuff
        /// <summary>
        /// Reads DDS header from file.
        /// </summary>
        /// <param name="h">Header struct.</param>
        /// <param name="r">File reader.</param>
        internal static void Read_DDS_HEADER(DDS_HEADER h, BinaryReader r)
        {
            h.dwSize = r.ReadInt32();
            h.dwFlags = r.ReadInt32();
            h.dwHeight = r.ReadInt32();
            h.dwWidth = r.ReadInt32();
            h.dwPitchOrLinearSize = r.ReadInt32();
            h.dwDepth = r.ReadInt32();
            h.dwMipMapCount = r.ReadInt32();
            for (int i = 0; i < 11; ++i)
                h.dwReserved1[i] = r.ReadInt32();
            Read_DDS_PIXELFORMAT(h.ddspf, r);
            h.dwCaps = r.ReadInt32();
            h.dwCaps2 = r.ReadInt32();
            h.dwCaps3 = r.ReadInt32();
            h.dwCaps4 = r.ReadInt32();
            h.dwReserved2 = r.ReadInt32();
        }

        /// <summary>
        /// Reads DDS pixel format.
        /// </summary>
        /// <param name="p">Pixel format struct.</param>
        /// <param name="r">File reader.</param>
        private static void Read_DDS_PIXELFORMAT(DDS_PIXELFORMAT p, BinaryReader r)
        {
            p.dwSize = r.ReadInt32();
            p.dwFlags = r.ReadInt32();
            p.dwFourCC = r.ReadInt32();
            p.dwRGBBitCount = r.ReadInt32();
            p.dwRBitMask = r.ReadUInt32();
            p.dwGBitMask = r.ReadUInt32();
            p.dwBBitMask = r.ReadUInt32();
            p.dwABitMask = r.ReadUInt32();
        }

        /// <summary>
        /// Contains information about DDS Headers. 
        /// </summary>
        public class DDS_HEADER
        {
            public int dwSize { get; set; }
            public int dwFlags { get; set; }
            public int dwHeight { get; set; }
            public int dwWidth { get; set; }
            public int dwPitchOrLinearSize { get; set; }
            public int dwDepth { get; set; }
            public int dwMipMapCount { get; set; }
            public int[] dwReserved1 = new int[11];
            public DDS_PIXELFORMAT ddspf
            {
                get; set;
            } = new DDS_PIXELFORMAT();
            public int dwCaps { get; set; }
            public int dwCaps2;
            public int dwCaps3;
            public int dwCaps4;
            public int dwReserved2;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("--DDS_HEADER--");
                sb.AppendLine($"dwSize: {dwSize}");
                sb.AppendLine($"dwFlags: 0x{dwFlags.ToString("X")}");  // As hex
                sb.AppendLine($"dwHeight: {dwHeight}");
                sb.AppendLine($"dwWidth: {dwWidth}");
                sb.AppendLine($"dwPitchOrLinearSize: {dwPitchOrLinearSize}");
                sb.AppendLine($"dwDepth: {dwDepth}");
                sb.AppendLine($"dwMipMapCount: {dwMipMapCount}");
                sb.AppendLine($"ddspf: ");
                sb.AppendLine(ddspf.ToString());
                sb.AppendLine($"dwCaps: 0x{dwCaps.ToString("X")}");
                sb.AppendLine($"dwCaps2: {dwCaps2}");
                sb.AppendLine($"dwCaps3: {dwCaps3}");
                sb.AppendLine($"dwCaps4: {dwCaps4}");
                sb.AppendLine($"dwReserved2: {dwReserved2}");
                sb.AppendLine("--END DDS_HEADER--");
                return sb.ToString();
            }
        }

        
        /// <summary>
        /// Contains information about DDS Pixel Format.
        /// </summary>
        public class DDS_PIXELFORMAT
        {
            public int dwSize { get; set; }
            public int dwFlags { get; set; }
            public int dwFourCC { get; set; }
            public int dwRGBBitCount { get; set; }
            public uint dwRBitMask { get; set; }
            public uint dwGBitMask { get; set; }
            public uint dwBBitMask { get; set; }
            public uint dwABitMask { get; set; }

            public DDS_PIXELFORMAT()
            {
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("--DDS_PIXELFORMAT--");
                sb.AppendLine($"dwSize: {dwSize}");
                sb.AppendLine($"dwFlags: 0x{dwFlags.ToString("X")}");  // As hex
                sb.AppendLine($"dwFourCC: 0x{dwFourCC.ToString("X")}");  // As Hex
                sb.AppendLine($"dwRGBBitCount: {dwRGBBitCount}");
                sb.AppendLine($"dwRBitMask: 0x{dwRBitMask.ToString("X")}");  // As Hex
                sb.AppendLine($"dwGBitMask: 0x{dwGBitMask.ToString("X")}");  // As Hex
                sb.AppendLine($"dwBBitMask: 0x{dwBBitMask.ToString("X")}");  // As Hex
                sb.AppendLine($"dwABitMask: 0x{dwABitMask.ToString("X")}");  // As Hex
                sb.AppendLine("--END DDS_PIXELFORMAT--");
                return sb.ToString();
            }
        }


        /// <summary>
        /// Builds a header for DDS file format using provided information.
        /// </summary>
        /// <param name="Mips">Number of mips in image.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="surfaceformat">DDS FourCC.</param>
        /// <returns>Header for DDS file.</returns>
        public static DDS_HEADER Build_DDS_Header(int Mips, int Height, int Width, ImageEngineFormat surfaceformat)
        {
            DDS_HEADER header = new DDS_HEADER();
            header.dwSize = 124;
            header.dwFlags = 0x1 | 0x2 | 0x4 | 0x0 | 0x1000 | (Mips != 1 ? 0x20000 : 0x0) | 0x0 | 0x0;  // Flags to denote fields: DDSD_CAPS = 0x1 | DDSD_HEIGHT = 0x2 | DDSD_WIDTH = 0x4 | DDSD_PITCH = 0x8 | DDSD_PIXELFORMAT = 0x1000 | DDSD_MIPMAPCOUNT = 0x20000 | DDSD_LINEARSIZE = 0x80000 | DDSD_DEPTH = 0x800000
            header.dwWidth = Width;
            header.dwHeight = Height;
            header.dwCaps = 0x1000 | (Mips == 1 ? 0 : (0x8 | 0x400000));  // Flags are: 0x8 = Optional: Used for mipmapped textures | 0x400000 = DDSCAPS_MIMPAP | 0x1000 = DDSCAPS_TEXTURE
            header.dwMipMapCount = Mips == 1 ? 1 : Mips;

            DDS_PIXELFORMAT px = new DDS_PIXELFORMAT();
            px.dwSize = 32;
            px.dwFourCC = (int)surfaceformat;
            px.dwFlags = 4;

            switch (surfaceformat)
            {
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    px.dwFlags |= 0x80000;
                    header.dwPitchOrLinearSize = (int)(Width * Height);
                    break;
                case ImageEngineFormat.DDS_ATI1:
                    header.dwFlags |= 0x80000;  
                    header.dwPitchOrLinearSize = (int)(Width * Height / 2f);
                    break;
                case ImageEngineFormat.DDS_G8_L8:
                    px.dwFlags = 0x20000;
                    header.dwPitchOrLinearSize = Width * 8; // maybe?
                    header.dwFlags |= 0x8;
                    px.dwRGBBitCount = 8;
                    px.dwRBitMask = 0xFF;
                    px.dwFourCC = 0x0;
                    break;
                case ImageEngineFormat.DDS_ARGB:
                    px.dwFlags = 0x41;
                    px.dwFourCC = 0x0;
                    px.dwRGBBitCount = 32;
                    px.dwRBitMask = 0xFF0000;
                    px.dwGBitMask = 0xFF00;
                    px.dwBBitMask = 0xFF;
                    px.dwABitMask = 0xFF000000;
                    break;
                case ImageEngineFormat.DDS_V8U8:
                    px.dwFourCC = 0x0;
                    px.dwFlags = 0x80000;  // 0x80000 not actually a valid value....
                    px.dwRGBBitCount = 16;
                    px.dwRBitMask = 0xFF;
                    px.dwGBitMask = 0xFF00;
                    break;
            }
            

            header.ddspf = px;
            return header;
        }


        /// <summary>
        /// Write DDS header to stream via BinaryWriter.
        /// </summary>
        /// <param name="header">Populated DDS header by Build_DDS_Header.</param>
        /// <param name="writer">Stream to write to.</param>
        public static void Write_DDS_Header(DDS_HEADER header, BinaryWriter writer)
        {
            // KFreon: Write magic number ("DDS")
            writer.Write(0x20534444);

            // KFreon: Write all header fields regardless of filled or not
            writer.Write(header.dwSize);
            writer.Write(header.dwFlags);
            writer.Write(header.dwHeight);
            writer.Write(header.dwWidth);
            writer.Write(header.dwPitchOrLinearSize);
            writer.Write(header.dwDepth);
            writer.Write(header.dwMipMapCount);

            // KFreon: Write reserved1
            for (int i = 0; i < 11; i++)
                writer.Write(0);

            // KFreon: Write PIXELFORMAT
            DDS_PIXELFORMAT px = header.ddspf;
            writer.Write(px.dwSize);
            writer.Write(px.dwFlags);
            writer.Write(px.dwFourCC);
            writer.Write(px.dwRGBBitCount);
            writer.Write(px.dwRBitMask);
            writer.Write(px.dwGBitMask);
            writer.Write(px.dwBBitMask);
            writer.Write(px.dwABitMask);

            writer.Write(header.dwCaps);
            writer.Write(header.dwCaps2);
            writer.Write(header.dwCaps3);
            writer.Write(header.dwCaps4);
            writer.Write(header.dwReserved2);
        }
        #endregion Header Stuff


        #region Saving
        /// <summary>
        /// Writes a DDS file using a format specific function to write pixels.
        /// </summary>
        /// <param name="MipMaps">List of MipMaps to save. Pixels only.</param>
        /// <param name="Destination">Stream to save to.</param>
        /// <param name="header">Header to use.</param>
        /// <param name="PixelWriter">Function to write pixels. Optionally also compresses blocks before writing.</param>
        /// <param name="isBCd">True = Block Compressed DDS. Performs extra manipulation to get and order Texels.</param>
        /// <returns>True on success.</returns>
        internal static bool WriteDDS(List<MipMap> MipMaps, Stream Destination, DDS_HEADER header, Action<Stream, Stream, int, int> PixelWriter, bool isBCd)
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(Destination, Encoding.Default, true))
                    Write_DDS_Header(header, writer);

                for (int m = 0; m < MipMaps.Count; m++)
                {
                    unsafe
                    {
                        UnmanagedMemoryStream mipmap = new UnmanagedMemoryStream((byte*)MipMaps[m].BaseImage.BackBuffer.ToPointer(), MipMaps[m].Width * MipMaps[m].Height * 4);
                        using (var compressed = WriteMipMap(mipmap, MipMaps[m].Width, MipMaps[m].Height, PixelWriter, isBCd))
                            compressed.WriteTo(Destination);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return false;
            }
        }


        /// <summary>
        /// Write a mipmap to a stream using a format specific pixel writing function.
        /// </summary>
        /// <param name="pixelData">Pixels of mipmap.</param>
        /// <param name="Width">Mipmap Width.</param>
        /// <param name="Height">Mipmap Height.</param>
        /// <param name="PixelWriter">Function to write pixels with. Also compresses if block compressed texture.</param>
        /// <param name="isBCd">True = Block Compressed DDS.</param>
        private static MemoryStream WriteMipMap(Stream pixelData, int Width, int Height, Action<Stream, Stream, int, int> PixelWriter, bool isBCd)
        {
            int bitsPerScanLine = isBCd ? 4 * Width : Width;  // KFreon: Bits per image line.

            MemoryStream mipmap = new MemoryStream(bitsPerScanLine * 2);  // not accurate length requirements

            // KFreon: Loop over rows and columns, doing extra moving if Block Compressed to accommodate texels.
            int texelCount = isBCd ? Height / 4 : Height;
            int compressedLineSize = 0;
            if (texelCount == 0)
            {
                // ignore for now...
                mipmap.Write(new byte[bitsPerScanLine], 0, bitsPerScanLine); // hopefully long enough to end it
            }
            else
            {
                Action<int, ParallelLoopState> action = new Action<int, ParallelLoopState>((rowr, loopstate) =>
                {
                    int rowIndex = rowr;
                    using (var compressedLine = WriteMipLine(pixelData, Width, Height, bitsPerScanLine, isBCd, rowIndex, PixelWriter))
                    {
                        if (compressedLine != null)
                        {
                            lock (mipmap)
                            {
                                // KFreon: Detect size of a compressed line
                                if (compressedLineSize == 0)
                                    compressedLineSize = (int)compressedLine.Length;

                                mipmap.Seek(rowIndex * compressedLineSize, SeekOrigin.Begin);
                                compressedLine.WriteTo(mipmap);
                            }
                        }
                        else if (ImageEngine.EnableThreading)
                            loopstate.Break();
                        else if (!ImageEngine.EnableThreading)
                            return;
                    }
                });

                // Decide whether to thread or not
                if (ImageEngine.EnableThreading)
                {
                    ParallelOptions po = new ParallelOptions();
                    po.MaxDegreeOfParallelism = -1;
                    Parallel.For(0, texelCount, po, (rowr, loopstate) => action(rowr, loopstate));
                }
                else
                {
                    for (int rowr = 0; rowr < texelCount; rowr++)
                        action(rowr, null);
                }
                
            }

            return mipmap;
        }

        private static MemoryStream WriteMipLine(Stream pixelData, int Width, int Height, int bitsPerScanLine, bool isBCd, int rowIndex, Action<Stream, Stream, int, int> PixelWriter)
        {
            MemoryStream CompressedLine = new MemoryStream(bitsPerScanLine); // Not correct compressed size but it's close enough to not have it waste tonnes of time copying.
            using (MemoryStream UncompressedLine = new MemoryStream(4 * bitsPerScanLine))
            {
                lock (pixelData)
                {
                    // KFreon: Ensure we're in the right place
                    pixelData.Position = rowIndex * 4 * bitsPerScanLine;  // Uncompressed location

                    // KFreon: since mip count is an estimate, check to see if there are any mips left to read.
                    if (pixelData.Position >= pixelData.Length)
                        return null;

                    // KFreon: Read compressed line
                    UncompressedLine.ReadFrom(pixelData, 4 * bitsPerScanLine);
                    UncompressedLine.Position = 0;
                }

                for (int w = 0; w < Width; w += (isBCd ? 4 : 1))
                {
                    PixelWriter(CompressedLine, UncompressedLine, Width, Height);
                    if (isBCd && w != Width - 4 && Width > 4 && Height > 4)  // KFreon: Only do this if dimensions are big enough
                        UncompressedLine.Seek(-(bitsPerScanLine * 4) + 4 * 4, SeekOrigin.Current);  // Not at an row end texel. Moves back up to read next texel in row.
                }
            }

            return CompressedLine;
        }
        #endregion Save


        #region Loading
        private static MipMap ReadUncompressedMipMap(Stream stream, int mipWidth, int mipHeight, Func<Stream, List<byte>> PixelReader)
        {
            // KFreon: Since mip count is an estimate, check if there are any mips left to read.
            if (stream.Position >= stream.Length)
                return null;

            int count = 0;
            byte[] mipmap = new byte[mipHeight * mipWidth * 4];
            for (int y = 0; y < mipHeight; y++)
            {
                for (int x = 0; x < mipWidth; x++)
                {
                    List<byte> bgra = PixelReader(stream);  // KFreon: Reads pixel using a method specific to the format as provided
                    mipmap[count++] = bgra[0];
                    mipmap[count++] = bgra[1];
                    mipmap[count++] = bgra[2];
                    mipmap[count++] = bgra[3];
                }
            }

            return new MipMap(UsefulThings.WPF.Images.CreateWriteableBitmap(mipmap, mipWidth, mipHeight));
        }

        private static MipMap ReadCompressedMipMap(Stream compressed, int mipWidth, int mipHeight, int blockSize, long mipOffset, Func<Stream, List<byte[]>> DecompressBlock)
        {
            //MemoryStream mipmap = new MemoryStream(4 * mipWidth * mipHeight);
            byte[] mipmapData = new byte[4 * mipWidth * mipHeight];

            // Loop over rows and columns NOT pixels
            int compressedLineSize = blockSize * mipWidth / 4;
            int bitsPerScanline = 4 * (int)mipWidth;
            int texelCount = mipHeight / 4;
            if (texelCount != 0)
            {
                Action<int, ParallelLoopState> action = new Action<int, ParallelLoopState>((rowr, loopstate) =>
                {
                    int row = rowr;
                    using (MemoryStream DecompressedLine = ReadBCMipLine(compressed, mipHeight, mipWidth, bitsPerScanline, mipOffset, compressedLineSize, row, DecompressBlock))
                    {
                        if (DecompressedLine != null)
                            lock (mipmapData)
                            {
                                int index = row * bitsPerScanline * 4;
                                DecompressedLine.Position = 0;
                                int length = DecompressedLine.Length > mipmapData.Length ? mipmapData.Length : (int)DecompressedLine.Length;
                                if (index + length <= mipmapData.Length)
                                    DecompressedLine.Read(mipmapData, index, length);
                            }
                        else if (ImageEngine.EnableThreading)
                            loopstate.Break();
                        else if (!ImageEngine.EnableThreading)
                            return;
                    }
                });

                if (ImageEngine.EnableThreading)
                    Parallel.For(0, texelCount, (rowr, loopstate) => action(rowr, loopstate));
                else
                    for (int rowr = 0; rowr < texelCount; rowr++)
                        action(rowr, null);

            }

            return new MipMap(UsefulThings.WPF.Images.CreateWriteableBitmap(mipmapData, mipWidth, mipHeight));
        }

        private static List<byte> ReadG8_L8Pixel(Stream fileData)
        {
            byte red = (byte)fileData.ReadByte();
            byte green = red;
            byte blue = red;  // KFreon: Same colour for other channels to make grayscale.

            return new List<byte>() { blue, green, red, 0xFF };
        }

        private static List<byte> ReadV8U8Pixel(Stream fileData)
        {
            byte[] rg = fileData.ReadBytes(2);
            byte red = (byte)(rg[0] - V8U8Adjust);
            byte green = (byte)(rg[1] - V8U8Adjust);
            byte blue = 0xFF;

            return new List<byte>() { blue, green, red, 0xFF };
        }

        private static List<byte> ReadRGBPixel(Stream fileData)
        {
            var rgb = fileData.ReadBytes(3);
            byte red = rgb[0];
            byte green = rgb[1];
            byte blue = rgb[2];
            return new List<byte>() { red, green, blue, 0xFF };
        }

        private static List<byte> ReadA8L8Pixel(Stream fileData)
        {
            var al = fileData.ReadBytes(2);
            return new List<byte>() { al[0], al[0], al[0], al[1] };
        }

        internal static List<MipMap> LoadDDS(Stream compressed, DDS_HEADER header, Format format, int desiredMaxDimension)
        {           
            List<MipMap> MipMaps = new List<MipMap>();

            int mipWidth = header.dwWidth;
            int mipHeight = header.dwHeight;

            int estimatedMips = header.dwMipMapCount == 0 ? EstimateNumMipMaps(mipWidth, mipHeight) + 1 : header.dwMipMapCount;
            long mipOffset = 128;  // Includes header

            // KFreon: Check number of mips is correct. i.e. For some reason, some images have more data than they should, so it can't detected it.
            // So I check the number of mips possibly contained in the image based on size and compare it to how many it should have.
            // Any image that is too short to contain all the mips it should loads only the top mip and ignores the "others".
            int testest = 0;
            var test = EnsureMipInImage(compressed.Length, mipWidth, mipHeight, 4, format, out testest, estimatedMips);  // Update number of mips too
            if (test == -1)
                estimatedMips = 1;

            // KFreon: Decide which mip to start loading at - going to just load a few mipmaps if asked instead of loading all, then choosing later. That's slow.
            if (desiredMaxDimension != 0 && estimatedMips > 1)
            {
                int tempEstimation;
                mipOffset = EnsureMipInImage(compressed.Length, mipWidth, mipHeight, desiredMaxDimension, format, out tempEstimation);  // Update number of mips too
                if (mipOffset > 128)
                {

                    double divisor = mipHeight > mipWidth ? mipHeight / desiredMaxDimension : mipWidth / desiredMaxDimension;
                    mipHeight = (int)(mipHeight / divisor);
                    mipWidth = (int)(mipWidth / divisor);

                    if (mipWidth == 0 || mipHeight == 0)  // Reset as a dimension is too small to resize
                    {
                        mipHeight = header.dwHeight;
                        mipWidth = header.dwWidth;
                        mipOffset = 128;
                    }
                    else
                        estimatedMips = tempEstimation + 1;  // cos it needs the extra one for the top?
                }
                else
                    mipOffset = 128;

            }

            compressed.Position = mipOffset;



            Func<Stream, List<byte[]>> DecompressBCBlock = null;
            Func<Stream, List<byte>> UncompressedPixelReader = null;
            switch (format.SurfaceFormat)
            {
                case ImageEngineFormat.DDS_RGB:
                    UncompressedPixelReader = ReadRGBPixel;
                    break;
                case ImageEngineFormat.DDS_A8L8:
                    UncompressedPixelReader = ReadA8L8Pixel;
                    break;
                case ImageEngineFormat.DDS_ARGB:  // leave this one. It has a totally different reading method and is done later
                    break;
                case ImageEngineFormat.DDS_ATI1:
                    DecompressBCBlock = DecompressATI1;
                    break;
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    DecompressBCBlock = DecompressATI2Block;
                    break;
                case ImageEngineFormat.DDS_DXT1:
                    DecompressBCBlock = DecompressBC1Block;
                    break;
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                    DecompressBCBlock = DecompressBC2Block;
                    break;
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    DecompressBCBlock = DecompressBC3Block;
                    break;
                case ImageEngineFormat.DDS_G8_L8:
                    UncompressedPixelReader = ReadG8_L8Pixel;
                    break;
                case ImageEngineFormat.DDS_V8U8:
                    UncompressedPixelReader = ReadV8U8Pixel;
                    break;
                default:
                    throw new Exception("ahaaha");
            }

            // KFreon: Read mipmaps
            for (int m = 0; m < estimatedMips; m++)
            {
                // KFreon: If mip is too small, skip out. This happens most often with non-square textures. I think it's because the last mipmap is square instead of the same aspect.
                if (mipWidth <= 0 || mipHeight <= 0 || compressed.Position >= compressed.Length)  // Needed cos it doesn't throw when reading past the end for some reason.
                {
                    Debugger.Break();
                    break;
                }

                MipMap mipmap = null;
                if (format.IsBlockCompressed)
                    mipmap = ReadCompressedMipMap(compressed, mipWidth, mipHeight, format.BlockSize, mipOffset, DecompressBCBlock);
                else
                {
                    int mipLength = mipWidth * mipHeight * 4;

                    var array = new byte[mipLength];
                    long position = compressed.Position;

                    if (format.SurfaceFormat == ImageEngineFormat.DDS_ARGB)
                    {
                        compressed.Position = position;
                        compressed.Read(array, 0, array.Length);
                    }
                    else
                        mipmap = ReadUncompressedMipMap(compressed, mipWidth, mipHeight, UncompressedPixelReader);

                    if (mipmap == null)
                        mipmap = new MipMap(UsefulThings.WPF.Images.CreateWriteableBitmap(array, mipWidth, mipHeight));
                }

                MipMaps.Add(mipmap);

                mipOffset += mipWidth * mipHeight * format.BlockSize / 16; // Only used for BC textures

                mipWidth /= 2;
                mipHeight /= 2;

            }
            if (MipMaps.Count == 0)
                Debugger.Break();
            return MipMaps;
        }

        private static MemoryStream ReadBCMipLine(Stream compressed, int mipHeight, int mipWidth, int bitsPerScanLine, long mipOffset, int compressedLineSize, int rowIndex, Func<Stream, List<byte[]>> DecompressBlock)
        {
            int bitsPerPixel = 4;

            MemoryStream DecompressedLine = new MemoryStream(bitsPerScanLine * 4);

            // KFreon: Read compressed line into new stream for multithreading purposes
            using (MemoryStream CompressedLine = new MemoryStream(compressedLineSize))
            {
                lock (compressed)
                {
                    // KFreon: Seek to correct texel
                    compressed.Position = mipOffset + rowIndex * compressedLineSize;  // +128 = header size

                    // KFreon: since mip count is an estimate, check to see if there are any mips left to read.
                    if (compressed.Position >= compressed.Length)
                        return null;

                    // KFreon: Read compressed line
                    CompressedLine.ReadFrom(compressed, compressedLineSize);
                    if (CompressedLine.Length < compressedLineSize)
                        Debugger.Break();
                }
                CompressedLine.Position = 0;

                // KFreon: Read texels in row
                for (int column = 0; column < mipWidth; column += 4)
                {
                    try
                    {
                        // decompress 
                        List<byte[]> decompressed = DecompressBlock(CompressedLine);
                        byte[] blue = decompressed[0];
                        byte[] green = decompressed[1];
                        byte[] red = decompressed[2];
                        byte[] alpha = decompressed[3];


                        // Write texel
                        int TopLeft = column * bitsPerPixel;// + rowIndex * 4 * bitsPerScanLine;  // Top left corner of texel IN BYTES (i.e. expanded pixels to 4 channels)
                        DecompressedLine.Seek(TopLeft, SeekOrigin.Begin);
                        byte[] block = new byte[16];
                        for (int i = 0; i < 16; i += 4)
                        {
                            // BGRA
                            for (int j = 0; j < 16; j += 4)
                            {
                                block[j] = blue[i + (j >> 2)];
                                block[j + 1] = green[i + (j >> 2)];
                                block[j + 2] = red[i + (j >> 2)];
                                block[j + 3] = alpha[i + (j >> 2)];
                            }
                            DecompressedLine.Write(block, 0, 16);

                            // Go one line of pixels down (bitsPerScanLine), then to the left side of the texel (4 pixels back from where it finished)
                            DecompressedLine.Seek(bitsPerScanLine - bitsPerPixel * 4, SeekOrigin.Current);
                        }
                    }
                    catch
                    {
                        // Ignore. Most likely error reading smaller mips that don't behave
                    }
                    
                }
            }
                
            return DecompressedLine;
        }
        #endregion Loading


        #region Block Decompression
        /// <summary>
        /// Decompresses an 8 bit channel.
        /// </summary>
        /// <param name="compressed">Compressed image data.</param>
        /// <param name="isSigned">true = use signed alpha range (-254 -- 255), false = 0 -- 255</param>
        /// <returns>Single channel decompressed (16 bits).</returns>
        internal static byte[] Decompress8BitBlock(Stream compressed, bool isSigned)
        {
            byte[] DecompressedBlock = new byte[16];

            // KFreon: Read min and max colours (not necessarily in that order)
            byte[] block = new byte[8];
            compressed.Read(block, 0, 8);

            byte min = block[0];
            byte max = block[1];

            byte[] Colours = Build8BitPalette(min, max, isSigned);

            // KFreon: Decompress pixels
            ulong bitmask = (ulong)block[2] << 0 | (ulong)block[3] << 8 | (ulong)block[4] << 16 |   // KFreon: Read all 6 compressed bytes into single 
                (ulong)block[5] << 24 | (ulong)block[6] << 32 | (ulong)block[7] << 40;


            // KFreon: Bitshift and mask compressed data to get 3 bit indicies, and retrieve indexed colour of pixel.
            for (int i = 0; i < 16; i++)
                DecompressedBlock[i] = (byte)Colours[bitmask >> (i * 3) & 0x7];

            return DecompressedBlock;
        }

        /// <summary>
        /// Decompresses a 3 channel (RGB) block.
        /// </summary>
        /// <param name="compressed">Compressed image data.</param>
        /// <param name="isDXT1">True = DXT1, otherwise false.</param>
        /// <returns>16 pixel BGRA channels.</returns>
        internal static List<byte[]> DecompressRGBBlock(Stream compressed, bool isDXT1)
        {
            int[] DecompressedBlock = new int[16];

            ushort colour0;
            ushort colour1;
            byte[] pixels = null;
            int[] Colours = null;

            List<byte[]> DecompressedChannels = new List<byte[]>(4);
            byte[] red = new byte[16];
            byte[] green = new byte[16];
            byte[] blue = new byte[16];
            byte[] alpha = new byte[16];
            DecompressedChannels.Add(blue);
            DecompressedChannels.Add(green);
            DecompressedChannels.Add(red);
            DecompressedChannels.Add(alpha);

            try
            {
                using (BinaryReader reader = new BinaryReader(compressed, Encoding.Default, true))
                {
                    // Read min max colours
                    colour0 = (ushort)reader.ReadInt16();
                    colour1 = (ushort)reader.ReadInt16();
                    Colours = BuildRGBPalette(colour0, colour1, isDXT1);

                    // Decompress pixels
                    pixels = reader.ReadBytes(4);
                }
            }
            catch (EndOfStreamException e)
            {
                Console.WriteLine();
                // It's due to weird shaped mips at really low resolution. Like 2x4

                return DecompressedChannels;
            }
            

                
            for (int i = 0; i < 16; i += 4)
            {
                //byte bitmask = (byte)compressed.ReadByte();
                byte bitmask = pixels[i / 4];
                for (int j = 0; j < 4; j++)
                    DecompressedBlock[i + j] = Colours[bitmask >> (2 * j) & 0x03];
            }

            // KFreon: Decode into BGRA
            for (int i = 0; i < 16; i++)
            {
                int colour = DecompressedBlock[i];
                var rgb = ReadDXTColour(colour);
                red[i] = rgb[0];
                green[i] = rgb[1];
                blue[i] = rgb[2];
                alpha[i] = 0xFF;
            }
            return DecompressedChannels;
        }
        #endregion


        #region Block Compression
        #region RGB DXT
        /// <summary>
        /// This region contains stuff adpated/taken from the DirectXTex project: https://github.com/Microsoft/DirectXTex
        /// Things needed to be in the range 0-1 instead of 0-255, hence new struct etc
        /// </summary>
        struct RGBColour
        {
            public float r, g, b, a;

            public RGBColour(float red, float green, float blue, float alpha)
            {
                r = red;
                g = green;
                b = blue;
                a = alpha;
            }
        }

        static float[] pC3 = { 1f, 1f / 2f, 0f };
        static float[] pD3 = { 0f, 1f / 2f, 1f };

        static float[] pC4 = { 1f, 2f / 3f, 1f / 3f, 0f };
        static float[] pD4 = { 0f, 1f / 3f, 2f / 3f, 1f };

        static uint[] psteps3 = { 0, 2, 1 };
        static uint[] psteps4 = { 0, 2, 3, 1 };

        static RGBColour Luminance = new RGBColour(0.2125f / 0.7154f, 1f, 0.0721f / 0.7154f, 1f);
        static RGBColour LuminanceInv = new RGBColour(0.7154f / 0.2125f, 1f, 0.7154f / 0.0721f, 1f);

        static RGBColour Decode565(uint wColour)
        {
            RGBColour colour = new RGBColour();
            colour.r = ((wColour >> 11) & 31) * (1f / 31f);
            colour.g = ((wColour >> 5) & 63) * (1f / 63f);
            colour.b = ((wColour >> 0) & 31) * (1f / 31f);
            colour.a = 1f;

            return colour;
        }

        static uint Encode565(RGBColour colour)
        {
            RGBColour temp = new RGBColour();
            temp.r = (colour.r < 0f) ? 0f : (colour.r > 1f) ? 1f : colour.r;
            temp.g = (colour.g < 0f) ? 0f : (colour.g > 1f) ? 1f : colour.g;
            temp.b = (colour.b < 0f) ? 0f : (colour.b > 1f) ? 1f : colour.b;

            return (uint)(temp.r * 31f + 0.5f) << 11 | (uint)(temp.g * 63f + 0.5f) << 5 | (uint)(temp.b * 31f + 0.5f);
        }

        static RGBColour ReadColourFromTexel(byte[] texel, int i)
        {
            // Pull out rgb from texel
            byte r = texel[i + 2];
            byte g = texel[i + 1];
            byte b = texel[i];
            byte a = texel[i + 3];

            // Create current pixel colour
            RGBColour current = new RGBColour();
            current.r = r / 255f;
            current.g = g / 255f;
            current.b = b / 255f;
            current.a = a / 255f;

            return current;
        }

        private static RGBColour[] OptimiseRGB(RGBColour[] Colour, int uSteps)
        {
            float[] pC = uSteps == 3 ? pC3 : pC4;
            float[] pD = uSteps == 3 ? pD3 : pD4;

            // Find min max
            RGBColour X = Luminance;
            RGBColour Y = new RGBColour();

            for (int i = 0; i < Colour.Length; i++)
            {
                RGBColour current = Colour[i];

                // X = min, Y = max
                if (current.r < X.r)
                    X.r = current.r;

                if (current.g < X.g)
                    X.g = current.g;

                if (current.b < X.b)
                    X.b = current.b;


                if (current.r > Y.r)
                    Y.r = current.r;

                if (current.g > Y.g)
                    Y.g = current.g;

                if (current.b > Y.b)
                    Y.b = current.b;
            }

            // Diagonal axis - starts with difference between min and max
            RGBColour diag = new RGBColour();
            diag.r = Y.r - X.r;
            diag.g = Y.g - X.g;
            diag.b = Y.b - X.b;

            float fDiag = diag.r * diag.r + diag.g * diag.g + diag.b * diag.b;
            if (fDiag < 1.175494351e-38F)
            {
                RGBColour min1 = new RGBColour();
                min1.r = X.r;
                min1.g = X.g;
                min1.b = X.b;

                RGBColour max1 = new RGBColour();
                max1.r = Y.r;
                max1.g = Y.g;
                max1.b = Y.b;

                return new RGBColour[] { min1, max1 };
            }

            float FdiagInv = 1f / fDiag;

            RGBColour Dir = new RGBColour();
            Dir.r = diag.r * FdiagInv;
            Dir.g = diag.g * FdiagInv;
            Dir.b = diag.b * FdiagInv;

            RGBColour Mid = new RGBColour();
            Mid.r = (X.r + Y.r) * .5f;
            Mid.g = (X.g + Y.g) * .5f;
            Mid.b = (X.b + Y.b) * .5f;

            float[] fDir = new float[4];

            for (int i = 0; i < Colour.Length; i++)
            {
                RGBColour pt = new RGBColour();
                pt.r = Dir.r * (Colour[i].r - Mid.r);
                pt.g = Dir.g * (Colour[i].g - Mid.g);
                pt.b = Dir.b * (Colour[i].b - Mid.b);

                float f = 0;
                f = pt.r + pt.g + pt.b;
                fDir[0] += f * f;

                f = pt.r + pt.g - pt.b;
                fDir[1] += f * f;

                f = pt.r - pt.g + pt.b;
                fDir[2] += f * f;

                f = pt.r - pt.g - pt.b;
                fDir[3] += f * f;
            }

            float fDirMax = fDir[0];
            int iDirMax = 0;
            for (int iDir = 1; iDir < 4; iDir++)
            {
                if (fDir[iDir] > fDirMax)
                {
                    fDirMax = fDir[iDir];
                    iDirMax = iDir;
                }
            }

            if ((iDirMax & 2) != 0)
            {
                float f = X.g;
                X.g = Y.g;
                Y.g = f;
            }

            if ((iDirMax & 1) != 0)
            {
                float f = X.b;
                X.b = Y.b;
                Y.b = f;
            }

            if (fDiag < 1f / 4096f)
            {
                RGBColour min1 = new RGBColour();
                min1.r = X.r;
                min1.g = X.g;
                min1.b = X.b;

                RGBColour max1 = new RGBColour();
                max1.r = Y.r;
                max1.g = Y.g;
                max1.b = Y.b;


                return new RGBColour[] { min1, max1 };
            }

            // newtons method for local min of sum of squares error.
            float fsteps = uSteps - 1;
            for (int iteration = 0; iteration < 8; iteration++)
            {
                RGBColour[] pSteps = new RGBColour[4];

                for (int iStep = 0; iStep < uSteps; iStep++)
                {
                    pSteps[iStep].r = X.r * pC[iStep] + Y.r * pD[iStep];
                    pSteps[iStep].g = X.g * pC[iStep] + Y.g * pD[iStep];
                    pSteps[iStep].b = X.b * pC[iStep] + Y.b * pD[iStep];
                }


                // colour direction
                Dir.r = Y.r - X.r;
                Dir.g = Y.g - X.g;
                Dir.b = Y.b - X.b;

                float fLen = Dir.r * Dir.r + Dir.g * Dir.g + Dir.b * Dir.b;

                if (fLen < (1f / 4096f))
                    break;

                float fScale = fsteps / fLen;
                Dir.r *= fScale;
                Dir.g *= fScale;
                Dir.b *= fScale;

                // Evaluate function and derivatives
                float d2X = 0, d2Y = 0;
                RGBColour dX, dY;
                dX = new RGBColour();
                dY = new RGBColour();

                for (int i = 0; i < Colour.Length; i++)
                {
                    RGBColour current = Colour[i];

                    float fDot = (current.r - X.r) * Dir.r + (current.g - X.g) * Dir.g + (current.b - X.b) * Dir.b;

                    int iStep = 0;
                    if (fDot <= 0)
                        iStep = 0;
                    else if (fDot >= fsteps)
                        iStep = uSteps - 1;
                    else
                        iStep = (int)(fDot + .5f);

                    RGBColour diff = new RGBColour();
                    diff.r = pSteps[iStep].r - current.r;
                    diff.g = pSteps[iStep].g - current.g;
                    diff.b = pSteps[iStep].b - current.b;

                    float fC = pC[iStep] * 1f / 8f;
                    float fD = pD[iStep] * 1f / 8f;

                    d2X += fC * pC[iStep];
                    dX.r += fC * diff.r;
                    dX.g += fC * diff.g;
                    dX.b += fC * diff.b;

                    d2Y += fD * pD[iStep];
                    dY.r += fD * diff.r;
                    dY.g += fD * diff.g;
                    dY.b += fD * diff.b;
                }

                // Move endpoints
                if (d2X > 0f)
                {
                    float f = -1f / d2X;
                    X.r += dX.r * f;
                    X.g += dX.g * f;
                    X.b += dX.b * f;
                }

                if (d2Y > 0f)
                {
                    float f = -1f / d2Y;
                    Y.r += dY.r * f;
                    Y.g += dY.g * f;
                    Y.b += dY.b * f;
                }

                float fEpsilon = (0.25f / 64.0f) * (0.25f / 64.0f);
                if ((dX.r * dX.r < fEpsilon) && (dX.g * dX.g < fEpsilon) && (dX.b * dX.b < fEpsilon) &&
                    (dY.r * dY.r < fEpsilon) && (dY.g * dY.g < fEpsilon) && (dY.b * dY.b < fEpsilon))
                {
                    break;
                }
            }

            RGBColour min = new RGBColour();
            min.r = X.r;
            min.g = X.g;
            min.b = X.b;

            RGBColour max = new RGBColour();
            max.r = Y.r;
            max.g = Y.g;
            max.b = Y.b;


            return new RGBColour[] { min, max };
        }

        private static byte[] CompressRGBTexel(byte[] texel, bool isDXT1, float alphaRef)
        {
            bool dither = true;
            int uSteps = 4;

            // Determine if texel is fully and entirely transparent. If so, can set to white and continue.
            if (isDXT1)
            {
                int uColourKey = 0;

                // Alpha stuff
                for (int i = 0; i < texel.Length; i += 4)
                {
                    RGBColour colour = ReadColourFromTexel(texel, i);
                    if (colour.a < alphaRef)
                        uColourKey++;
                }

                if (uColourKey == 16)
                {
                    // Entire texel is transparent

                    byte[] retval1 = new byte[8];
                    retval1[2] = byte.MaxValue;
                    retval1[3] = byte.MaxValue;

                    retval1[4] = byte.MaxValue;
                    retval1[5] = byte.MaxValue;
                    retval1[6] = byte.MaxValue;
                    retval1[7] = byte.MaxValue;

                    return retval1;
                }

                uSteps = uColourKey > 0 ? 3 : 4;
            }

            RGBColour[] Colour = new RGBColour[16];
            RGBColour[] Error = new RGBColour[16];

            int index = 0;
            for (int i = 0; i < texel.Length; i += 4)
            {
                index = i / 4;
                RGBColour current = ReadColourFromTexel(texel, i);

                if (dither)
                {
                    // Adjust for accumulated error
                    // This works by figuring out the error between the current pixel colour and the adjusted colour? Dunno what the adjustment is. Looks like a 5:6:5 range adaptation
                    // Then, this error is distributed across the "next" few pixels and not the previous.
                    current.r += Error[index].r;
                    current.g += Error[index].g;
                    current.b += Error[index].b;
                }


                // 5:6:5 range adaptation?
                Colour[index].r = (int)(current.r * 31f + .5f) * (1f / 31f);
                Colour[index].g = (int)(current.g * 63f + .5f) * (1f / 63f);
                Colour[index].b = (int)(current.b * 31f + .5f) * (1f / 31f);

                if (dither)
                {
                    // Calculate difference between current pixel colour and adapted pixel colour?
                    RGBColour diff = new RGBColour();
                    diff.r = current.a * (byte)(current.r - Colour[index].r);
                    diff.g = current.a * (byte)(current.g - Colour[index].g);
                    diff.b = current.a * (byte)(current.b - Colour[index].b);

                    // If current pixel is not at the end of a row
                    if ((index & 3) != 3)
                    {
                        Error[index + 1].r += diff.r * (7f / 16f);
                        Error[index + 1].g += diff.g * (7f / 16f);
                        Error[index + 1].b += diff.b * (7f / 16f);
                    }

                    // If current pixel is not in bottom row
                    if (index < 12)
                    {
                        // If current pixel IS at end of row
                        if ((index & 3) != 0)
                        {
                            Error[index + 3].r += diff.r * (3f / 16f);
                            Error[index + 3].g += diff.g * (3f / 16f);
                            Error[index + 3].b += diff.b * (3f / 16f);
                        }

                        Error[index + 4].r += diff.r * (5f / 16f);
                        Error[index + 4].g += diff.g * (5f / 16f);
                        Error[index + 4].b += diff.b * (5f / 16f);

                        // If current pixel is not at end of row
                        if ((index & 3) != 3)
                        {
                            Error[index + 5].r += diff.r * (1f / 16f);
                            Error[index + 5].g += diff.g * (1f / 16f);
                            Error[index + 5].b += diff.b * (1f / 16f);
                        }
                    }
                }

                Colour[index].r *= Luminance.r;
                Colour[index].g *= Luminance.g;
                Colour[index].b *= Luminance.b;
            }

            // Palette colours
            RGBColour ColourA, ColourB, ColourC, ColourD;
            ColourA = new RGBColour();
            ColourB = new RGBColour();
            ColourC = new RGBColour();
            ColourD = new RGBColour();

            // OPTIMISER
            RGBColour[] minmax = OptimiseRGB(Colour, uSteps);
            ColourA = minmax[0];
            ColourB = minmax[1];

            // Create interstitial colours?
            ColourC.r = ColourA.r * LuminanceInv.r;
            ColourC.g = ColourA.g * LuminanceInv.g;
            ColourC.b = ColourA.b * LuminanceInv.b;

            ColourD.r = ColourB.r * LuminanceInv.r;
            ColourD.g = ColourB.g * LuminanceInv.g;
            ColourD.b = ColourB.b * LuminanceInv.b;


            // Yeah...dunno
            uint wColourA = Encode565(ColourC);
            uint wColourB = Encode565(ColourD);

            if (uSteps == 4 && wColourA == wColourB)
            {
                var bits = new byte[8];
                var c2 = BitConverter.GetBytes(wColourA);
                var c1 = BitConverter.GetBytes(wColourB);  //////////////////////////////////////////////////// MIN MAX
                bits[0] = c2[0];
                bits[1] = c2[1];

                bits[2] = c1[0];
                bits[3] = c1[1];
                return bits;
            }

            ColourC = Decode565(wColourA);
            ColourD = Decode565(wColourB);

            ColourA.r = ColourC.r * Luminance.r;
            ColourA.g = ColourC.g * Luminance.g;
            ColourA.b = ColourC.b * Luminance.b;

            ColourB.r = ColourD.r * Luminance.r;
            ColourB.g = ColourD.g * Luminance.g;
            ColourB.b = ColourD.b * Luminance.b;


            // Create palette colours
            RGBColour[] step = new RGBColour[4];
            uint Min = 0;
            uint Max = 0;

            if ((uSteps == 3) == (wColourA <= wColourB))
            {
                Min = wColourA;
                Max = wColourB;
                step[0] = ColourA;
                step[1] = ColourB;
            }
            else
            {
                Min = wColourB;
                Max = wColourA;
                step[0] = ColourB;
                step[1] = ColourA;
            }

            uint[] psteps;

            if (uSteps == 3)
            {
                psteps = psteps3;

                step[2].r = step[0].r + (1f / 2f) * (step[1].r - step[0].r);
                step[2].g = step[0].g + (1f / 2f) * (step[1].g - step[0].g);
                step[2].b = step[0].b + (1f / 2f) * (step[1].b - step[0].b);
            }
            else
            {
                psteps = psteps4;

                // "step" appears to be the palette as this is the interpolation
                step[2].r = step[0].r + (1f / 3f) * (step[1].r - step[0].r);
                step[2].g = step[0].g + (1f / 3f) * (step[1].g - step[0].g);
                step[2].b = step[0].b + (1f / 3f) * (step[1].b - step[0].b);

                step[3].r = step[0].r + (2f / 3f) * (step[1].r - step[0].r);
                step[3].g = step[0].g + (2f / 3f) * (step[1].g - step[0].g);
                step[3].b = step[0].b + (2f / 3f) * (step[1].b - step[0].b);
            }



            // Calculating colour direction apparently
            RGBColour Dir = new RGBColour();
            Dir.r = step[1].r - step[0].r;
            Dir.g = step[1].g - step[0].g;
            Dir.b = step[1].b - step[0].b;

            int fsteps = uSteps - 1;
            float fscale = (wColourA != wColourB) ? (fsteps / (Dir.r * Dir.r + Dir.g * Dir.g + Dir.b * Dir.b)) : 0.0f;
            Dir.r *= fscale;
            Dir.g *= fscale;
            Dir.b *= fscale;


            // Encoding colours apparently
            Array.Clear(Error, 0, Error.Length);  // Clear error for next bit
            uint dw = 0;
            index = 0;
            for (int i = 0; i < texel.Length; i += 4)
            {
                index = i / 4;
                RGBColour current = ReadColourFromTexel(texel, i);

                if ((uSteps == 3) && (current.a < alphaRef))
                {
                    dw = (uint)((3 << 30) | (dw >> 2));
                    continue;
                }

                current.r *= Luminance.r;
                current.g *= Luminance.g;
                current.b *= Luminance.b;


                if (dither)
                {
                    // Error again
                    current.r += Error[index].r;
                    current.g += Error[index].g;
                    current.b += Error[index].b;
                }


                float fdot = (current.r - step[0].r) * Dir.r + (current.g - step[0].g) * Dir.g + (current.b - step[0].b) * Dir.b;

                uint iStep = 0;
                if (fdot <= 0f)
                    iStep = 0;
                else if (fdot >= fsteps)
                    iStep = 1;
                else
                    iStep = psteps[(int)(fdot + .5f)];

                dw = (iStep << 30) | (dw >> 2);   // THIS  IS THE MAGIC here. This is the "list" of indicies. Somehow...


                // Dither again
                if (dither)
                {
                    // Calculate difference between current pixel colour and adapted pixel colour?
                    RGBColour diff = new RGBColour();
                    diff.r = current.a * (byte)(current.r - step[iStep].r);
                    diff.g = current.a * (byte)(current.g - step[iStep].g);
                    diff.b = current.a * (byte)(current.b - step[iStep].b);

                    // If current pixel is not at the end of a row
                    if ((index & 3) != 3)
                    {
                        Error[index + 1].r += diff.r * (7f / 16f);
                        Error[index + 1].g += diff.g * (7f / 16f);
                        Error[index + 1].b += diff.b * (7f / 16f);
                    }

                    // If current pixel is not in bottom row
                    if (index < 12)
                    {
                        // If current pixel IS at end of row
                        if ((index & 3) != 0)
                        {
                            Error[index + 3].r += diff.r * (3f / 16f);
                            Error[index + 3].g += diff.g * (3f / 16f);
                            Error[index + 3].b += diff.b * (3f / 16f);
                        }

                        Error[index + 4].r += diff.r * (5f / 16f);
                        Error[index + 4].g += diff.g * (5f / 16f);
                        Error[index + 4].b += diff.b * (5f / 16f);

                        // If current pixel is not at end of row
                        if ((index & 3) != 3)
                        {
                            Error[index + 5].r += diff.r * (1f / 16f);
                            Error[index + 5].g += diff.g * (1f / 16f);
                            Error[index + 5].b += diff.b * (1f / 16f);
                        }
                    }
                }
            }

            byte[] retval = new byte[8];
            var colour1 = BitConverter.GetBytes(Min);
            var colour2 = BitConverter.GetBytes(Max);
            retval[0] = colour1[0];
            retval[1] = colour1[1];

            retval[2] = colour2[0];
            retval[3] = colour2[1];

            var indicies = BitConverter.GetBytes(dw);
            retval[4] = indicies[0];
            retval[5] = indicies[1];
            retval[6] = indicies[2];
            retval[7] = indicies[3];

            return retval;
        }
        #endregion RGB DXT

        private static int GetClosestValue(byte[] arr, byte c)
        {
            int min = int.MaxValue;
            int index = 0;
            int minIndex = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                int check = arr[i] - c;
                check = (check ^ (check >> 7)) - (check >> 7);
                if (check < min)
                {
                    min = check;
                    minIndex = index;
                }

                index++;
            }
            return minIndex;
        }

        /// <summary>
        /// Compresses single channel using Block Compression.
        /// </summary>
        /// <param name="texel">4 channel Texel to compress.</param>
        /// <param name="channel">0-3 (BGRA)</param>
        /// <param name="isSigned">true = uses alpha range -255 -- 255, else 0 -- 255</param>
        /// <returns>8 byte compressed texel.</returns>
        public static byte[] Compress8BitBlock(byte[] texel, int channel, bool isSigned)
        {
            // KFreon: Get min and max
            byte min = byte.MaxValue;
            byte max = byte.MinValue;
            int count = channel;
            for (int i = 0; i < 16; i++)
            {
                byte colour = texel[count];
                if (colour > max)
                    max = colour;
                else if (colour < min)
                    min = colour;

                count += 4; // skip to next entry in channel
            }

            // Build Palette
            byte[] Colours = Build8BitPalette(min, max, isSigned);

            // Compress Pixels
            ulong line = 0;
            count = channel;
            List<int> indicies = new List<int>();
            for (int i = 0; i < 16; i++)
            {
                byte colour = texel[count];
                int index = GetClosestValue(Colours, colour);
                indicies.Add(index);
                line |= (ulong)index << (i * 3); 
                count += 4;  // Only need 1 channel
            }

            byte[] CompressedBlock = new byte[8];
            byte[] compressed = BitConverter.GetBytes(line);
            CompressedBlock[0] = min;
            CompressedBlock[1] = max;
            for (int i = 2; i < 8; i++)
                CompressedBlock[i] = compressed[i - 2];

            return CompressedBlock;
        }
        #endregion Block Compression


        /// <summary>
        /// Ensures all Mipmaps are generated in MipMaps.
        /// </summary>
        /// <param name="MipMaps">MipMaps to check.</param>
        /// <param name="mergeAlpha">True = flattens alpha, directly affecting RGB.</param>
        /// <returns>Number of mipmaps present in MipMaps.</returns>
        internal static int BuildMipMaps(List<MipMap> MipMaps, bool mergeAlpha)
        {
            if (MipMaps?.Count == 0)
                return 0;

            MipMap currentMip = MipMaps[0];

            // KFreon: Check if mips required
            int estimatedMips = DDSGeneral.EstimateNumMipMaps(currentMip.Width, currentMip.Height);
            if (MipMaps.Count > 1)
                return estimatedMips;

            // KFreon: Half dimensions until one == 1.
            MipMap[] newmips = new MipMap[estimatedMips];

            Action<int> action = new Action<int>(item =>
            {
                int index = item;
                MipMap newmip;
                newmip = ImageEngine.Resize(currentMip, 1f / Math.Pow(2, index), mergeAlpha);
                newmips[index - 1] = newmip;
            });

            // Start at 1 to skip top mip
            if (ImageEngine.EnableThreading)
                Parallel.For(1, estimatedMips + 1, item => action(item));
            else
                for (int item = 1; item < estimatedMips + 1; item++)
                    action(item);

            MipMaps.AddRange(newmips);
            return estimatedMips;
        }


        /// <summary>
        /// Gets 4x4 texel block from stream.
        /// </summary>
        /// <param name="pixelData">Image pixels.</param>
        /// <param name="Width">Width of image.</param>
        /// <param name="Height">Height of image.</param>
        /// <returns>4x4 texel.</returns>
        internal static byte[] GetTexel(Stream pixelData, int Width, int Height)
        {
            byte[] texel = new byte[16 * 4]; // 16 pixels, 4 bytes per pixel

            // KFreon: Edge case for when dimensions are too small for texel
            int count = 0;
            if (Width < 4 || Height < 4)
            {
                for (int h = 0; h < Height; h++)
                    for (int w = 0; w < Width; w++)
                        for (int i = 0; i < 4; i++)
                        {
                            if (count >= 64)
                                return texel;
                            else
                                texel[count++] = (byte)pixelData.ReadByte();
                        }

                return texel;
            }

            // KFreon: Normal operation. Read 4x4 texel row by row.
            int bitsPerScanLine = 4 * Width;
            for (int i = 0; i < 64; i += 16)  // pixel rows
            {
                pixelData.Read(texel, i, 16);
                /*for (int j = 0; j < 16; j += 4)  // pixels in row
                    for (int k = 0; k < 4; k++) // BGRA
                        texel[i + j + k] = (byte)pixelData.ReadByte();*/

                pixelData.Seek(bitsPerScanLine - 4 * 4, SeekOrigin.Current);  // Seek to next line of texel
            }
                

            return texel;
        }


        #region Palette/Colour
        /// <summary>
        /// Reads a packed DXT colour into RGB
        /// </summary>
        /// <param name="colour">Colour to convert to RGB</param>
        /// <returns>RGB bytes</returns>
        private static byte[] ReadDXTColour(int colour)
        {
            // Read RGB 5:6:5 data
            var b = (colour & 0x1F);
            var g = (colour & 0x7E0) >> 5;
            var r = (colour & 0xF800) >> 11;


            // Expand to 8 bit data
            byte testr = (byte)(r << 3);
            byte testg = (byte)(g << 2);
            byte testb = (byte)(b << 3);
            return new byte[3] { testr, testg, testb };
        }


        /// <summary>
        /// Creates a packed DXT colour from RGB.
        /// </summary>
        /// <param name="r">Red byte.</param>
        /// <param name="g">Green byte.</param>
        /// <param name="b">Blue byte.</param>
        /// <returns>DXT Colour</returns>
        private static int BuildDXTColour(byte r, byte g, byte b)
        {
            // Compress to 5:6:5
            byte r1 = (byte)(r >> 3);
            byte g1 = (byte)(g >> 2);
            byte b1 = (byte)(b >> 3);

            return (r1 << 11) | (g1 << 5) | (b1);
        }


        /// <summary>
        /// Builds palette for 8 bit channel.
        /// </summary>
        /// <param name="min">First main colour (often actually minimum)</param>
        /// <param name="max">Second main colour (often actually maximum)</param>
        /// <param name="isSigned">true = sets signed alpha range (-254 -- 255), false = 0 -- 255</param>
        /// <returns>8 byte colour palette.</returns>
        internal static byte[] Build8BitPalette(byte min, byte max, bool isSigned)
        {
            byte[] Colours = new byte[8];
            Colours[0] = min;
            Colours[1] = max;

            // KFreon: Choose which type of interpolation is required
            if (min > max)
            {
                // KFreon: Interpolate other colours
                for (int i = 2; i < 8; i++)
                {
                    double test = min + (max - min) * (i - 1) / 7;
                    Colours[i] = (byte)test;
                }
            }
            else
            {
                // KFreon: Interpolate other colours and add Opacity or something...
                for (int i = 2; i < 6; i++)
                {
                    //double test = ((8 - i) * min + (i - 1) * max) / 5.0f;   // KFreon: "Linear interpolation". Serves me right for trusting a website without checking it...
                    double extratest = min + (max - min) * (i - 1) / 5;
                    Colours[i] = (byte)extratest;
                }
                Colours[6] = (byte)(isSigned ? -254 : 0);  // KFreon: snorm and unorm have different alpha ranges
                Colours[7] = 255;
            }

            return Colours;
        }

        

        
        public static int[] BuildRGBPalette(int Colour0, int Colour1, bool isDXT1)
        {
            int[] Colours = new int[4];

            Colours[0] = Colour0;
            Colours[1] = Colour1;

            var Colour0s = ReadDXTColour(Colour0);
            var Colour1s = ReadDXTColour(Colour1);


            // Interpolate other 2 colours
            if (Colour0 > Colour1)
            {
                var r1 = (byte)(2f / 3f * Colour0s[0] + 1f / 3f * Colour1s[0]);
                var g1 = (byte)(2f / 3f * Colour0s[1] + 1f / 3f * Colour1s[1]);
                var b1 = (byte)(2f / 3f * Colour0s[2] + 1f / 3f * Colour1s[2]);

                var r2 = (byte)(1f / 3f * Colour0s[0] + 2f / 3f * Colour1s[0]);
                var g2 = (byte)(1f / 3f * Colour0s[1] + 2f / 3f * Colour1s[1]);
                var b2 = (byte)(1f / 3f * Colour0s[2] + 2f / 3f * Colour1s[2]);

                Colours[2] = BuildDXTColour(r1, g1, b1);
                Colours[3] = BuildDXTColour(r2, g2, b2);
            }
            else
            {
                // KFreon: Only for dxt1
                var r = (byte)(1 / 2f * Colour0s[0] + 1 / 2f * Colour1s[0]);
                var g = (byte)(1 / 2f * Colour0s[1] + 1 / 2f * Colour1s[1]);
                var b = (byte)(1 / 2f * Colour0s[2] + 1 / 2f * Colour1s[2]);
            
                Colours[2] = BuildDXTColour(r, g, b);
                Colours[3] = 0;
            }
            return Colours;
        }
        #endregion Palette/Colour
        

        /// <summary>
        /// Estimates number of MipMaps for a given width and height EXCLUDING the top one.
        /// i.e. If output is 10, there are 11 mipmaps total.
        /// </summary>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>Number of mipmaps expected for image.</returns>
        internal static int EstimateNumMipMaps(int Width, int Height)
        {
            int limitingDimension = Width > Height ? Height : Width;
            return (int)Math.Log(limitingDimension, 2); // There's 10 mipmaps besides the main top one.
        }

        internal static long EnsureMipInImage(long streamLength, int mainWidth, int mainHeight, int desiredMaxDimension, Format format, out int numMipMaps, double mipIndex = -1)
        {
            // TODO: Is the other estimated mips required?

            if (mainWidth <= desiredMaxDimension && mainHeight <= desiredMaxDimension)
            {
                numMipMaps = EstimateNumMipMaps(mainWidth, mainHeight);
                return 128; // One mip only
            }


            int dependentDimension = mainWidth > mainHeight ? mainWidth : mainHeight;

            mipIndex = mipIndex == -1 ? Math.Log((dependentDimension / desiredMaxDimension), 2) - 1 : mipIndex;
            if (mipIndex < -1)
                throw new InvalidDataException($"Invalid dimensions for mipmapping. Got desired: {desiredMaxDimension} and dependent: {dependentDimension}");


            int requiredOffset = (int)ImageEngine.ExpectedImageSize(mipIndex, format, mainHeight, mainWidth);  // +128 for header

            int limitingDimension = mainWidth > mainHeight ? mainHeight : mainWidth;
            double newDimDivisor = limitingDimension * 1f / desiredMaxDimension;
            numMipMaps = EstimateNumMipMaps((int)(mainWidth / newDimDivisor), (int)(mainHeight / newDimDivisor));

            // KFreon: Something wrong with the count here by 1 i.e. the estimate is 1 more than it should be 
            if (format.SurfaceFormat == ImageEngineFormat.DDS_ARGB)
                requiredOffset -= 2;

            // Should only occur when an image has no mips
            if (streamLength < requiredOffset)
                return -1;

            return requiredOffset;
        }

        /// <summary>
        /// Read an 8 byte BC1 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC1 compressed stream.</param>
        /// <returns>BGRA channels.</returns>
        private static List<byte[]> DecompressBC1Block(Stream compressed)
        {
            return DDSGeneral.DecompressRGBBlock(compressed, true);
        }


        /// <summary>
        /// Compress texel to 8 byte BC1 compressed block.
        /// </summary>
        /// <param name="texel">4x4 BGRA group of pixels.</param>
        /// <returns>8 byte BC1 compressed block.</returns>
        private static byte[] CompressBC1Block(byte[] texel)
        {
            return CompressRGBTexel(texel, true, DXT1AlphaThreshold);
        }


        /// <summary>
        /// Reads a 16 byte BC2 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC2 compressed stream.</param>
        /// <returns>BGRA channels.</returns>
        private static List<byte[]> DecompressBC2Block(Stream compressed)
        {
            // KFreon: Read alpha into byte[] for maximum speed? Might be cos it's a MemoryStream...
            byte[] CompressedAlphas = new byte[8];
            compressed.Read(CompressedAlphas, 0, 8);
            int count = 0;

            // KFreon: Read alpha
            byte[] alpha = new byte[16];
            for (int i = 0; i < 16; i += 2)
            {
                //byte twoAlphas = (byte)compressed.ReadByte();
                byte twoAlphas = CompressedAlphas[count++];
                for (int j = 0; j < 2; j++)
                    alpha[i + j] = (byte)(twoAlphas << (j * 4));
            }


            // KFreon: Organise output by adding alpha channel (channel read in RGB block is empty)
            List<byte[]> DecompressedBlock = DDSGeneral.DecompressRGBBlock(compressed, false);
            DecompressedBlock[3] = alpha;
            return DecompressedBlock;
        }


        /// <summary>
        /// Reads a 16 byte BC3 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC3 compressed image stream.</param>
        /// <returns>List of BGRA channels.</returns>
        private static List<byte[]> DecompressBC3Block(Stream compressed)
        {
            byte[] alpha = DDSGeneral.Decompress8BitBlock(compressed, false);
            List<byte[]> DecompressedBlock = DDSGeneral.DecompressRGBBlock(compressed, false);
            DecompressedBlock[3] = alpha;
            return DecompressedBlock;
        }


        /// <summary>
        /// Decompresses ATI2 (BC5) block.
        /// </summary>
        /// <param name="compressed">Compressed data stream.</param>
        /// <returns>16 pixel BGRA channels.</returns>
        private static List<byte[]> DecompressATI2Block(Stream compressed)
        {
            byte[] red = DDSGeneral.Decompress8BitBlock(compressed, false);
            byte[] green = DDSGeneral.Decompress8BitBlock(compressed, false);
            List<byte[]> DecompressedBlock = new List<byte[]>();
            
            

            // KFreon: Alpha needs to be 255
            byte[] alpha = new byte[16];
            byte[] blue = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                alpha[i] = 0xFF;
                /*double r = red[i] / 255.0;
                double g = green[i] / 255.0;
                double test = 1 - (r * g);
                double anbs = Math.Sqrt(test);
                double ans = anbs * 255.0;*/
                blue[i] = (byte)0xFF;
            }

            DecompressedBlock.Add(blue);
            DecompressedBlock.Add(green);
            DecompressedBlock.Add(red);
            DecompressedBlock.Add(alpha);

            return DecompressedBlock;
        }


        /// <summary>
        /// Compresses texel to 16 byte BC5 block.
        /// </summary>
        /// <param name="texel">4x4 BGRA set of pixels.</param>
        /// <returns>16 byte BC5 block.</returns>
        private static byte[] CompressBC5Block(byte[] texel)
        {
            byte[] red = DDSGeneral.Compress8BitBlock(texel, 2, false);
            byte[] green = DDSGeneral.Compress8BitBlock(texel, 1, false);

            return red.Concat(green).ToArray(red.Length + green.Length);
        }


        /// <summary>
        /// Decompresses an ATI1 (BC4) block.
        /// </summary>
        /// <param name="compressed">Compressed data stream.</param>
        /// <returns>BGRA channels (16 bits each)</returns>
        private static List<byte[]> DecompressATI1(Stream compressed)
        {
            byte[] channel = DDSGeneral.Decompress8BitBlock(compressed, false);
            List<byte[]> DecompressedBlock = new List<byte[]>();

            // KFreon: All channels are the same to make grayscale.
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(channel);

            // KFreon: Alpha needs to be 255
            byte[] alpha = new byte[16];
            for (int i = 0; i < 16; i++)
                alpha[i] = 0xFF;
            DecompressedBlock.Add(alpha);
            return DecompressedBlock;
        }


        /// <summary>
        /// Compress texel to 8 byte BC4 compressed block.
        /// </summary>
        /// <param name="texel">4x4 BGRA set of pixels.</param>
        /// <returns>8 byte BC4 compressed block.</returns>
        private static byte[] CompressBC4Block(byte[] texel)
        {
            return DDSGeneral.Compress8BitBlock(texel, 2, false);
        }



        /// <summary>
        /// Compress texel to 16 byte BC3 compressed block.
        /// </summary>
        /// <param name="texel">4x4 BGRA set of pixels.</param>
        /// <returns>16 byte BC3 compressed block.</returns>
        private static byte[] CompressBC3Block(byte[] texel)
        {
            // Compress Alpha
            byte[] Alpha = DDSGeneral.Compress8BitBlock(texel, 3, false);

            // Compress Colour
            byte[] RGB = DDSGeneral.CompressRGBTexel(texel, false, 0f);

            return Alpha.Concat(RGB).ToArray(Alpha.Length + RGB.Length);
        }


        /// <summary>
        /// Compress texel to 16 byte BC2 compressed block.
        /// </summary>
        /// <param name="texel">4x4 BGRA set of pixels.</param>
        /// <returns>16 byte BC2 compressed block.</returns>
        private static byte[] CompressBC2Block(byte[] texel)
        {
            // Compress Alpha
            byte[] Alpha = new byte[8];
            for (int i = 3; i < 64; i += 8)  // Only read alphas
            {
                byte twoAlpha = 0;
                for (int j = 0; j < 8; j += 4)
                    twoAlpha |= (byte)(texel[i + j] << j);
                Alpha[i / 8] = twoAlpha;
            }

            // Compress Colour
            byte[] RGB = DDSGeneral.CompressRGBTexel(texel, false, 0f);

            return Alpha.Concat(RGB).ToArray(Alpha.Length + RGB.Length);
        }

        internal static bool Save(List<MipMap> MipMaps, Stream Destination, Format format)
        {
            DDSGeneral.DDS_HEADER header = DDSGeneral.Build_DDS_Header(MipMaps.Count, MipMaps[0].Height, MipMaps[0].Width, format.SurfaceFormat);

            Func<byte[], byte[]> Compressor = null;
            Action<Stream, Stream, int, int> PixelWriter = null;


            switch (format.SurfaceFormat)
            {
                case ImageEngineFormat.DDS_ARGB:   // Way different method
                    using (BinaryWriter writer = new BinaryWriter(Destination, Encoding.Default, true))
                        DDSGeneral.Write_DDS_Header(header, writer);

                    try
                    {
                        unsafe
                        {
                            for (int m = 0; m < MipMaps.Count; m++)
                            {
                                var stream = new UnmanagedMemoryStream((byte*)MipMaps[m].BaseImage.BackBuffer.ToPointer(), 4 * MipMaps[m].Width * MipMaps[m].Height);
                                stream.CopyTo(Destination);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        throw;
                    }
                    

                    return true;
                case ImageEngineFormat.DDS_A8L8:
                    PixelWriter = WriteA8L8Pixel;
                    break;
                case ImageEngineFormat.DDS_RGB:
                    PixelWriter = WriteRGBPixel;
                    break;
                case ImageEngineFormat.DDS_ATI1:
                    Compressor = CompressBC4Block;
                    break;
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    Compressor = CompressBC5Block;
                    break;
                case ImageEngineFormat.DDS_DXT1:
                    Compressor = CompressBC1Block;
                    break;
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                    Compressor = CompressBC2Block;
                    break;
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    Compressor = CompressBC3Block;
                    break;
                case ImageEngineFormat.DDS_G8_L8:
                    PixelWriter = WriteG8_L8Pixel;
                    break;
                case ImageEngineFormat.DDS_V8U8:
                    PixelWriter = WriteV8U8Pixel;
                    break;
                default:
                    throw new Exception("ahaaha");
            }


            // KFreon: Set to DDS pixel writer. Needs to be here or the Compressor function is null (due to inclusion or whatever it's called)
            if (PixelWriter == null)
                PixelWriter = (writer, pixels, width, height) =>  
                {
                    byte[] texel = DDSGeneral.GetTexel(pixels, width, height);
                    byte[] CompressedBlock = Compressor(texel);
                    writer.Write(CompressedBlock, 0, CompressedBlock.Length);
                };

            return DDSGeneral.WriteDDS(MipMaps, Destination, header, PixelWriter, format.IsBlockCompressed);
        }


        private static void WriteG8_L8Pixel(Stream writer, Stream pixels, int unused1, int unused2)
        {
            // BGRA
            byte[] colours = new byte[3];
            pixels.Read(colours, 0, 3);
            pixels.Position++;  // Skip alpha

            // KFreon: Weight colours to look proper. Dunno if this affects things but anyway...Got weightings from ATi Compressonator
            int b1 = (int)(colours[0] * 3 * 0.082);
            int g1 = (int)(colours[1] * 3 * 0.6094);
            int r1 = (int)(colours[2] * 3 * 0.3086);

            int test = (int)((b1 + g1 + r1) / 3f);
            writer.WriteByte((byte)test);
        }

        private static void WriteV8U8Pixel(Stream writer, Stream pixels, int unused1, int unused2)
        {
            // BGRA
            pixels.Position++; // No blue
            /*var bytes = pixels.ReadBytesFromStream(2);
            writer.Write(bytes, 0, 2);*/

            byte green = (byte)(pixels.ReadByte() + V8U8Adjust);
            byte red = (byte)(pixels.ReadByte() + V8U8Adjust);
            writer.Write(new byte[] { red, green }, 0, 2);
            pixels.Position++;    // No alpha
        }

        private static void WriteA8L8Pixel(Stream writer, Stream pixels, int unused1, int unused2)
        {
            // BGRA
            // First 3 channels are the same value, so just use the last one.
            pixels.Position += 2;
            writer.ReadFrom(pixels, 2); 
        }

        private static void WriteRGBPixel(Stream writer, Stream pixels, int unused1, int unused2)
        {
            // BGRA
            var bytes = pixels.ReadBytes(3);
            writer.Write(bytes, 0, bytes.Length);
            pixels.Position++;
        }
    }
}
