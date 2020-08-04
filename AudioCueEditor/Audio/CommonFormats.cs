using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;
using NAudio.Wave;

namespace AudioCueEditor.Audio
{
    /// <summary>
    /// Static methods for converting common audio formats to a WavStream.
    /// </summary>
    public static class CommonFormatsConverter
    {
        /// <summary>
        /// Convert common audio formats to wav.
        /// </summary>
        /// <returns></returns>
        public static byte[] ConvertToWav(string path)
        {
            byte[] outBytes;

            using (var reader = new MediaFoundationReader(path))
            {
                using (var stream = new MemoryStream())
                {
                    WaveFileWriter.WriteWavFileToStream(stream, reader);
                    outBytes = stream.ToArray();
                }
            }

            return outBytes;
        }
    }
}
