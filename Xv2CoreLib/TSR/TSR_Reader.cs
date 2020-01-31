using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Xv2CoreLib.TSR
{
    public class TSR_Reader
    {
        public byte[] rawBytes { get; private set; }
        public int Position { get; set; } = 0;

        public TSR_Reader(string path)
        {
            rawBytes = File.ReadAllBytes(path);
        }

        public TSR_Reader(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
        }

        public int ReadInt32()
        {
            int ret = BitConverter.ToInt32(rawBytes, Position);
            Position += 4;
            return ret;
        }

        public uint ReadUInt32()
        {
            uint ret = BitConverter.ToUInt32(rawBytes, Position);
            Position += 4;
            return ret;
        }

        public ushort ReadUInt16()
        {
            ushort ret = BitConverter.ToUInt16(rawBytes, Position);
            Position += 2;
            return ret;
        }

        public short ReadInt16()
        {
            short ret = BitConverter.ToInt16(rawBytes, Position);
            Position += 2;
            return ret;
        }

        public float ReadFloat()
        {
            float ret = BitConverter.ToSingle(rawBytes, Position);
            Position += 4;
            return ret;
        }

        public string DecodeString(ref int enc_pos)
        {
            int size = BitConverter.ToInt32(rawBytes, Position);
            List<byte> encStringBytes = Utils.GetRangeFromByteArray(rawBytes, Position + 4, size).ToList(); //bytes.GetRange(Position + 4, size);

            if (enc_pos >= TSR_File.XOR_TABLE.Count())
                enc_pos = 0;

            for (int i = 0; i < size; i++)
            {
                byte b = (byte)(encStringBytes[i] ^ TSR_File.XOR_TABLE[enc_pos]);
                encStringBytes[i] = b;

                enc_pos++;
                if (enc_pos == TSR_File.XOR_TABLE.Count())
                    enc_pos = 0;

                if (b == 0 && i == (size - 1))
                    break;
            }

            Position += 4 + size;

            return Utils.GetString(encStringBytes, 0, size);
        }
    }
}
