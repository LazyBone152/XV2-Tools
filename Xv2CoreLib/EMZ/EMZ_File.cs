using System;
using System.IO.Compression;
using System.IO;
using System.Collections.Generic;
using Xv2CoreLib.ValuesDictionary;
using Xv2CoreLib.SDS;
using Xv2CoreLib.EMB_CLASS;
using YAXLib;
using System.Xml.Linq;

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

        /// <summary>
        /// Loads the contained data in the XML. This is NOT a <see cref="EMZ_File"/> object, as that is never serialized to XML, only the contained data is.
        /// </summary>
        /// <returns>Either an <see cref="SDS_File"/> or <see cref="EMB_File"/>, depending on the content in the xml.</returns>
        /// <returns></returns>
        public static object LoadFromXml(string path)
        {
            XDocument doc = XDocument.Load(path);

            if(doc.Root.Name == "SDS")
            {
                YAXSerializer serializer = new YAXSerializer(typeof(SDS_File), YAXSerializationOptions.DontSerializeNullObjects);
                return (SDS_File)serializer.Deserialize(doc.Root);
            }
            else if(doc.Root.Name == "EMB_File")
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EMB_File), YAXSerializationOptions.DontSerializeNullObjects);
                return (EMB_File)serializer.Deserialize(doc.Root);
            }
            else
            {
                throw new InvalidDataException("EMZ_File.LoadFromXml: Unknown data contained in XML!");
            }
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

            return emz.LoadData();
        }

        /// <summary>
        /// Automatically parse and return the contained data as the correct type (EMB or SDS).
        /// </summary>
        /// <returns>Either an <see cref="SDS.SDS_File"/> or <see cref="EMB_CLASS.EMB_File"/>, depending on the contained type within the EMZ.</returns>
        public object LoadData()
        {
            switch (BitConverter.ToInt32(Data, 0))
            {
                case SDS_File.SIGNATURE:
                    SDS_File sds = SDS_File.Load(Data);
                    sds.IsEMZ = true;
                    return sds;
                case EMB_File.SIGNATURE:
                    EMB_File emb =  EMB_File.LoadEmb(Data);
                    emb.IsEMZ = true;
                    return emb;
                default:
                    return null;
            }
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
    
        public bool IsDataEmb()
        {
            if(Data == null) return false;
            return BitConverter.ToInt32(Data, 0) == EMB_File.SIGNATURE;
        }

        public bool IsDataSds()
        {
            if (Data == null) return false;
            return BitConverter.ToInt32(Data, 0) == SDS_File.SIGNATURE;
        }
    }
}
