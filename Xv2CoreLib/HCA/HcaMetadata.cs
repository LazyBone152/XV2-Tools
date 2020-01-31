using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.HCA
{
    //For reading the metadata from HCA files, not an actual parser

    public struct HcaMetadata
    {
        //Fmt
        public byte Channels { get; private set; }
        public ushort SampleRate { get; private set; }
        public int BlockCount { get; private set; }
        public float DurationSeconds { get { return NumSamples / (float)SampleRate; } } //Seconds
        public TimeSpan Duration { get { return new TimeSpan(0, 0, 0, (int)DurationSeconds); } }
        public int NumSamples { get { return 0x80 * 8 * BlockCount; } } //Does not match up with sample count declared in ACB... but is very close

        //Loop
        public bool HasLoopData { get; private set; }
        public uint LoopStart { get; private set; }
        public uint LoopEnd { get; private set; }
        public ushort Loop_r01 { get; private set; }
        public ushort Loop_r02 { get; private set; }

        public HcaMetadata(byte[] hcaBytes)
        {
            Channels = 0;
            SampleRate = 0;
            BlockCount = 0;
            LoopStart = 0;
            LoopEnd = 0;
            Loop_r01 = 0;
            Loop_r02 = 0;
            HasLoopData = false;

            if (hcaBytes != null)
            {
                if (hcaBytes.Length > 24)
                {
                    //Load fmt
                    Channels = hcaBytes[8 + 4];
                    SampleRate = BigEndianConverter.ReadUInt16(hcaBytes, 8 + 6);
                    BlockCount = BigEndianConverter.ReadInt32(hcaBytes, 8 + 8);
                }

                //Load loop
                if(hcaBytes.Length > 456)
                {
                    int offset = 40;
                    const int loopSignature = 1819242352;

                    for (int i = 0; i < 100; i++)
                    {
                        if (BigEndianConverter.ReadUInt32(hcaBytes, offset) == loopSignature)
                        {
                            HasLoopData = true;
                            LoopStart = BigEndianConverter.ReadUInt32(hcaBytes, offset + 4);
                            LoopEnd = BigEndianConverter.ReadUInt32(hcaBytes, offset + 8);
                            Loop_r01 = BigEndianConverter.ReadUInt16(hcaBytes, offset + 12);
                            Loop_r02 = BigEndianConverter.ReadUInt16(hcaBytes, offset + 14);
                            break;
                        }
                        else
                        {
                            offset += 4;
                        }
                    }

                }

            }
            
        }
    }

}
