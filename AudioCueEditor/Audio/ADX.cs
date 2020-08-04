using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VGAudio.Cli;

namespace AudioCueEditor.Audio
{
    public static class ADX
    {
        public static WavStream Decode(byte[] adxBytes)
        {
            byte[] wavBytes;

            using (MemoryStream stream = new MemoryStream(adxBytes))
            {
                wavBytes = ConvertStream.ConvertFile(new Options(), stream, FileType.Adx, FileType.Wave);
            }

            return new WavStream(wavBytes);
        }

        public static byte[] Encode(byte[] wavBytes)
        {
            using(var wavStream = new MemoryStream(wavBytes))
                return ConvertStream.ConvertFile(new Options(), wavStream, FileType.Wave, FileType.Adx);
        }
    }
}
