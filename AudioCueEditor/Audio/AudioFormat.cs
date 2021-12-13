using NAudio.Wave;
using System.IO;
using VGAudio.Cli;
using Xv2CoreLib.HCA;

namespace AudioCueEditor.Audio
{
    public static class WAV
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

    public static class HCA
    {
        public static byte[] DecodeToWav(byte[] hcaBytes)
        {
            using (MemoryStream stream = new MemoryStream(hcaBytes))
            {
                return ConvertStream.ConvertFile(new Options(), stream, FileType.Hca, FileType.Wave);
            }
        }

        public static WavStream DecodeToWavStream(byte[] hcaBytes)
        {
            return new WavStream(DecodeToWav(hcaBytes));
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
            using (var wavStream = new MemoryStream(wavBytes))
                return ConvertStream.ConvertFile(new Options(), wavStream, FileType.Wave, FileType.Adx);
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
            using (var stream = new MemoryStream(wavBytes))
            {
                return ConvertStream.ConvertFile(new Options(), stream, FileType.Wave, FileType.Atrac9);
            }
        }
    }

}
