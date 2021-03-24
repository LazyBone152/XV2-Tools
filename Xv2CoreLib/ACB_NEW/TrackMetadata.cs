using System;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.ACB_NEW
{
    public struct TrackMetadata
    {
        #region Constants
        private const int HCA_SIGNATURE = 0x00414348;
        private const ushort ADX_SIGNATURE = 0x80;

        //HCA specific
        private const int FMT_SIGNATURE = 0x00746D66;
        #endregion

        public bool IsValidAudioFile { get; private set; }

        public byte Channels { get; private set; }
        public uint SampleRate { get; private set; }
        public uint NumSamples { get; private set; }

        //Duration
        public uint DurationSeconds { get { return (NumSamples != 0 && SampleRate != 0) ? NumSamples / SampleRate : 0; } }
        public TimeSpan Duration { get { return new TimeSpan(0, 0, 0, (int)DurationSeconds); } }

        public TrackMetadata(byte[] bytes)
        {
            IsValidAudioFile = false;
            Channels = 0;
            SampleRate = 0;
            NumSamples = 0;

            try
            {
                if (BitConverter.ToUInt16(bytes, 0) == ADX_SIGNATURE)
                {
                    //Is ADX
                    IsValidAudioFile = true;
                    Channels = bytes[7];
                    SampleRate = BigEndianConverter.ReadUInt32(bytes, 8);
                    NumSamples = BigEndianConverter.ReadUInt32(bytes, 12);
                }
                else if (BitConverter.ToInt32(bytes, 0) == HCA_SIGNATURE)
                {
                    IsValidAudioFile = true;
                    int dataOffset = BigEndianConverter.ReadUInt16(bytes, 6);
                    int fmtOffset = bytes.IndexOfValue(FMT_SIGNATURE, 0, dataOffset, false);

                    if (fmtOffset != -1)
                    {
                        Channels = bytes[fmtOffset + 4];
                        SampleRate = BigEndianConverter.ReadUInt16(bytes, fmtOffset + 6);
                        int blockCount = BigEndianConverter.ReadInt32(bytes, fmtOffset + 8);
                        NumSamples = (uint)(blockCount * 1024);
                    }
                }
                else
                {
                    //DSP, which has no signature... thanks nintendo!
                    NumSamples = BigEndianConverter.ReadUInt32(bytes, 0);
                    SampleRate = BigEndianConverter.ReadUInt32(bytes, 8);
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

            if(Channels > 10 || SampleRate < 0 || NumSamples < 0)
            {
                //Values make no sense
                IsValidAudioFile = false;
                Channels = 0;
                SampleRate = 0;
                NumSamples = 0;
            }
        }

    }


}
