using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VGAudio.Cli;
using VGAudio.Cli.Metadata;
using Xv2CoreLib.HCA;
using Xv2CoreLib.Resource;

namespace AudioCueEditor.Audio
{
    public static class HCA
    {
        public static WavStream Decode(byte[] hcaBytes)
        {
            byte[] wavBytes;

            using(MemoryStream stream = new MemoryStream(hcaBytes))
            {
                wavBytes = ConvertStream.ConvertFile(new Options(), stream, FileType.Hca, FileType.Wave);
            }

            return new WavStream(wavBytes);
        }

        public static byte[] Encode(byte[] wavBytes)
        {
            using(var wavStream = new MemoryStream(wavBytes))
            {
                return ConvertStream.ConvertFile(new Options(), wavStream, FileType.Wave, FileType.Hca);
            }
        }
        
        /// <summary>
        /// Loop the track from the specified start and end.
        /// </summary>
        public static byte[] EncodeLoop(byte[] hcaBytes, bool loop, double start, double end)
        {
            return HcaMetadata.SetLoop(hcaBytes, loop, start, end, 0);
        }
        
        public static byte[] EncodeLoop(byte[] hcaBytes, bool loop)
        {
            HcaMetadata metadata = new HcaMetadata(hcaBytes);
            if (metadata.HasLoopData && loop) return hcaBytes; //Reuse existing loop data
            return HcaMetadata.SetLoop(hcaBytes, loop, 0, metadata.DurationSeconds, 0);
        }
    }

    public static class BC_WAV
    {
        public static WavStream Decode(byte[] bytes)
        {
            byte[] wavBytes;

            using (MemoryStream stream = new MemoryStream(bytes))
            {
                wavBytes = ConvertStream.ConvertFile(new Options(), stream, FileType.Bcwav, FileType.Wave);
            }

            return new WavStream(wavBytes);
        }

        public static byte[] Encode(byte[] wavBytes)
        {
            using (var wavStream = new MemoryStream(wavBytes))
            {
                return ConvertStream.ConvertFile(new Options(), wavStream, FileType.Wave, FileType.Bcwav);
            }
        }
    }

    public static class DSP
    {
        public static WavStream Decode(byte[] bytes)
        {
            byte[] wavBytes;

            using (MemoryStream stream = new MemoryStream(bytes))
            {
                wavBytes = ConvertStream.ConvertFile(new Options(), stream, FileType.Dsp, FileType.Wave);
            }

            return new WavStream(wavBytes);
        }

        public static byte[] Encode(byte[] wavBytes)
        {
            using (var wavStream = new MemoryStream(wavBytes))
            {
                return ConvertStream.ConvertFile(new Options(), wavStream, FileType.Wave, FileType.Dsp);
            }
        }
    }
}
