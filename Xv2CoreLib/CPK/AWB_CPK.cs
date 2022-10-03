using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UsefulThings;
using Xv2CoreLib.UTF;
using Xv2CoreLib.AFS2;
using YAXLib;
using _cpk = CriPakTools.CPK;
using Xv2CoreLib.ACB;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.CPK
{
    //CPK Parser solely for cpk-based AWB files.
    //These only use ITOC table, so TOC + ETOC can be ignored.

    public class AWB_CPK : IAwbFile
    {
        public const int CPK_SIGNATURE = 0x204B5043;
        private const int TOC_SIGNATURE = 0x20434F54;
        private const int ITOC_SIGNATURE = 0x434F5449;
        private const int ETOC_SIGNATURE = 0x434F5445;
        private const ulong CRI_SIGNATURE = 5283359157895299072;

        //Header
        public UTF_File CPK_Header { get; set; }
        [YAXAttributeForClass]
        public int CPK_Unk1 { get; set; } = 255;

        //ITOC (regenerate this on save)
        public UTF_File ITOC { get; set; }
        [YAXAttributeForClass]
        public int ITOC_Unk1 { get; set; } = 255;

        //Files
        public AsyncObservableCollection<CPK_Entry> Entries { get; set; } = new AsyncObservableCollection<CPK_Entry>();

        public AWB_CPK() { }

        public AWB_CPK(AFS2_File afs2File, UTF_File cpkHeader)
        {
            CPK_Header = cpkHeader;

            foreach(var entry in afs2File.Entries.OrderBy(x => x.ID))
            {
                Entries.Add(new CPK_Entry(entry.ID, entry.bytes));
            }
        }

        #region LoadSave
        public static AWB_CPK Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            return Load(bytes);
        }
    
        public static AWB_CPK Load(byte[] bytes)
        {
            return Load(bytes, 0, bytes.Length);
        }

        public static AWB_CPK Load(byte[] bytes, int offset, int length)
        {
            AWB_CPK cpk = new AWB_CPK();

            using (MemoryStream stream = new MemoryStream(bytes, offset, length))
            {
                //CPK Header
                if (stream.ReadInt32() != CPK_SIGNATURE)
                    throw new InvalidDataException("CPK Signature not found.");

                cpk.CPK_Unk1 = stream.ReadInt32();
                ulong cpkTableSize = stream.ReadUInt32();
                stream.Seek(4, SeekOrigin.Current);

                cpk.CPK_Header = UTF_File.LoadUtfTable(stream.ReadBytes((int)cpkTableSize), (int)cpkTableSize);

                //ITOC Header
                if (cpk.CPK_Header.ColumnHasValue("ItocOffset"))
                {
                    long itocOffset = (long)cpk.CPK_Header.GetValue<ulong>("ItocOffset", TypeFlag.UInt64, 0);
                    if (itocOffset > 0)
                    {
                        stream.Seek(itocOffset, SeekOrigin.Begin);

                        if (stream.ReadInt32() != ITOC_SIGNATURE)
                            throw new InvalidDataException("ITOC Signature not found.");

                        cpk.ITOC_Unk1 = stream.ReadInt32();
                        ulong itocTableSize = stream.ReadUInt32();
                        stream.Seek(4, SeekOrigin.Current);

                        cpk.ITOC = UTF_File.LoadUtfTable(stream.ReadBytes((int)itocTableSize), (int)itocTableSize);

                        if (cpk.ITOC.TableName != "CpkItocInfo")
                            throw new InvalidDataException($"Unexpected ITOC: {cpk.ITOC.TableName}. Cannot continue.");
                    }
                }

                //TOC
                if (cpk.CPK_Header.ColumnHasValue("TocOffset"))
                {
                    if (cpk.CPK_Header.GetValue<ulong>("TocOffset", TypeFlag.UInt64, 0) != 0)
                    {
                        throw new InvalidDataException($"This cpk has a TOC table, cannot continue.");
                    }
                }

                //ETOC
                if (cpk.CPK_Header.ColumnHasValue("EtocOffset"))
                {
                    if (cpk.CPK_Header.GetValue<ulong>("EtocOffset", TypeFlag.UInt64, 0) != 0)
                    {
                        throw new InvalidDataException($"This cpk has a ETOC table, cannot continue.");
                    }
                }

                //Validate header
                if (!cpk.CPK_Header.ColumnHasValue("Files"))
                    throw new InvalidDataException("\"Files\" column not found or missing value.");

                if (!cpk.CPK_Header.ColumnHasValue("Align"))
                    throw new InvalidDataException("\"Align\" column not found or missing value.");

                if (!cpk.CPK_Header.ColumnHasValue("ContentOffset"))
                    throw new InvalidDataException("\"ContentOffset\" column not found or missing value.");

                uint numFiles = cpk.CPK_Header.GetValue<uint>("Files", TypeFlag.UInt32, 0);
                ulong contentOffset = cpk.CPK_Header.GetValue<ulong>("ContentOffset", TypeFlag.UInt64, 0);
                ushort align = cpk.CPK_Header.GetValue<ushort>("Align", TypeFlag.UInt16, 0);
                ulong currentOffset = contentOffset;

                //Validate ITOC
                if (!cpk.ITOC.ColumnHasValue("FilesL") || !cpk.ITOC.ColumnHasValue("FilesH"))
                {
                    throw new InvalidDataException($"\"FilesL\" or \"FilesH\" column not found or missing value.");
                }

                int numH = (int)cpk.ITOC.GetValue<uint>("FilesH", TypeFlag.UInt32, 0);
                int numL = (int)cpk.ITOC.GetValue<uint>("FilesL", TypeFlag.UInt32, 0);
                UTF_File dataH = cpk.ITOC.GetColumnTable("DataH", true);
                UTF_File dataL = cpk.ITOC.GetColumnTable("DataL", true);

                if (numH + numL != numFiles)
                {
                    throw new InvalidDataException($"FilesH + FilesL does not equal Files. Cannot continue.");
                }

                //Load files
                for(ushort id = 0; id < numFiles; id++)
                {
                    uint fileSize = 0;
                    uint extractSize = 0;
                    bool found = false;

                    //DataL seems to be for files of less than 65535 bytes.
                    for (int i = 0; i < numL; i++)
                    {
                        if (dataL.GetValue<ushort>("ID", TypeFlag.UInt16, i) != id)
                            continue;

                        fileSize = dataL.GetValue<ushort>("FileSize", TypeFlag.UInt16, i);
                        extractSize = dataL.GetValue<ushort>("ExtractSize", TypeFlag.UInt16, i);
                        found = true;
                        break;
                    }

                    if (!found)
                    {
                        for (int i = 0; i < numH; i++)
                        {
                            if (dataH.GetValue<ushort>("ID", TypeFlag.UInt16, i) != id)
                                continue;

                            fileSize = dataH.GetValue<uint>("FileSize", TypeFlag.UInt32, i);
                            extractSize = dataH.GetValue<uint>("ExtractSize", TypeFlag.UInt32, i);
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        if (fileSize == 0) continue; //null entry

                        stream.Seek((long)currentOffset, SeekOrigin.Begin);

                        byte[] data = stream.ReadBytes((int)fileSize);

                        if (data.Length > 8)
                        {
                            string isComp = Encoding.ASCII.GetString(data.GetRange(0, 8));

                            if (isComp == "CRILAYLA")
                            {
                                int size = (int)((extractSize > fileSize) ? extractSize : fileSize);
                                data = _cpk.DecompressCRILAYLA(data, size);
                            }
                        }

                        cpk.Entries.Add(new CPK_Entry(id, data));

                        //Increment offset
                        currentOffset += fileSize;

                        if ((currentOffset % align) != 0)
                        {
                            currentOffset += (align - (currentOffset % align));
                        }
                    }
                    else
                    {
                        throw new Exception($"Could not find the file for ID {id} in either \"DataL\" or \"DataH\".");
                    }

                }

            }


            return cpk;
        }

        public byte[] Write()
        {
            PadWithNullEntries();
            ushort align = CPK_Header.GetValue<ushort>("Align", TypeFlag.UInt16, 0);

            List<byte> bytes = new List<byte>();
            List<byte> contentBytes = new List<byte>();

            //Update header
            ulong contentOffset = 0x800; 
            ulong contentSize = GetContentSize();

            CPK_Header.SetValue("Files", (uint)Entries.Count, TypeFlag.UInt32, 0);
            CPK_Header.SetValue("ContentOffset", contentOffset, TypeFlag.UInt64, 0);
            CPK_Header.SetValue("ContentSize", contentSize, TypeFlag.UInt64, 0);
            CPK_Header.SetValue("ItocOffset", contentOffset + contentSize, TypeFlag.UInt64, 0);

            //ITOC
            int numH = 0;
            int numL = 0;

            //Create DataL/DataH tables
            UTF_File dataH = new UTF_File("CpkItocH");
            UTF_File dataL = new UTF_File("CpkItocL");
            dataL.Columns.Add(new UTF_Column("ID", TypeFlag.UInt16, StorageFlag.PerRow));
            dataL.Columns.Add(new UTF_Column("FileSize", TypeFlag.UInt16, StorageFlag.PerRow));
            dataL.Columns.Add(new UTF_Column("ExtractSize", TypeFlag.UInt16, StorageFlag.PerRow));
            dataH.Columns.Add(new UTF_Column("ID", TypeFlag.UInt16, StorageFlag.PerRow));
            dataH.Columns.Add(new UTF_Column("FileSize", TypeFlag.UInt32, StorageFlag.PerRow));
            dataH.Columns.Add(new UTF_Column("ExtractSize", TypeFlag.UInt32, StorageFlag.PerRow));

            foreach (var entry in Entries)
            {
                if(entry.Data.Length > ushort.MaxValue)
                {
                    dataH.AddValue("FileSize", TypeFlag.UInt32, numH, entry.Data.Length.ToString());
                    dataH.AddValue("ExtractSize", TypeFlag.UInt32, numH, entry.Data.Length.ToString());
                    dataH.AddValue("ID", TypeFlag.UInt16, numH, entry.ID.ToString());
                    numH++;
                }
                else
                {
                    dataL.AddValue("FileSize", TypeFlag.UInt16, numL, entry.Data.Length.ToString());
                    dataL.AddValue("ExtractSize", TypeFlag.UInt16, numL, entry.Data.Length.ToString());
                    dataL.AddValue("ID", TypeFlag.UInt16, numL, entry.ID.ToString());
                    numL++;
                }

                //Write data
                if(entry.Data.Length > 0)
                {
                    contentBytes.AddRange(entry.Data);

                    if ((contentBytes.Count % align) != 0)
                    {
                        contentBytes.AddRange(new byte[(align - (contentBytes.Count % align))]);
                    }
                }
            }

            //Create ITOC table
            ITOC = new UTF_File();
            ITOC.TableName = "CpkItocInfo";
            ITOC.Columns.Add(new UTF_Column("FilesL", TypeFlag.UInt32, StorageFlag.PerRow));
            ITOC.Columns.Add(new UTF_Column("FilesH", TypeFlag.UInt32, StorageFlag.PerRow));
            ITOC.Columns.Add(new UTF_Column("DataL", TypeFlag.Data, StorageFlag.PerRow));
            ITOC.Columns.Add(new UTF_Column("DataH", TypeFlag.Data, StorageFlag.PerRow));

            ITOC.AddValue("FilesL", TypeFlag.UInt32, 0, numL.ToString());
            ITOC.AddValue("FilesH", TypeFlag.UInt32, 0, numH.ToString());
            ITOC.AddData("DataL", 0, dataL.Write());
            ITOC.AddData("DataH", 0, dataH.Write());

            //Create file
            byte[] itocBytes = ITOC.Write();
            CPK_Header.SetValue("ItocSize", itocBytes.Length, TypeFlag.UInt64, 0);
            byte[] headerBytes = CPK_Header.Write();

            bytes.AddRange(BitConverter.GetBytes(CPK_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(CPK_Unk1));
            bytes.AddRange(BitConverter.GetBytes((ulong)headerBytes.Length));
            bytes.AddRange(headerBytes);
            bytes.AddRange(new byte[2040 - bytes.Count]);
            bytes.AddRange(BitConverter.GetBytes((ulong)CRI_SIGNATURE));

            if (bytes.Count != 0x800)
                throw new InvalidDataException("CPK header is wrong size.");

            bytes.AddRange(contentBytes);
            bytes.AddRange(BitConverter.GetBytes(ITOC_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(ITOC_Unk1));
            bytes.AddRange(BitConverter.GetBytes((ulong)itocBytes.Length));
            bytes.AddRange(itocBytes);

            return bytes.ToArray();
        }


        #endregion

        #region Helper
        private ulong GetContentSize()
        {
            ulong size = 0;
            ushort align = CPK_Header.GetValue<ushort>("Align", TypeFlag.UInt16, 0);

            foreach(var entry in Entries)
            {
                size += (ulong)entry.Data?.Length;

                //byte alignment
                if ((size % align) != 0)
                {
                    size += (align - (size % align));
                }
            }

            return size;
        }

        private void PadWithNullEntries()
        {
            //Keeps all IDs consecutive by adding null entries (need for compatibility with Eternity Audio Tool)

            int maxId = Entries.Max(x => x.ID);

            for (int i = 0; i < maxId; i++)
            {
                if (!Entries.Any(x => x.ID == i))
                    Entries.Add(new CPK_Entry(i, new byte[0]));
            }

            SortEntries();
        }

        private void SortEntries()
        {
            if (Entries != null)
                Entries.Sort((x, y) => x.ID - y.ID);
        }
        #endregion

    }

    public class CPK_Entry
    {
        [YAXAttributeForClass]
        public ushort ID { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] Data { get; set; }

        public CPK_Entry(int id, byte[] data)
        {
            ID = (ushort)id;
            Data = data;
        }
    }
}
