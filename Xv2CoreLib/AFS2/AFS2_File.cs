using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.HCA;
using Xv2CoreLib.CPK;
using YAXLib;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.ACB_NEW;

namespace Xv2CoreLib.AFS2
{
    public enum PointerSize
    {
        UInt16 = 2,
        UInt32 = 4
    }

    [Serializable]
    public class AFS2_File
    {
        public const int AFS2_SIGNATURE = 844318273;

        [YAXAttributeForClass]
        public byte I_04 { get; set; } = 1;
        [YAXAttributeForClass]
        public PointerSize Pointer_Size { get; set; } = PointerSize.UInt32;
        [YAXAttributeForClass]
        public byte I_06 { get; set; } = 2;
        [YAXAttributeForClass]
        public byte I_07 { get; set; }

        [YAXAttributeForClass]
        public int ByteAlignment { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AudioFile")]
        public List<AFS2_Entry> Entries { get; set; }


        public AFS2_File() { }

        /// <summary>
        /// Initialize from an old style CPK AWB.
        /// </summary>
        public AFS2_File(AWB_CPK cpk)
        {
            Entries = new List<AFS2_Entry>();
            
            foreach(var entry in cpk.Entries)
            {
                Entries.Add(new AFS2_Entry(entry));
            }
        }

        #region LoadSave
        public void SortEntries()
        {
            if (Entries != null)
                Entries.Sort((x, y) => x.AwbId - y.AwbId);
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
            if (size == -1)
            {
                size = rawBytes.Length;
            }

            AFS2_File afs2File = new AFS2_File() { Entries = new List<AFS2_Entry>() };

            if (BitConverter.ToInt32(rawBytes, offset + 0) != AFS2_SIGNATURE)
            {
                throw new InvalidDataException(String.Format("AFS2 Signature not found at offset 0x{0}.\nParse failed.", offset.ToString("x")));
            }

            int count = BitConverter.ToInt32(rawBytes, offset + 8);
            afs2File.ByteAlignment = BitConverter.ToInt32(rawBytes, offset + 12);
            afs2File.I_04 = rawBytes[offset + 4];
            afs2File.Pointer_Size = (PointerSize)rawBytes[offset + 5];
            afs2File.I_06 = rawBytes[offset + 6];
            afs2File.I_07 = rawBytes[offset + 7];

            //Init
            int cueIdOffset = 16 + offset;
            int pointerListOffset = ((2 * count) + 16) + offset;
            List<int> Pointers = new List<int>();
            List<int> PointersFiltered = new List<int>();

            //Cue IDs
            for (int i = 0; i < count; i++)
            {
                //IDs
                afs2File.Entries.Add(new AFS2_Entry()
                {
                    AwbId = BitConverter.ToUInt16(rawBytes, cueIdOffset)
                });
                cueIdOffset += 2;

                //Pointers
                switch (afs2File.Pointer_Size)
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
            if (afs2File.Pointer_Size == PointerSize.UInt16)
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

                afs2File.Entries[i].bytes = rawBytes.GetRange(PointersFiltered[i], length);
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
            int count = (afs2File.Entries != null) ? afs2File.Entries.Count : 0;
            int finalPointer = 0;
            List<int> Pointers = new List<int>();
            afs2File.SortEntries();

            //Header
            bytes.AddRange(BitConverter.GetBytes((uint)AFS2_SIGNATURE));
            bytes.Add(afs2File.I_04);
            bytes.Add((byte)afs2File.Pointer_Size);
            bytes.Add(afs2File.I_06);
            bytes.Add(afs2File.I_07);
            bytes.AddRange(BitConverter.GetBytes((uint)count));
            bytes.AddRange(BitConverter.GetBytes((uint)afs2File.ByteAlignment));

            //Cue ID list
            for (int i = 0; i < count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(afs2File.Entries[i].AwbId));
            }

            //Pointer List
            for (int i = 0; i < count + 1; i++)
            {
                switch (afs2File.Pointer_Size)
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
                        throw new Exception(String.Format("Undefined PointerSize encountered: {0}\nSave failed.", afs2File.Pointer_Size));
                }
            }

            int headerEnd = bytes.Count;

            for (int i = 0; i < count; i++)
            {
                //Fill pointer
                switch (afs2File.Pointer_Size)
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
                bytes.AddRange(afs2File.Entries[i].bytes);
            }

            //Fill in final pointer
            switch (afs2File.Pointer_Size)
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

        #endregion

        #region Helper
        public static int GetActualOffset(int offset, int byteAlignment)
        {
            double f_offset = offset;
            while (f_offset / byteAlignment != Math.Floor(f_offset / byteAlignment))
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

            foreach (var file in Entries)
            {
                if (id <= file.AwbId)
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
            foreach (var awbTrack in Entries)
            {
                if (id == awbTrack.AwbId) return true;
            }
            return false;
        }

        public void PadWithNullEntries()
        {
            //Keeps all IDs consecutive by adding null entries (need for compatibility with Eternity Audio Tool)

            int maxId = Entries.Max(x => x.AwbId);

            for (int i = 0; i < maxId; i++)
            {
                if (!Entries.Any(x => x.AwbId == i))
                    Entries.Add(new AFS2_Entry() { AwbId = (ushort)i, bytes = new byte[0] });
            }

            SortEntries();
        }

        #endregion

        #region GetAdd
        //Add
        public ushort AddEntry(AFS2_Entry entry, bool reuseExistingEntries)
        {
            ushort existingEntry = GetExistingEntry(entry);

            if (existingEntry != ushort.MaxValue)
            {
                return existingEntry;
            }
            else
            {
                ushort newId = (ushort)NextID();
                Entries.Add(new AFS2_Entry()
                {
                    AwbId = newId,
                    bytes = entry.bytes
                });

                return newId;
            }
        }
        
        //Get
        public AFS2_Entry GetEntry(ushort id)
        {
            foreach (var file in Entries)
            {
                if (file.AwbId == id) return file;
            }
            return null;
        }

        public ushort GetExistingEntry(AFS2_Entry entry)
        {
            foreach (var _entry in Entries)
            {
                if (Utils.CompareArray(_entry.bytes, entry.bytes)) return _entry.AwbId;
            }

            return ushort.MaxValue;
        }

        public ushort GetExistingEntry(byte[] bytes)
        {
            foreach (var _entry in Entries)
            {
                if (Utils.CompareArray(bytes, _entry.bytes)) return _entry.AwbId;
            }

            return ushort.MaxValue;
        }

        //Undoable
        public List<IUndoRedo> AddEntry(byte[] bytes, bool reuseExistingEntries, out ushort newAwbId)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            ushort existingEntry = GetExistingEntry(bytes);

            if (existingEntry != ushort.MaxValue)
            {
                newAwbId = existingEntry;
            }
            else
            {
                ushort newId = (ushort)NextID();
                var newEntry = new AFS2_Entry()
                {
                    AwbId = newId,
                    bytes = bytes
                };
                Entries.Add(newEntry);
                undos.Add(new UndoableListAdd<AFS2_Entry>(Entries, newEntry));

                newAwbId = newId;
            }

            return undos;
        }

        #endregion

        public static AFS2_File CreateNewAwbFile()
        {
            return new AFS2_File()
            {
                ByteAlignment = 32,
                Entries = new List<AFS2_Entry>(),
                Pointer_Size = PointerSize.UInt32
            };
        }
        
        
        
    }

    [YAXSerializeAs("AudioFile")]
    [Serializable]
    public class AFS2_Entry
    {
        [YAXAttributeForClass]
        public ushort AwbId { get; set; }
        [YAXAttributeFor("Data")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] bytes { get; set; }

        [YAXDontSerialize]
        public TrackMetadata HcaInfo { get { return new TrackMetadata(bytes); } }

        public AFS2_Entry() { }

        public AFS2_Entry(CPK_Entry cpkEntry)
        {
            AwbId = (ushort)cpkEntry.ID;
            bytes = cpkEntry.Data;
        }

        public bool BytesIsNullOrEmpty()
        {
            if (bytes == null) return true;
            return bytes.Length == 0;
        }
    }
}
