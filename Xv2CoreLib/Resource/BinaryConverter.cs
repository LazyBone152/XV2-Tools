using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib.Resource
{
    /// <summary>
    /// Provides static methods for dealing with Big Endian data types.
    /// </summary>
    public static class BigEndianConverter
    {
        public static Version UIntToVersion(uint value, bool hex)
        {
            var values = BitConverter.GetBytes(value);

            if (hex)
            {
                return new Version(Convert.ToByte(values[3].ToString("x")), Convert.ToByte(values[2].ToString("x")), Convert.ToByte(values[1].ToString("x")), Convert.ToByte(values[0].ToString("x")));
            }
            else
            {
                return new Version(values[3], values[2], values[1], values[0]);
            }
        }

        public static uint VersionToUInt(Version version, bool hex)
        {
            if (hex)
            {
                byte[] bytes = new byte[4] { byte.Parse(version.Revision.ToString(), System.Globalization.NumberStyles.HexNumber), byte.Parse(version.Build.ToString(), System.Globalization.NumberStyles.HexNumber), byte.Parse(version.Minor.ToString(), System.Globalization.NumberStyles.HexNumber), byte.Parse(version.Major.ToString(), System.Globalization.NumberStyles.HexNumber) };
                return BitConverter.ToUInt32(bytes, 0);
            }
            else
            {
                byte[] bytes = new byte[4] { (byte)version.Revision, (byte)version.Build, (byte)version.Minor, (byte)version.Major };
                return BitConverter.ToUInt32(bytes, 0);
            }
        }

        public static ushort[] ToUInt16Array(byte[] bytes, int index = 0, int count = -1)
        {
            if (bytes == null) return new ushort[0];
            if (count == -1)
                count = bytes.Length / 2;

            ushort[] ints = new ushort[count];

            for (int i = 0; i < count * 2; i += 2)
            {
                ints[i / 2] = BigEndianConverter.ReadUInt16(bytes, index + i);
            }

            return ints;
        }

        public static short ReadInt16(byte[] bytes, int offset)
        {
            var _bytes = CopyBytes(bytes, offset, 2);
            return BitConverter.ToInt16(_bytes.Reverse().ToArray(), 0);
        }

        public static ushort ReadUInt16(List<byte> bytes, int offset)
        {
            var _bytes = bytes.GetRange(offset, 2);
            _bytes.Reverse();
            return BitConverter.ToUInt16(_bytes.ToArray(), 0);
        }

        public static ushort ReadUInt16(byte[] bytes, int offset)
        {
            var _bytes = CopyBytes(bytes, offset, 2);
            return BitConverter.ToUInt16(_bytes.Reverse().ToArray(), 0);
        }
        
        public static int ReadInt32(byte[] bytes, int offset)
        {
            var _bytes = CopyBytes(bytes, offset, 4);
            return BitConverter.ToInt32(_bytes.Reverse().ToArray(), 0);
        }

        public static uint ReadUInt32(byte[] bytes, int offset)
        {
            var _bytes = CopyBytes(bytes, offset, 4);
            return BitConverter.ToUInt32(_bytes.Reverse().ToArray(), 0);
        }

        public static Int64 ReadInt64(byte[] bytes, int offset)
        {
            var _bytes = CopyBytes(bytes, offset, 8);
            return BitConverter.ToInt64(_bytes.Reverse().ToArray(), 0);
        }

        public static UInt64 ReadUInt64(byte[] bytes, int offset)
        {
            var _bytes = CopyBytes(bytes, offset, 8);
            return BitConverter.ToUInt64(_bytes.Reverse().ToArray(), 0);
        }

        public static Single ReadSingle(byte[] bytes, int offset)
        {
            var _bytes = CopyBytes(bytes, offset, 4);
            return BitConverter.ToSingle(_bytes.Reverse().ToArray(), 0);
        }

        public static Double ReadDouble(byte[] bytes, int offset)
        {
            var _bytes = CopyBytes(bytes, offset, 8);
            return BitConverter.ToDouble(_bytes.Reverse().ToArray(), 0);
        }

        public static byte[] GetBytes(UInt16 value)
        {
            return (byte[])BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(Int16 value)
        {
            return (byte[])BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(UInt32 value)
        {
            return (byte[])BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(Int32 value)
        {
            return (byte[])BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(UInt64 value)
        {
            return (byte[])BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(Int64 value)
        {
            return (byte[])BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(Single value)
        {
            return (byte[])BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(Double value)
        {
            return (byte[])BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(ushort[] intArray)
        {
            List<byte> bytes = new List<byte>();

            foreach (ushort i in intArray)
            {
                var valueBytes = BitConverter.GetBytes(i);

                bytes.AddRange(new byte[2] { valueBytes[1], valueBytes[0] });
            }

            return bytes.ToArray();
        }

        private static byte[] CopyBytes(byte[] bytes, int offset, int count)
        {
            byte[] newBytes = new byte[count];
            for(int i = 0; i < count; i++)
            {
                newBytes[i] = bytes[offset + i];
            }

            return newBytes;
        }
    }
}
