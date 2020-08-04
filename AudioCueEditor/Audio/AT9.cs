using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VGAudio.Cli;

namespace AudioCueEditor.Audio
{
    public static class AT9
    {
        public static WavStream Decode(byte[] at9Bytes)
        {
            byte[] wavBytes;

            using (MemoryStream stream = new MemoryStream(at9Bytes))
            {
                wavBytes = ConvertStream.ConvertFile(new Options(), stream, FileType.Atrac9, FileType.Wave);
            }

            return new WavStream(wavBytes);
        }

        public static byte[] Encode(byte[] wavBytes)
        {
            using(var stream = new MemoryStream(wavBytes))
            {
                return ConvertStream.ConvertFile(new Options(), stream, FileType.Wave, FileType.Atrac9);
            }
        }
    }
}
