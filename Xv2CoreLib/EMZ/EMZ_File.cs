using System;
using System.IO.Compression;
using System.IO;
using System.Collections.Generic;

namespace Xv2CoreLib.EMZ
{
    public class EMZ_File
    {
        internal const int SIGNATURE = 1515013411;

        public byte[] Data { get; set; }

        public EMZ_File() { }

        public EMZ_File(byte[] data)
        {
            Data = data;
        }

        public static EMZ_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static EMZ_File Load(byte[] bytes)
        {
            if (BitConverter.ToInt32(bytes, 0) != SIGNATURE)
                throw new InvalidDataException("#EMZ signature not found.");

            EMZ_File emzFile = new EMZ_File();

            int uncompressedSize = BitConverter.ToInt32(bytes, 8);
            int dataOffset = BitConverter.ToInt32(bytes, 12);
            byte[] compressedData = bytes.GetRange(dataOffset, bytes.Length - dataOffset);

            using(MemoryStream decompressStream = new MemoryStream(uncompressedSize))
            {
                using (MemoryStream compressedStream = new MemoryStream(compressedData))
                {
                    using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(decompressStream);
                    }
                }

                emzFile.Data = decompressStream.ToArray();
            }

            return emzFile;
        }

        /// <summary>
        /// Load the EMZ and automatically parse it as its contained type (SDS or EMB)
        /// </summary>
        /// <returns>Either an <see cref="SDS.SDS_File"/> or <see cref="EMB_CLASS.EMB_File"/>, depending on the contained type within the EMZ.</returns>
        public static object LoadData(byte[] bytes)
        {
            EMZ_File emz = Load(bytes);

            switch(BitConverter.ToInt32(emz.Data, 0))
            {
                case SDS.SDS_File.SIGNATURE:
                    return SDS.SDS_File.Load(emz.Data);
                case EMB_CLASS.EMB_File.SIGNATURE:
                    return EMB_CLASS.EMB_File.LoadEmb(emz.Data);
            }

            return null;
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(0)); //Unknown value
            bytes.AddRange(BitConverter.GetBytes(Data.Length));
            bytes.AddRange(BitConverter.GetBytes(16));

            using(MemoryStream compressedStream = new MemoryStream(Data.Length))
            {
                using(MemoryStream dataStream = new MemoryStream(Data))
                {
                    using(DeflateStream inflateStream = new DeflateStream(compressedStream, CompressionMode.Compress))
                    {
                        dataStream.CopyTo(inflateStream);
                    }
                }

                bytes.AddRange(compressedStream.ToArray());
            }

            return bytes.ToArray();
        }
    }
}
