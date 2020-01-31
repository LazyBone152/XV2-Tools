using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.HCA;
using YAXLib;

namespace Xv2CoreLib.AFS2
{
    public enum PointerSize
    {
        UInt16 = 2,
        UInt32 = 4
    }

    public class AFS2_File
    {
        public const int AFS2_SIGNATURE = 844318273;

        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] Version { get; set; }
        [YAXAttributeForClass]
        public int ByteAlignment { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AudioFile")]
        public List<AFS2_AudioFile> EmbeddedFiles { get; set; }

        public void SortEntries()
        {
            if (EmbeddedFiles != null)
                EmbeddedFiles.Sort((x, y) => x.AwbId - y.AwbId);
        }

        public void Save(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            byte[] header;
            File.WriteAllBytes(path, WriteAfs2File(this, out header));
        }

        public static void ParseAsf2File(string filePath)
        {
            byte[] rawBytes = File.ReadAllBytes(filePath);
            var asf2File = LoadAfs2File(rawBytes, 0, rawBytes.Count());
            YAXSerializer serializer = new YAXSerializer(typeof(AFS2_File));
            serializer.SerializeToFile(asf2File, filePath + ".xml");
        }

        public static AFS2_File LoadAfs2File(string filePath)
        {
            byte[] rawBytes = File.ReadAllBytes(filePath);
            var asf2File = LoadAfs2File(rawBytes, 0, rawBytes.Count());
            return asf2File;
        }

        public static AFS2_File LoadAfs2File(byte[] rawBytes, int offset = 0, int size = -1)
        {
            if(size == -1)
            {
                size = rawBytes.Length;
            }

            AFS2_File afs2File = new AFS2_File() { EmbeddedFiles = new List<AFS2_AudioFile>() };

            if(BitConverter.ToInt32(rawBytes, offset + 0) != AFS2_SIGNATURE)
            {
                throw new InvalidDataException(String.Format("AFS2 Signature not found at offset 0x{0}.\nParse failed.", offset.ToString("x")));
            }

            int count = BitConverter.ToInt32(rawBytes, offset + 8);
            afs2File.ByteAlignment = BitConverter.ToInt32(rawBytes, offset + 12);
            afs2File.Version = rawBytes.GetRange(offset + 4, 4);

            //Version validation
            PointerSize pointerSize = (PointerSize)rawBytes[offset + 5];

            //Init
            int cueIdOffset = 16 + offset;
            int pointerListOffset = ((2 * count) + 16) + offset;
            List<int> Pointers = new List<int>();
            List<int> PointersFiltered = new List<int>();

            //Cue IDs
            for (int i = 0; i < count; i++)
            {
                //IDs
                afs2File.EmbeddedFiles.Add(new AFS2_AudioFile()
                {
                    AwbId = BitConverter.ToUInt16(rawBytes, cueIdOffset)
                });
                cueIdOffset += 2;

                //Pointers
                switch (pointerSize)
                {
                    case PointerSize.UInt16:
                        Pointers.Add(BitConverter.ToUInt16(rawBytes, pointerListOffset));
                        PointersFiltered.Add(BitConverter.ToUInt16(rawBytes, pointerListOffset));
                        pointerListOffset += 2;
                        break;
                    case PointerSize.UInt32:
                        Pointers.Add(BitConverter.ToInt32(rawBytes, pointerListOffset));
                        PointersFiltered.Add(BitConverter.ToInt32(rawBytes, pointerListOffset));
                        pointerListOffset += 4;
                        break;
                }
            }
            if(pointerSize == PointerSize.UInt16)
            {
                Pointers.Add(BitConverter.ToUInt16(rawBytes, pointerListOffset));
                PointersFiltered.Add(BitConverter.ToUInt16(rawBytes, pointerListOffset));
            }
            else
            {
                Pointers.Add(BitConverter.ToInt32(rawBytes, pointerListOffset));
                PointersFiltered.Add(BitConverter.ToInt32(rawBytes, pointerListOffset));
            }

            //Filter offsets
            for (int i = 0; i < count + 1; i++)
            {
                PointersFiltered[i] = GetActualOffset(PointersFiltered[i], afs2File.ByteAlignment) + offset;
                Pointers[i] += offset;
            }

            //Data
            for (int i = 0; i < count; i++)
            {
                //Get length
                int length = 0;
                length = Pointers[i + 1] - PointersFiltered[i];

                afs2File.EmbeddedFiles[i].bytes = rawBytes.GetRange(PointersFiltered[i], length);
            }

            return afs2File;
        }

        public static void SaveAfs2File(string filePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath));
            YAXSerializer serializer = new YAXSerializer(typeof(AFS2_File), YAXSerializationOptions.DontSerializeNullObjects);
            AFS2_File utfFile = (AFS2_File)serializer.DeserializeFromFile(filePath);
            byte[] header;
            byte[] bytes = WriteAfs2File(utfFile, out header);
            File.WriteAllBytes(saveLocation, bytes);
        }

        public byte[] WriteAfs2File()
        {
            byte[] header;
            return WriteAfs2File(this, out header);
        }

        public byte[] WriteAfs2File(out byte[] header)
        {
            return WriteAfs2File(this, out header);
        }

        public static byte[] WriteAfs2File(AFS2_File afs2File, out byte[] header)
        {
            header = null;
            List<byte> bytes = new List<byte>();
            if (afs2File == null) return bytes.ToArray();

            //Init
            int count = (afs2File.EmbeddedFiles != null) ? afs2File.EmbeddedFiles.Count : 0;
            int finalPointer = 0;
            List<int> Pointers = new List<int>();
            afs2File.Version[1] = (byte)PointerSize.UInt32; //Hardcode a 32 bit pointer size.
            afs2File.SortEntries();

            //Header
            if (afs2File.Version.Length != 4) throw new InvalidDataException(String.Format("Invalid Afs2File Version size. Expected 4 bytes, found {0}.", afs2File.Version.Length));
            PointerSize pointerSize = (PointerSize)afs2File.Version[1];
            bytes.AddRange(BitConverter.GetBytes((uint)AFS2_SIGNATURE));
            bytes.AddRange(afs2File.Version);
            bytes.AddRange(BitConverter.GetBytes((uint)count));
            bytes.AddRange(BitConverter.GetBytes((uint)afs2File.ByteAlignment));

            //Cue ID list
            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(afs2File.EmbeddedFiles[i].AwbId));
            }

            //Pointer List
            for (int i = 0; i < count + 1; i++)
            {
                switch (pointerSize)
                {
                    case PointerSize.UInt16:
                        Pointers.Add(bytes.Count());
                        finalPointer = bytes.Count();
                        bytes.AddRange(new byte[2]);
                        break;
                    case PointerSize.UInt32:
                        Pointers.Add(bytes.Count());
                        finalPointer = bytes.Count();
                        bytes.AddRange(new byte[4]);
                        break;
                    default:
                        throw new Exception(String.Format("Undefined PointerSize encountered: {0}\nSave failed.", pointerSize));
                }
            }

            int headerEnd = bytes.Count;

            for(int i = 0; i < count; i++)
            {
                //Fill pointer
                switch (pointerSize)
                {
                    case PointerSize.UInt16:
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((UInt16)bytes.Count()), Pointers[i]);
                        break;
                    case PointerSize.UInt32:
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), Pointers[i]);
                        break;
                }

                //Add padding
                bytes.AddRange(new byte[CalculatePadding(bytes.Count, afs2File.ByteAlignment)]);

                //Add data
                bytes.AddRange(afs2File.EmbeddedFiles[i].bytes);
            }

            //Fill in final pointer
            switch (pointerSize)
            {
                case PointerSize.UInt16:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((UInt16)bytes.Count()), finalPointer);
                    break;
                case PointerSize.UInt32:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), finalPointer);
                    break;
            }

            header = bytes.GetRange(0, headerEnd).ToArray();

            return bytes.ToArray();
        }

        public static int GetActualOffset(int offset, int byteAlignment)
        {
            double f_offset = offset;
            while(f_offset / byteAlignment != Math.Floor(f_offset / byteAlignment))
            {
                f_offset += 1.0;
            }
            return (int)f_offset;
        }

        public static int CalculatePadding(int offset, int byteAlignment)
        {
            double f_offset = offset;
            double padding = 0f;
            while (f_offset / byteAlignment != Math.Floor(f_offset / byteAlignment))
            {
                f_offset += 1.0;
                padding += 1.0;
            }
            return (int)padding;
        }

        public int NextID()
        {
            int id = 0;

            foreach(var file in EmbeddedFiles)
            {
                if(id <= file.AwbId)
                {
                    id++;
                }
            }

            while (IsIdUsed(id))
            {
                id++;
            }
            return id;
        }

        private bool IsIdUsed(int id)
        {
            foreach(var awbTrack in EmbeddedFiles)
            {
                if (id == awbTrack.AwbId) return true;
            }
            return false;
        }

        public ushort AddEntry(AFS2_AudioFile entry, bool reuseExistingEntries)
        {
            ushort existingEntry = GetExistingEntry(entry);

            if(existingEntry != ushort.MaxValue)
            {
                return existingEntry;
            }
            else
            {
                ushort newId = (ushort)NextID();
                EmbeddedFiles.Add(new AFS2_AudioFile()
                {
                    AwbId = newId,
                    bytes = entry.bytes
                });

                return newId;
            }
        }

        public AFS2_AudioFile GetEntry(ushort id)
        {
            foreach(var file in EmbeddedFiles)
            {
                if (file.AwbId == id) return file;
            }
            return null;
        }

        public ushort GetExistingEntry(AFS2_AudioFile entry)
        {
            foreach(var _entry in EmbeddedFiles)
            {
                if (Utils.CompareArray(_entry.bytes, entry.bytes)) return _entry.AwbId;
            }

            return ushort.MaxValue;
        }

        public static AFS2_File CreateNewAwbFile()
        {
            return new AFS2_File()
            {
                ByteAlignment = 32,
                EmbeddedFiles = new List<AFS2_AudioFile>(),
                Version = new byte[] { 1, 4, 2, 0 }
            };
        }
    }

    [YAXSerializeAs("AudioFile")]
    public class AFS2_AudioFile
    {
        [YAXAttributeForClass]
        public ushort AwbId { get; set; }
        [YAXAttributeFor("Data")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] bytes { get; set; }

        [YAXDontSerialize]
        public HcaMetadata HcaInfo { get { return new HcaMetadata(bytes); } }
    }
}
