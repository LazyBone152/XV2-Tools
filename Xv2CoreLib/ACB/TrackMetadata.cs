using System;
using System.IO;
using VGAudio;
using VGAudio.Cli;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.ACB
{
    [Serializable]
    public class TrackMetadata
    {
        private const ushort ADX_SIGNATURE = 0x80;

        public bool IsValidAudioFile { get; private set; }

        //Fmt
        public byte Channels { get; private set; }
        public ushort SampleRate { get; private set; }
        public int BlockCount { get; private set; }
        public int NumSamples { get; private set; }

        public uint Milliseconds { get { return (uint)(DurationSeconds * 1000.0); } }
        public double DurationSeconds { get { return BlocksToSeconds((uint)BlockCount, SampleRate); } } //Seconds
        public TimeSpan Duration { get { return new TimeSpan(0, 0, 0, (int)DurationSeconds); } }

        //Loop
        public bool HasLoopData { get; private set; }
        public uint LoopStart { get; private set; }
        public uint LoopEnd { get; private set; }

        public double LoopStartSeconds { get { return BlocksToSeconds(LoopStart, SampleRate); } }
        public double LoopEndSeconds { get { return BlocksToSeconds(LoopEnd, SampleRate); } }

        public uint LoopStartMs => BlocksToMs(LoopStartMs, SampleRate);
        public uint LoopEndMs => BlocksToMs(LoopEndMs, SampleRate);

        public uint LoopStartSamples { get { return BlocksToSamples(LoopStart); } }
        public uint LoopEndSamples { get { return BlocksToSamples(LoopEnd); } }

        public TrackMetadata() { }

        public TrackMetadata(byte[] hcaBytes)
        {
            if(hcaBytes?.Length > 100)
                SetValues(hcaBytes);
        }

        private void SetValues(byte[] bytes)
        {
            VGAudio.Containers.Hca.HcaReader reader = new VGAudio.Containers.Hca.HcaReader();
            var hca = reader.ParseFile(bytes);

            if (hca != null)
            {
                HasLoopData = hca.Hca.Looping;
                LoopStart = (uint)hca.Hca.LoopStartFrame;
                LoopEnd = (uint)hca.Hca.LoopEndFrame;

                BlockCount = hca.Hca.FrameCount;
                Channels = (byte)hca.Hca.ChannelCount;
                SampleRate = (ushort)hca.Hca.SampleRate;
                NumSamples = hca.Hca.SampleCount;

                IsValidAudioFile = true;
            }
            else
            {
                try
                {
                    //Try unencrypted reads of other formats

                    if (BitConverter.ToUInt16(bytes, 0) == ADX_SIGNATURE)
                    {
                        //Is ADX
                        IsValidAudioFile = true;
                        Channels = bytes[7];
                        SampleRate = (ushort)BigEndianConverter.ReadUInt32(bytes, 8);
                        NumSamples = (int)BigEndianConverter.ReadUInt32(bytes, 12);
                    }
                    else
                    {
                        //For DSP, which has no signature (?)

                        NumSamples = (int)BigEndianConverter.ReadUInt32(bytes, 0);
                        SampleRate = (ushort)BigEndianConverter.ReadUInt32(bytes, 8);
                        Channels = (byte)BigEndianConverter.ReadUInt16(bytes, 74);

                        if (Channels == 0)
                            Channels = 1;
                    }
                }
                catch
                {
                    IsValidAudioFile = false;
                    Channels = 0;
                    SampleRate = 0;
                    NumSamples = 0;
                }
            }
        }

        //Helpers
        private static uint BlocksToMs(uint blocks, ushort sampleRate)
        {
            return (uint)((blocks * 1024.0) / sampleRate * 1000f);
        }

        private static double BlocksToSeconds(uint blocks, ushort sampleRate)
        {
            return (blocks * 1024.0) / sampleRate;
        }

        private static uint BlocksToSamples(uint blocks)
        {
            return (uint)(blocks * 1024.0);
        }


        /// <summary>
        /// Loop the track from the specified start and end.
        /// </summary>
        public static byte[] EncodeHcaLoop(byte[] hcaBytes, bool loop, int startMs, int endMs)
        {
            using (MemoryStream stream = new MemoryStream(hcaBytes))
            {
                var options = new Options();

                options.NoLoop = !loop;
                options.Loop = loop;
                options.LoopStart = startMs;
                options.LoopEnd = endMs;

                ConvertStatics.SetLoop(loop, startMs, endMs);

                return ConvertStream.ConvertFile(options, stream, FileType.Hca, FileType.Hca);
            }
        }
    }


}
