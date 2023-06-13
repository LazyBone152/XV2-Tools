using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.CPK;
using YAXLib;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.ACB;
using System.Security.Cryptography;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.AFS2
{
    public enum PointerSize
    {
        UInt16 = 2,
        UInt32 = 4,
        UInt64 = 8
    }

    [Serializable]
    public class AFS2_File : IAwbFile
    {
        public const int AFS2_SIGNATURE = 844318273;

        [YAXAttributeForClass]
        public byte I_04 { get; set; } = 1;
        [YAXAttributeForClass]
        public PointerSize Pointer_Size { get; set; } = PointerSize.UInt32;
        [YAXAttributeForClass]
        public byte I_06 { get; set; } = 2; //ID size?
        [YAXAttributeForClass]
        public byte I_07 { get; set; }

        [YAXAttributeForClass]
        public int ByteAlignment { get; set; } 
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AudioFile")]
        public AsyncObservableCollection<AFS2_Entry> Entries { get; set; } = new AsyncObservableCollection<AFS2_Entry>();


        public AFS2_File() { }

        /// <summary>
        /// Initialize from an old style CPK AWB.
        /// </summary>
        public AFS2_File(AWB_CPK cpk)
        {
            foreach(var entry in cpk.Entries)
            {
                Entries.Add(new AFS2_Entry(entry));
            }
        }

        #region LoadSave
        public void SortEntries()
        {
            if (Entries != null)
                Entries.Sort((x, y) => x.ID - y.ID);
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
            var asf2File = LoadFromArray(rawBytes, 0, rawBytes.Count());
            YAXSerializer serializer = new YAXSerializer(typeof(AFS2_File));
            serializer.SerializeToFile(asf2File, filePath + ".xml");
        }

        public static AFS2_File LoadFromArray(byte[] rawBytes, int offset = 0, int size = -1)
        {
            if (size == -1)
            {
                size = rawBytes.Length;
            }

            AFS2_File afs2File = new AFS2_File();

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

            if (afs2File.Pointer_Size == PointerSize.UInt64)
                throw new InvalidDataException(string.Format($"AFS2_File.LoadFromArray: Pointer_Size = {afs2File.Pointer_Size} is not supported when loading into memory. AFS2_File.LoadFromStream should be used for these large AWBs."));

            if (afs2File.Pointer_Size != PointerSize.UInt16 && afs2File.Pointer_Size != PointerSize.UInt32)
                throw new InvalidDataException(string.Format($"AFS2_File.LoadFromArray: Pointer_Size = {afs2File.Pointer_Size} is not recognized!"));

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
                    ID = BitConverter.ToUInt16(rawBytes, cueIdOffset)
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

        public static AFS2_File LoadFromStream(string path)
        {
            AFS2_File afs2File = new AFS2_File();

            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                using (BinaryReader stream = new BinaryReader(fs))
                {
                    //Header
                    if (stream.ReadInt32() != AFS2_SIGNATURE)
                        throw new InvalidDataException(string.Format("AFS2_File.LoadFromStream: AFS2 Signature not found!"));

                    afs2File.I_04 = stream.ReadByte();
                    afs2File.Pointer_Size = (PointerSize)stream.ReadByte();
                    afs2File.I_06 = stream.ReadByte();
                    afs2File.I_07 = stream.ReadByte();
                    int count = stream.ReadInt32();
                    afs2File.ByteAlignment = stream.ReadInt32();

                    if(afs2File.Pointer_Size != PointerSize.UInt16 && afs2File.Pointer_Size != PointerSize.UInt32 && afs2File.Pointer_Size != PointerSize.UInt64)
                        throw new InvalidDataException(string.Format($"AFS2_File.LoadFromStream: Pointer_Size = {afs2File.Pointer_Size} is not recognized!"));

                    //AWB entry IDs
                    for (int i = 0; i < count; i++)
                    {
                        afs2File.Entries.Add(new AFS2_Entry(stream.ReadUInt16()));
                    }

                    //AWB entry pointers
                    for (int i = 0; i < count; i++)
                    {
                        //to the data of this entry
                        long offset;
                        //to the data of next entry, or in the case of the last entry, the end of the file
                        long nextOffset;

                        if (afs2File.Pointer_Size == PointerSize.UInt16)
                        {
                            offset = stream.ReadUInt16();
                            nextOffset = stream.ReadUInt16();
                        }
                        else if (afs2File.Pointer_Size == PointerSize.UInt32)
                        {
                            offset = stream.ReadUInt32();
                            nextOffset = stream.ReadUInt32();
                        }
                        else if (afs2File.Pointer_Size == PointerSize.UInt64)
                        {
                            offset = stream.ReadInt64();
                            nextOffset = stream.ReadInt64();
                        }
                        else
                        {
                            throw new InvalidDataException(string.Format($"AFS2_File.LoadFromStream: Pointer_Size = {afs2File.Pointer_Size} is not recognized!"));
                        }

                        //Get actual offset, accounting for byte alignment
                        offset = GetActualOffset(offset, afs2File.ByteAlignment);

                        if(i != count - 1)
                            nextOffset = GetActualOffset(nextOffset, afs2File.ByteAlignment);

                        //Save stream pos so we can revert after retrieving the data
                        long pos = stream.BaseStream.Position - (int)afs2File.Pointer_Size;
                        stream.BaseStream.Seek(offset, SeekOrigin.Begin);

                        afs2File.Entries[i].bytes = stream.ReadBytes((int)(nextOffset - offset));
                        stream.BaseStream.Seek(pos, SeekOrigin.Begin);
                    }

                }
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

            //AWB ID list
            for (int i = 0; i < count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(afs2File.Entries[i].ID));
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

            //int headerEnd = bytes.Count;
            int headerEnd = bytes.Count + CalculatePadding(bytes.Count, afs2File.ByteAlignment);

            for (int i = 0; i < count; i++)
            {
                //Fill pointer
                switch (afs2File.Pointer_Size)
                {
                    case PointerSize.UInt16:
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((ushort)bytes.Count()), Pointers[i]);
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
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((ushort)bytes.Count()), finalPointer);
                    break;
                case PointerSize.UInt32:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), finalPointer);
                    break;
            }

            //Validate header size
            if (bytes.Count <= headerEnd)
                headerEnd = bytes.Count - 1;

            header = bytes.GetRange(0, headerEnd).ToArray();

            return bytes.ToArray();
        }

        public void SaveToStream(string path, out byte[] header, out byte[] md5hash)
        {
            string tempPath = path + "_temp";

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (FileStream fs = new FileStream(tempPath, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    List<byte> headerBytes = new List<byte>();

                    //Init
                    int count = (Entries != null) ? Entries.Count : 0;
                    int finalPointer = 0;
                    List<int> Pointers = new List<int>();
                    SortEntries();

                    ulong byteAlignment = (ulong)ByteAlignment;

                    //Calc pointer size
                    PointerSize calcPointerSize = CalculatePointerSize();

                    //Only keep the result if its larger than the current one
                    if (Pointer_Size < calcPointerSize)
                        Pointer_Size = calcPointerSize;

                    //Header
                    headerBytes.AddRange(BitConverter.GetBytes((uint)AFS2_SIGNATURE));
                    headerBytes.Add(I_04);
                    headerBytes.Add((byte)Pointer_Size);
                    headerBytes.Add(I_06);
                    headerBytes.Add(I_07);
                    headerBytes.AddRange(BitConverter.GetBytes((uint)count));
                    headerBytes.AddRange(BitConverter.GetBytes((uint)ByteAlignment));

                    //Awb ID list
                    for (int i = 0; i < count; i++)
                    {
                        headerBytes.AddRange(BitConverter.GetBytes(Entries[i].ID));
                    }

                    //Pointer List
                    for (int i = 0; i < count + 1; i++)
                    {
                        switch (Pointer_Size)
                        {
                            case PointerSize.UInt16:
                                Pointers.Add(headerBytes.Count);
                                headerBytes.AddRange(new byte[2]);
                                break;
                            case PointerSize.UInt32:
                                Pointers.Add(headerBytes.Count);
                                headerBytes.AddRange(new byte[4]);
                                break;
                            case PointerSize.UInt64:
                                Pointers.Add(headerBytes.Count);
                                headerBytes.AddRange(new byte[8]);
                                break;
                            default:
                                throw new Exception(string.Format("Undefined PointerSize encountered: {0}\nSave failed.", Pointer_Size));
                        }
                    }

                    finalPointer = headerBytes.Count - (int)Pointer_Size;
                    int headerEnd = count > 0 ? headerBytes.Count + CalculatePadding(headerBytes.Count, ByteAlignment) : headerBytes.Count;

                    writer.Write(headerBytes.ToArray());

                    //Write data
                    for (int i = 0; i < count; i++)
                    {
                        //Fill pointer
                        switch (Pointer_Size)
                        {
                            case PointerSize.UInt16:
                                headerBytes = Utils.ReplaceRange(headerBytes, BitConverter.GetBytes((ushort)writer.BaseStream.Position), Pointers[i]);
                                break;
                            case PointerSize.UInt32:
                                headerBytes = Utils.ReplaceRange(headerBytes, BitConverter.GetBytes((uint)writer.BaseStream.Position), Pointers[i]);
                                break;
                            case PointerSize.UInt64:
                                headerBytes = Utils.ReplaceRange(headerBytes, BitConverter.GetBytes((ulong)writer.BaseStream.Position), Pointers[i]);
                                break;
                        }

                        //Add padding
                        writer.Write(new byte[CalculatePadding((ulong)(writer.BaseStream.Position), byteAlignment)]);

                        //Add data
                        writer.Write(Entries[i].bytes);
                    }

                    //Fill in final pointer
                    switch (Pointer_Size)
                    {
                        case PointerSize.UInt16:
                            headerBytes = Utils.ReplaceRange(headerBytes, BitConverter.GetBytes((ushort)writer.BaseStream.Position), finalPointer);
                            break;
                        case PointerSize.UInt32:
                            headerBytes = Utils.ReplaceRange(headerBytes, BitConverter.GetBytes((uint)writer.BaseStream.Position), finalPointer);
                            break;
                        case PointerSize.UInt64:
                            headerBytes = Utils.ReplaceRange(headerBytes, BitConverter.GetBytes((ulong)writer.BaseStream.Position), finalPointer);
                            break;
                    }

                    //Add header to file
                    header = headerBytes.ToArray();

                    writer.Seek(0, SeekOrigin.Begin);
                    writer.Write(header);

                    //Calculate hash
                    fs.Seek(0, SeekOrigin.Begin);

                    using (MD5Cng md5 = new MD5Cng())
                    {
                        md5hash = md5.ComputeHash(fs);
                    }
                }
            }

            if (File.Exists(path))
                File.Delete(path);

            //Move newly created AWB file over old one
            File.Move(tempPath, path);

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

        public static long GetActualOffset(long offset, int byteAlignment)
        {
            return offset + (long)CalculatePadding((ulong)offset, (ulong)byteAlignment);
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

        public static ulong CalculatePadding(ulong offset, ulong byteAlignment)
        {
            return (byteAlignment - (offset % byteAlignment)) % byteAlignment;
        }

        private PointerSize CalculatePointerSize()
        {
            //To calculate the pointer size we need to know the final file size... so first that needs to be calculated
            ulong size = CalculateFileSize();

            //Unclear if PointerSize is actually signed or not
            if (size < (ulong)short.MaxValue)
                return PointerSize.UInt16;
            if (size < (ulong)int.MaxValue)
                return PointerSize.UInt32;
            if (size < (ulong)ulong.MaxValue)
                return PointerSize.UInt64;

            if(size > ulong.MaxValue)
            {
                throw new InvalidDataException($"AFS2_File.CalculatePointerSize: The AWB file is too large!\n\nSize in bytes: {size}");
            }

            return PointerSize.UInt32;
        }

        private ulong CalculateFileSize()
        {
            ulong size = 0;
            size += 16; //Header
            size += (ulong)(2 * Entries.Count); //IDs
            size += (ulong)(8 * Entries.Count); //Pointer List. Assume largest possible size
            size += 8; //Final pointer to end of file

            for (int i = 0; i < Entries.Count; i++)
            {
                size += CalculatePadding(size, (ulong)ByteAlignment);
                size += (ulong)Entries[i].bytes.Length;
            }

            return size;
        }

        public int NextID()
        {
            int id = 0;

            foreach (var file in Entries)
            {
                if (id <= file.ID)
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
                if (id == awbTrack.ID) return true;
            }
            return false;
        }

        public void PadWithNullEntries()
        {
            //Keeps all IDs consecutive by adding null entries (need for compatibility with Eternity Audio Tool)

            int maxId = Entries.Max(x => x.ID);

            for (int i = 0; i < maxId; i++)
            {
                if (!Entries.Any(x => x.ID == i))
                    Entries.Add(new AFS2_Entry() { ID = (ushort)i, bytes = new byte[0] });
            }

            SortEntries();
        }

        /// <summary>
        /// Search for an encryption key for any HCA tracks in this AWB, if any.
        /// </summary>
        public ulong TryGetEncrpytionKey()
        {
            return TryGetEncrpytionKey(this);
        }

        /// <summary>
        /// Search for an encryption key for any HCA tracks in this AWB, if any.
        /// </summary>
        public static ulong TryGetEncrpytionKey(AFS2_File awbFile)
        {
            foreach (var track in awbFile.Entries)
            {
                try
                {
                    VGAudio.Containers.Hca.HcaReader reader = new VGAudio.Containers.Hca.HcaReader();

                    if (track.bytes != null)
                    {
                        var header = reader.ParseFile(track.bytes);

                        if (header.EncryptionKey == null) continue;
                        if (header.EncryptionKey.KeyCode != 0) return header.EncryptionKey.KeyCode;
                    }
                }
                catch (InvalidDataException)
                {
                    //Not an HCA file
                }

            }

            return 0;
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
                    ID = newId,
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
                if (file.ID == id) return file;
            }
            return null;
        }

        public ushort GetExistingEntry(AFS2_Entry entry)
        {
            foreach (var _entry in Entries)
            {
                if (Utils.CompareArray(_entry.bytes, entry.bytes)) return _entry.ID;
            }

            return ushort.MaxValue;
        }

        public ushort GetExistingEntry(byte[] bytes)
        {
            foreach (var _entry in Entries)
            {
                if (Utils.CompareArray(bytes, _entry.bytes)) return _entry.ID;
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
                    ID = newId,
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
                Pointer_Size = PointerSize.UInt32
            };
        }
        
        
        
    }

    [YAXSerializeAs("AudioFile")]
    [Serializable]
    public class AFS2_Entry
    {
        [YAXAttributeForClass]
        public ushort ID { get; set; }

        //Bytes:
        private byte[] _bytes = null;
        [YAXAttributeFor("Data")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] bytes
        {
            get { return _bytes; }
            set
            {
                if(_bytes != value)
                {
                    _bytes = value;
                    HcaInfo = new TrackMetadata(value);
                    WavBytes = null;
                }
            }
        }

        //HCA:
        [YAXDontSerialize]
        public TrackMetadata HcaInfo { get; set; }

        //WAV:
        public byte[] WavBytes = null;


        public AFS2_Entry() { }

        public AFS2_Entry(CPK_Entry cpkEntry)
        {
            ID = (ushort)cpkEntry.ID;
            bytes = cpkEntry.Data;
        }

        public AFS2_Entry(ushort id) 
        {
            ID = id;
        }

        public bool BytesIsNullOrEmpty()
        {
            if (bytes == null) return true;
            return bytes.Length == 0;
        }

        public List<IUndoRedo> SetLoop(bool loop, int startMs, int endMs)
        {
            if (HcaInfo == null)
                throw new ArgumentException("SetLoop: HcaInfo isn't loaded.");

            if (HcaInfo.EncodeType != EncodeType.HCA)
                throw new InvalidOperationException("SetLoop: Can only edit loop on HCA encoded tracks.");

            List<IUndoRedo> undos = new List<IUndoRedo>();

            byte[] originalFile = bytes.DeepCopy();
            //awbEntry.bytes = HcaMetadata.SetLoop(awbEntry.bytes, loop, startSeconds, endSeconds);
            bytes = TrackMetadata.EncodeHcaLoop(bytes, loop, startMs, endMs);

            VGAudio.Containers.Hca.HcaReader reader = new VGAudio.Containers.Hca.HcaReader();
            VGAudio.Containers.Hca.HcaStructure header = reader.ParseFile(bytes);

            undos.Add(new UndoableProperty<AFS2_Entry>(nameof(bytes), this, originalFile, bytes.DeepCopy()));

            return undos;
        }
    }
}
