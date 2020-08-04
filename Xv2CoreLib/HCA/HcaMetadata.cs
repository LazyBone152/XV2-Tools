using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.HCA
{
    //For reading and modifying the metadata from HCA files, not an actual parser
    //Big endian

    [Serializable]
    public struct HcaMetadata
    {
        private const int LOOP_SIGNATURE = 0x706F6F6C;
        private const int ATH_SIGNATURE = 0x00687461;
        private const int VBR_SIGNATURE = 0x00726276;
        private const int DEC_SIGNATURE = 0x00636564;
        private const int COMP_SIGNATURE = 0x706D6F63;
        private const int FMT_SIGNATURE = 0x00746D66;
        private const int HCA_SIGNATURE = 0x00414348;

        public bool ValidHcaFile { get; private set; }

        //Fmt
        public byte Channels { get; private set; }
        public ushort SampleRate { get; private set; }
        public int BlockCount { get; private set; }
        public int NumSamples { get { return BlockCount * 1024; } }

        public uint Milliseconds { get { return (uint)(DurationSeconds * 1000.0); } }
        public double DurationSeconds { get { return BlocksToSeconds((uint)BlockCount, SampleRate); } } //Seconds
        public TimeSpan Duration { get { return new TimeSpan(0, 0, 0, (int)DurationSeconds); } }

        //Loop
        public bool HasLoopData { get; private set; }
        public uint LoopStart { get; private set; }
        public uint LoopEnd { get; private set; }
        public ushort Loop_r01 { get; private set; }
        public ushort Loop_r02 { get; private set; }

        public double LoopStartSeconds { get { return BlocksToSeconds(LoopStart, SampleRate); } }
        public double LoopEndSeconds { get { return BlocksToSeconds(LoopEnd, SampleRate); } }

        public uint LoopStartMs { get { return (uint)(LoopStartSeconds * 1000.0); } }
        public uint LoopEndMs { get { return (uint)(LoopEndSeconds * 1000.0); } }

        public uint LoopStartSamples { get { return BlocksToSamples(LoopStart); } }
        public uint LoopEndSamples { get { return BlocksToSamples(LoopEnd); } }

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
            ValidHcaFile = false;

            if (hcaBytes != null)
            {
                if (BitConverter.ToInt32(hcaBytes, 0) != HCA_SIGNATURE)
                {
                    return;
                }

                ValidHcaFile = true;
                int dataOffset = BigEndianConverter.ReadUInt16(hcaBytes, 6);
                int fmtOffset = hcaBytes.IndexOfValue(FMT_SIGNATURE, 0, dataOffset, false);
                int loopOffset = hcaBytes.IndexOfValue(LOOP_SIGNATURE, 0, dataOffset, false);

                //Load fmt
                if (hcaBytes.Length > fmtOffset + 16 && fmtOffset != -1)
                {
                    Channels = hcaBytes[8 + 4];
                    SampleRate = BigEndianConverter.ReadUInt16(hcaBytes, fmtOffset + 6);
                    BlockCount = BigEndianConverter.ReadInt32(hcaBytes, fmtOffset + 8);
                }

                //Load loop
                if(hcaBytes.Length > loopOffset + 16 && loopOffset != -1)
                {
                    HasLoopData = true;
                    LoopStart = BigEndianConverter.ReadUInt32(hcaBytes, loopOffset + 4);
                    LoopEnd = BigEndianConverter.ReadUInt32(hcaBytes, loopOffset + 8);
                    Loop_r01 = BigEndianConverter.ReadUInt16(hcaBytes, loopOffset + 12);
                    Loop_r02 = BigEndianConverter.ReadUInt16(hcaBytes, loopOffset + 14);
                    
                }

            }
            
        }

        /// <summary>
        /// Set or remove a HCA loop.
        /// </summary>
        /// <param name="hcaBytes">The hca file.</param>
        /// <param name="loop">If false, any loop data in the hca file will be removed. If none exists then nothing will happen.</param>
        /// <param name="loopStart">The start of the desired loop, in seconds.</param>
        /// <param name="loopEnd">The end of the loop, in seconds.</param>
        /// <param name="numLoops">The number of loops. (The game seems to ignore this... so just use 0)</param>
        /// <returns></returns>
        public static byte[] SetLoop(byte[] hcaBytes, bool loop, double loopStart, double loopEnd, int numLoops = 0)
        {
            if (loopStart > loopEnd || numLoops < 0) return hcaBytes;
            if (loopStart < 0.0) loopStart = 0.0;

            HcaMetadata metadata = new HcaMetadata(hcaBytes);
            if (!metadata.ValidHcaFile) throw new InvalidOperationException("SetLoop: the provided file is not a valid HCA file.");

            ushort dataOffset = BigEndianConverter.ReadUInt16(hcaBytes, 6);
            int loopIndex = hcaBytes.IndexOfValue(LOOP_SIGNATURE, 0, dataOffset, false);

            if (!loop)
            {
                if (loopIndex == -1) return hcaBytes;

                //Remove loop and return
                List<byte> bytes = hcaBytes.ToList();
                bytes.RemoveRange(loopIndex, 16);

                //Adjust dataOffset
                dataOffset -= 16;

                hcaBytes = bytes.ToArray();
            }
            else
            {
                uint loopStartBlocks = SecondsToBlocks(loopStart, metadata.SampleRate);
                uint loopEndBlocks = SecondsToBlocks(loopEnd, metadata.SampleRate);

                if (loopEndBlocks >= metadata.BlockCount || loopEnd < 0) loopEndBlocks = (uint)(metadata.BlockCount - 1);
                if (loopStartBlocks < 0 || loopStartBlocks > metadata.BlockCount) loopStartBlocks = 0;

                int loopCount = (numLoops == 0 && numLoops < 0x80) ? 0x80 : numLoops;

                List<byte> newLoop = new List<byte>();
                newLoop.AddRange(BitConverter.GetBytes(LOOP_SIGNATURE));
                newLoop.AddRange(BigEndianConverter.GetBytes(loopStartBlocks));
                newLoop.AddRange(BigEndianConverter.GetBytes(loopEndBlocks));
                newLoop.AddRange(BigEndianConverter.GetBytes((ushort)loopCount));
                newLoop.AddRange(BigEndianConverter.GetBytes((ushort)0x400));
                

                if (loopIndex != -1)
                {
                    hcaBytes = Utils.ReplaceRange(hcaBytes, newLoop.ToArray(), loopIndex);
                }
                else
                {
                    List<byte> bytes = hcaBytes.ToList();
                    bytes.InsertRange(IndexOfLoopBlock(hcaBytes, dataOffset), newLoop);
                    hcaBytes = bytes.ToArray();

                    //Adjust dataOffset
                    dataOffset += 16;
                }
            }

            //Update header size
            hcaBytes = Utils.ReplaceRange(hcaBytes, BigEndianConverter.GetBytes((ushort)(dataOffset)), 6);

            //Set checksum
            hcaBytes = SetHeaderChecksum(hcaBytes);

            return hcaBytes;
        }

        /// <summary>
        /// Look for the index where to insert the loop block. 
        /// </summary>
        /// <returns></returns>
        private static int IndexOfLoopBlock(byte[] bytes, int range)
        {
            int fmtOffset = bytes.IndexOfValue(FMT_SIGNATURE, 8, range, false);
            int comp = bytes.IndexOfValue(COMP_SIGNATURE, 8, range, false);
            int dec = bytes.IndexOfValue(DEC_SIGNATURE, 8, range, false);
            int vbr = bytes.IndexOfValue(VBR_SIGNATURE, 8, range, false);
            int ath = bytes.IndexOfValue(ATH_SIGNATURE, 8, range, false);

            if (ath != -1) return ath + 6;
            if (vbr != -1) return vbr + 8;
            if (dec != -1) return dec + 12;
            if (comp != -1) return comp + 16;
            if (fmtOffset != -1) return fmtOffset + 16;

            return 8;
        }

        public static byte[] SetHeaderChecksum(byte[] bytes)
        {
            ushort dataOffset = BigEndianConverter.ReadUInt16(bytes, 6);
            ushort checksum = CheckSum(bytes, dataOffset - 2, 0);
            return Utils.ReplaceRange(bytes, BigEndianConverter.GetBytes((ushort)(checksum)), dataOffset - 2);
        }

        public static ushort CheckSum(byte[] data, int size, ushort sum)
        {
	        ushort[] v =
            {
                0x0000,0x8005,0x800F,0x000A,0x801B,0x001E,0x0014,0x8011,0x8033,0x0036,0x003C,0x8039,0x0028,0x802D,0x8027,0x0022,
                0x8063,0x0066,0x006C,0x8069,0x0078,0x807D,0x8077,0x0072,0x0050,0x8055,0x805F,0x005A,0x804B,0x004E,0x0044,0x8041,
                0x80C3,0x00C6,0x00CC,0x80C9,0x00D8,0x80DD,0x80D7,0x00D2,0x00F0,0x80F5,0x80FF,0x00FA,0x80EB,0x00EE,0x00E4,0x80E1,
                0x00A0,0x80A5,0x80AF,0x00AA,0x80BB,0x00BE,0x00B4,0x80B1,0x8093,0x0096,0x009C,0x8099,0x0088,0x808D,0x8087,0x0082,
                0x8183,0x0186,0x018C,0x8189,0x0198,0x819D,0x8197,0x0192,0x01B0,0x81B5,0x81BF,0x01BA,0x81AB,0x01AE,0x01A4,0x81A1,
                0x01E0,0x81E5,0x81EF,0x01EA,0x81FB,0x01FE,0x01F4,0x81F1,0x81D3,0x01D6,0x01DC,0x81D9,0x01C8,0x81CD,0x81C7,0x01C2,
                0x0140,0x8145,0x814F,0x014A,0x815B,0x015E,0x0154,0x8151,0x8173,0x0176,0x017C,0x8179,0x0168,0x816D,0x8167,0x0162,
                0x8123,0x0126,0x012C,0x8129,0x0138,0x813D,0x8137,0x0132,0x0110,0x8115,0x811F,0x011A,0x810B,0x010E,0x0104,0x8101,
                0x8303,0x0306,0x030C,0x8309,0x0318,0x831D,0x8317,0x0312,0x0330,0x8335,0x833F,0x033A,0x832B,0x032E,0x0324,0x8321,
                0x0360,0x8365,0x836F,0x036A,0x837B,0x037E,0x0374,0x8371,0x8353,0x0356,0x035C,0x8359,0x0348,0x834D,0x8347,0x0342,
                0x03C0,0x83C5,0x83CF,0x03CA,0x83DB,0x03DE,0x03D4,0x83D1,0x83F3,0x03F6,0x03FC,0x83F9,0x03E8,0x83ED,0x83E7,0x03E2,
                0x83A3,0x03A6,0x03AC,0x83A9,0x03B8,0x83BD,0x83B7,0x03B2,0x0390,0x8395,0x839F,0x039A,0x838B,0x038E,0x0384,0x8381,
                0x0280,0x8285,0x828F,0x028A,0x829B,0x029E,0x0294,0x8291,0x82B3,0x02B6,0x02BC,0x82B9,0x02A8,0x82AD,0x82A7,0x02A2,
                0x82E3,0x02E6,0x02EC,0x82E9,0x02F8,0x82FD,0x82F7,0x02F2,0x02D0,0x82D5,0x82DF,0x02DA,0x82CB,0x02CE,0x02C4,0x82C1,
                0x8243,0x0246,0x024C,0x8249,0x0258,0x825D,0x8257,0x0252,0x0270,0x8275,0x827F,0x027A,0x826B,0x026E,0x0264,0x8261,
                0x0220,0x8225,0x822F,0x022A,0x823B,0x023E,0x0234,0x8231,0x8213,0x0216,0x021C,0x8219,0x0208,0x820D,0x8207,0x0202,
            };

            for (int i = 0; i < size; i++)
                sum = (ushort)((sum << 8) ^ v[(sum >> 8) ^ data[i]]);
	
	        return sum;
        }

        //Helpers
        private static double BlocksToSeconds(uint blocks, ushort sampleRate)
        {
            return (blocks * 1024.0) / sampleRate;
        }

        private static uint SecondsToBlocks(double seconds, ushort sampleRate)
        {
            return (uint)Math.Round(((seconds * sampleRate) / 1024.0));
            //return (uint)((seconds * sampleRate) / 1024.0);
        }

        private static uint BlocksToSamples(uint blocks)
        {
            return (uint)(blocks * 1024.0);
        }
        

    }

}
