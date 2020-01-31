using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.IO;
using Microsoft.Win32;

namespace UsefulThings
{
    /// <summary>
    /// General C# helpers.
    /// </summary>
    public static class General
    {
        /// <summary>
        /// Does bit conversion from streams
        /// </summary>
        public static class StreamBitConverter
        {
            /// <summary>
            /// Reads a UInt32 from a stream at given offset.
            /// </summary>
            /// <param name="stream">Stream to read from.</param>
            /// <param name="offset">Offset to start reading from in stream.</param>
            /// <returns>Number read from stream.</returns>
            public static UInt32 ToUInt32(Stream stream, int offset)
            {
                // KFreon: Seek to specified offset
                byte[] fourBytes = new byte[4];
                stream.Seek(offset, SeekOrigin.Begin);

                // KFreon: Read 4 bytes from stream at offset and convert to UInt32
                stream.Read(fourBytes, 0, 4);
                UInt32 retval = BitConverter.ToUInt32(fourBytes, 0);

                // KFreon: Clear array and reset stream position
                fourBytes = null;
                return retval;
            }
        }

        /// <summary>
        /// Gets DPI scaling factor for main monitor from registry keys. 
        /// Returns 1 if key is unavailable.
        /// </summary>
        /// <returns>Returns scale or 1 if not found.</returns>
        public static double GetDPIScalingFactorFROM_REGISTRY()
        {
            var currentDPI = (int)(Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics", "AppliedDPI", 96) ?? 96);
            return currentDPI / 96.0;
        }


        /// <summary>
        /// Gets DPI Scaling factor for monitor app is currently on. 
        /// NOT actual DPI, the scaling factor relative to standard 96 DPI.
        /// </summary>
        /// <param name="current">Main window to get DPI for.</param>
        /// <returns>DPI scaling factor.</returns>
        public static double GetDPIScalingFactorFOR_CURRENT_MONITOR(Window current)
        {
            PresentationSource source = PresentationSource.FromVisual(current);
            Matrix m = source.CompositionTarget.TransformToDevice;
            return m.M11;
        }

        /// <summary>
        /// Returns actual DPI of given visual object. Application DPI is constant across it's visuals.
        /// </summary>
        /// <param name="anyVisual">Any visual from the Application UI to test.</param>
        /// <returns>DPI of Application.</returns>
        public static int GetAbsoluteDPI(Visual anyVisual)
        {
            PresentationSource source = PresentationSource.FromVisual(anyVisual);
            if (source != null)
                return (int)(96.0 * source.CompositionTarget.TransformToDevice.M11);

            return 96;
        }


        /// <summary>
        /// Changes a filename in a full filepath string.
        /// </summary>
        /// <param name="fullPath">Original full filepath.</param>
        /// <param name="newFilenameWithoutExt">New filename to use.</param>
        /// <returns>Filepath with changed filename.</returns>
        public static string ChangeFilename(string fullPath, string newFilenameWithoutExt)
        {
            return fullPath.Replace(Path.GetFileNameWithoutExtension(fullPath), newFilenameWithoutExt);
        }


        /// <summary>
        /// Ensures the first character of a string is in Upper Case
        /// </summary>
        /// <param name="s">String to convert first to upper case.</param>
        /// <returns>New string with upper case start</returns>
        public static string UpperCaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        /// <summary>
        /// Determines if number is a power of 2. 
        /// </summary>
        /// <param name="number">Number to check.</param>
        /// <returns>True if number is a power of 2.</returns>
        public static bool IsPowerOfTwo(int number)
        {
            return (number & (number - 1)) == 0;
        }


        /// <summary>
        /// Determines if number is a power of 2. 
        /// </summary>
        /// <param name="number">Number to check.</param>
        /// <returns>True if number is a power of 2.</returns>
        public static bool IsPowerOfTwo(long number)
        {
            return (number & (number - 1)) == 0;
        }


        /// <summary>
        /// Rounds number to the nearest power of 2. Doesn't use Math. Uses bitshifting (not my method).
        /// </summary>
        /// <param name="number">Number to round.</param>
        /// <returns>Nearest power of 2.</returns>
        public static int RoundToNearestPowerOfTwo(int number)
        {
            // KFreon: Gets next Highest power
            int next = number - 1;
            next |= next >> 1;
            next |= next >> 2;
            next |= next >> 4;
            next |= next >> 8;
            next |= next >> 16;
            next++;

            // KFreon: Compare previous and next for the closest
            int prev = next >> 1;
            return number - prev > next - number ? next : prev;
        }


        /// <summary>
        /// Extends on substring functionality to extract string between two other strings. e.g. ExtractString("indigo", "in", "go") == "di"
        /// </summary>
        /// <param name="str">String to extract from.</param>
        /// <param name="left">Extraction starts after this string.</param>
        /// <param name="right">Extraction ends before this string.</param>
        /// <returns>String between left and right strings.</returns>
        public static string ExtractString(string str, string left, string right)
        {
            int startIndex = str.IndexOf(left) + left.Length;
            int endIndex = str.IndexOf(right, startIndex);
            return str.Substring(startIndex, endIndex - startIndex);
        }


        /// <summary>
        /// Extends on substring functionality to extract string between a delimiter. e.g. ExtractString("I like #snuffles# and things", "#") == "snuffles"
        /// </summary>
        /// <param name="str">String to extract from.</param>
        /// <param name="enclosingElement">Element to extract between. Must be present twice in str.</param>
        /// <returns>String between two enclosingElements.</returns>
        public static string ExtractString(string str, string enclosingElement)
        {
            return ExtractString(str, enclosingElement, enclosingElement);
        }


        #region Stream Compression/Decompression
        /// <summary>
        /// Decompresses stream using GZip. Returns decompressed Stream.
        /// Returns null if stream isn't compressed.
        /// </summary>
        /// <param name="compressedStream">Stream compressed with GZip.</param>
        public static MemoryStream DecompressStream(Stream compressedStream)
        {
            MemoryStream newStream = new MemoryStream();
            compressedStream.Seek(0, SeekOrigin.Begin);

            GZipStream Decompressor = null;
            try
            {
                Decompressor = new GZipStream(compressedStream, CompressionMode.Decompress, true);
                Decompressor.CopyTo(newStream);
            }
            catch (InvalidDataException invdata)
            {
                return null;
            }
            catch(Exception e)
            {
                throw;
            }
            finally
            {
                if (Decompressor != null)
                    Decompressor.Dispose();
            }
            
            return newStream;
        }


        /// <summary>
        /// Compresses stream with GZip. Returns new compressed stream.
        /// </summary>
        /// <param name="DecompressedStream">Stream to compress.</param>
        /// <param name="compressionLevel">Level of compression to use.</param>
        public static MemoryStream CompressStream(Stream DecompressedStream, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            MemoryStream ms = new MemoryStream();
            using (GZipStream Compressor = new GZipStream(ms, compressionLevel, true))
            {
                DecompressedStream.Seek(0, SeekOrigin.Begin);
                DecompressedStream.CopyTo(Compressor);
            }

            return ms;
        }
        #endregion Stream Compression/Decompression
        


        /// <summary>
        /// Converts given double to filesize with appropriate suffix.
        /// </summary>
        /// <param name="size">Size in bytes.</param>
        /// <param name="FullSuffix">True = Bytes, KiloBytes, etc. False = B, KB, etc</param>
        public static string GetFileSizeAsString(double size, bool FullSuffix = false)
        {
            string[] sizes = null;
            if (FullSuffix)
                sizes = new string[] { "Bytes", "Kilobytes", "Megabytes", "Gigabytes" };
            else
                sizes = new string[] { "B", "KB", "MB", "GB" };
            
            int order = 0;
            while (size >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                size = size / 1024;
            }

            return size.ToString("F1") + " " + sizes[order];
        }


        /// <summary>
        /// Gets file extensions as filter string for SaveFileDialog and OpenFileDialog as a SINGLE filter entry.
        /// </summary>
        /// <param name="exts">List of extensions to use.</param>
        /// <param name="filterName">Name of filter entry. e.g. 'Images|*.jpg;*.bmp...', Images is the filter name</param>
        /// <returns>Filter string from extensions.</returns>
        public static string GetExtsAsFilter(List<string> exts, string filterName)
        {
            StringBuilder sb = new StringBuilder(filterName + "|");
            foreach (string str in exts)
                sb.Append("*" + str + ";");
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }


        /// <summary>
        /// Gets file extensions as filter string for SaveFileDialog and OpenFileDialog as MULTIPLE filter entries.
        /// </summary>
        /// <param name="exts">List of file extensions. Must have same number as filterNames.</param>
        /// <param name="filterNames">List of file names. Must have same number as exts.</param>
        /// <returns>Filter string of names and extensions.</returns>
        public static string GetExtsAsFilter(List<string> exts, List<string> filterNames)
        {
            // KFreon: Flip out if number of extensions is different to number of names of said extensions
            if (exts.Count != filterNames.Count)
                return null;

            // KFreon: Build filter string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < exts.Count; i++)
                sb.Append(filterNames[i] + "|*" + exts[i] + "|");
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }


        /// <summary>
        /// Gets version of assembly calling this function.
        /// </summary>
        /// <returns>String of assembly version.</returns>
        public static string GetCallingVersion()
        {
            return Assembly.GetCallingAssembly().GetName().Version.ToString();
        }


        /// <summary>
        /// Gets version of main assembly that started this process.
        /// </summary>
        /// <returns></returns>
        public static string GetStartingVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version.ToString();
        }


        /// <summary>
        /// Gets location of assembly calling this function.
        /// </summary>
        /// <returns>Path to location.</returns>
        public static string GetExecutingLoc()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        }


        #region File IO
        /// <summary>
        /// Read text from file as single string.
        /// </summary>
        /// <param name="filename">Path to filename.</param>
        /// <param name="result">Contents of file.</param>
        /// <returns>Null if successful, error as string otherwise.</returns>
        public static string ReadTextFromFile(string filename, out string result)
        {
            result = null;
            string err = null;

            // Try to read file, but fail safely if necessary
            try
            {
                if (filename.isFile())
                    result = File.ReadAllText(filename);
                else
                    err = "Not a file.";
            }
            catch (Exception e)
            {
                err = e.Message;
            }

            return err;
        }


        /// <summary>
        /// Reads lines of file into List.
        /// </summary>
        /// <param name="filename">File to read from.</param>
        /// <param name="Lines">Contents of file.</param>
        /// <returns>Null if success, error message otherwise.</returns>
        public static string ReadLinesFromFile(string filename, out List<string> Lines)
        {
            Lines = null;
            string err = null;

            try
            {
                // KFreon: Only bother if it is a file
                if (filename.isFile())
                {
                    string[] lines = File.ReadAllLines(filename);
                    Lines = lines.ToList(lines.Length);
                }
                    
                else
                    err = "Not a file.";
            }
            catch (Exception e)
            {
                err = e.Message;
            }

            return err;
        }
        
         /// <summary>
        /// Gets external image data as byte[] with some buffering i.e. retries if fails up to 20 times.
        /// </summary>
        /// <param name="file">File to get data from.</param>
        /// <param name="OnFailureSleepTime">Time (in ms) between attempts for which to sleep.</param>
        /// <param name="retries">Number of attempts to read.</param>
        /// <returns>byte[] of image.</returns>
        public static byte[] GetExternalData(string file, int retries = 20, int OnFailureSleepTime = 300)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    // KFreon: Try readng file to byte[]
                    return File.ReadAllBytes(file);
                }
                catch (IOException e)
                {
                    // KFreon: Sleep for a bit and try again
                    System.Threading.Thread.Sleep(OnFailureSleepTime);
                    Console.WriteLine(e.Message);
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    Console.WriteLine();
                }
            }
            return null;
        }
        #endregion File IO
    }
}
