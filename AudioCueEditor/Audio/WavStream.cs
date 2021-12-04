using NAudio.Wave;
using System.IO;

namespace AudioCueEditor.Audio
{
    public class WavStream
    {

        public LoopStream waveStream { get; private set; }
        public Stream stream { get; private set; }
        public bool IsValid { get { return (waveStream != null); } }

        public WavStream(byte[] _wavBytes)
        {
            Init(_wavBytes);
        }

        ~WavStream()
        {
            Dispose();
        }

        public void Init(byte[] wavFile)
        {
            Dispose();
            stream = new MemoryStream(wavFile);
            waveStream = new LoopStream(WaveFormatConversionStream.CreatePcmStream(new WaveFileReader(stream)));
        }

        public void Dispose()
        {
            try
            {
                if (waveStream != null)
                    waveStream.Dispose();
            }
            finally
            {
                waveStream = null;
            }

            try
            {
                if (stream != null)
                    stream.Dispose();
            }
            finally
            {
                stream = null;
            }

        }
        
    }
}
