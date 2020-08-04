using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using Xv2CoreLib.Resource;
using Xv2CoreLib.AFS2;

namespace Xv2CoreLib.UTF
{
    /// <summary>
    /// Represents CRIWARE's UTF format.
    /// </summary>
    [Serializable]
    public class UTF_File
    {
        public UTF_File()
        {
            EncodingType = _EncodingType.UTF8;
        }

        public UTF_File(string name)
        {
            TableName = name;
            EncodingType = _EncodingType.UTF8;
        }

        public const uint UTF_SIGNATURE = 1079333958; //Big Endian (@UTF)
        public const uint AFS2_SIGNATURE = 1095127858;

        [YAXAttributeForClass]
        public string TableName { get; set; }
        [YAXAttributeForClass]
        public byte I_00 { get; set; }
        [YAXAttributeForClass]
        public _EncodingType EncodingType { get; set; }
        [YAXAttributeForClass]
        public int DefaultRowCount { get; set; } //This is used for when there are no actual rows but a count is set regardless... Instead of writing 0 we write the original value
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Column")]
        public List<UTF_Column> Columns { get; set; } = new List<UTF_Column>();
#if !DEBUG
        [YAXDontSerialize]
#endif
        public List<DataInfo> DataInfos { get; set; }
        

        public static void ParseUtfFile(string filePath)
        {
            byte[] rawBytes = File.ReadAllBytes(filePath);
            var utfFile = LoadUtfTable(rawBytes, rawBytes.Length);
            YAXSerializer serializer = new YAXSerializer(typeof(UTF_File));
            serializer.SerializeToFile(utfFile, filePath + ".xml");
        }

        public static void SaveUtfFile(string filePath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath));
            YAXSerializer serializer = new YAXSerializer(typeof(UTF_File), YAXSerializationOptions.DontSerializeNullObjects);
            UTF_File utfFile = (UTF_File)serializer.DeserializeFromFile(filePath);
            byte[] bytes = WriteUtfTable(utfFile, true);
            File.WriteAllBytes(saveLocation, bytes);
        }

        public static UTF_File LoadUtfTable(byte[] rawBytes, int tableSize, int offset = 0)
        {
            //Init
            List<DataInfo> dataInfo = new List<DataInfo>();
            int tableStartOffset = offset;

            UTF_File utfFile = new UTF_File();
            
            uint signature = BigEndianConverter.ReadUInt32(rawBytes, offset + 0);

            //Check encryption
            if(signature != UTF_SIGNATURE)
            {
                rawBytes = DecryptUTF(rawBytes); //Decrypt the file
                signature = BigEndianConverter.ReadUInt32(rawBytes, offset + 0); //Update signature with the newly decrypted file
            }

            //Check signature
            if (signature != UTF_SIGNATURE)
            {
                throw new InvalidDataException("\"@UTF\" Signature not found. Parse failed.");
            }

            if (BigEndianConverter.ReadInt32(rawBytes, offset + 4) != 0)
            {
                tableSize = BigEndianConverter.ReadInt32(rawBytes, offset + 4) + 8;
            }

            

            utfFile.I_00 = rawBytes[offset + 8];
            utfFile.EncodingType = (_EncodingType)rawBytes[offset + 9];

            uint rowOffset = (uint)(BigEndianConverter.ReadUInt16(rawBytes, offset + 10) + offset + 8);
            uint stringsOffset = (uint)(BigEndianConverter.ReadUInt32(rawBytes, offset + 12) + offset + 8);
            uint DataOffset = (uint)(BigEndianConverter.ReadUInt32(rawBytes, offset + 16) + offset + 8);
            uint ColumnCount = BigEndianConverter.ReadUInt16(rawBytes, offset + 24);
            uint RowLength = BigEndianConverter.ReadUInt16(rawBytes, offset + 26);
            uint RowCount = BigEndianConverter.ReadUInt32(rawBytes, offset + 28);
            utfFile.DefaultRowCount = (int)RowCount;

            //Table Name
            if(BigEndianConverter.ReadInt32(rawBytes, offset + 20) != -1)
            {
                utfFile.TableName = Utils.GetStringUtf(rawBytes, (int)(stringsOffset + BigEndianConverter.ReadInt32(rawBytes, offset + 20)));
                
            }

            //Columns
            int rowPosition = 0;
            offset += 32;
            utfFile.Columns = new List<UTF_Column>();
            for(int i = 0; i < ColumnCount; i++)
            {
                UTF_Column Column = new UTF_Column();

                int offsetToName = (int)(BigEndianConverter.ReadUInt32(rawBytes, offset + 1) + stringsOffset);

                //Flag
                byte flag_a = Int4Converter.ToInt4(rawBytes[offset])[0];
                byte flag_b = Int4Converter.ToInt4(rawBytes[offset])[1];
                Column.StorageFlag = (StorageFlag)flag_b;
                Column.TypeFlag = (TypeFlag)flag_a;
                
                Column.Name = Utils.GetStringUtf(rawBytes, offsetToName);
                
                if(Column.StorageFlag == StorageFlag.Constant)
                {
                    int accessOffset = offset + 5;
                    switch (Column.TypeFlag)
                    {
                        case TypeFlag.UInt8:
                            Column.Constant = rawBytes[accessOffset].ToString();
                            offset++;
                            break;
                        case TypeFlag.Int8:
                            Column.Constant = Convert.ToSByte(rawBytes[accessOffset]).ToString();
                            offset++;
                            break;
                        case TypeFlag.UInt16:
                            Column.Constant = BigEndianConverter.ReadUInt16(rawBytes, accessOffset).ToString();
                            offset += 2;
                            break;
                        case TypeFlag.Int16:
                            Column.Constant = BigEndianConverter.ReadInt16(rawBytes, accessOffset).ToString();
                            offset += 2;
                            break;
                        case TypeFlag.UInt32:
                            Column.Constant = BigEndianConverter.ReadUInt32(rawBytes, accessOffset).ToString();
                            offset += 4;
                            break;
                        case TypeFlag.Int32:
                            Column.Constant = BigEndianConverter.ReadInt32(rawBytes, accessOffset).ToString();
                            offset += 4;
                            break;
                        case TypeFlag.UInt64:
                            Column.Constant = BigEndianConverter.ReadUInt64(rawBytes, accessOffset).ToString();
                            offset += 8;
                            break;
                        case TypeFlag.Int64:
                            Column.Constant = BigEndianConverter.ReadInt64(rawBytes, accessOffset).ToString();
                            offset += 8;
                            break;
                        case TypeFlag.Single:
                            Column.Constant = BigEndianConverter.ReadSingle(rawBytes, accessOffset).ToString("0.0");
                            offset += 4;
                            break;
                        case TypeFlag.Double:
                            Column.Constant = BigEndianConverter.ReadDouble(rawBytes, accessOffset).ToString("0.0");
                            offset += 8;
                            break;
                        case TypeFlag.GUID:
                            Column.Bytes = rawBytes.GetRange(offset, 16);
                            offset += 16;
                            break;
                        case TypeFlag.String:
                            int strOffset = (int)(BigEndianConverter.ReadInt32(rawBytes, accessOffset) + stringsOffset);
                            Column.Constant = Utils.GetStringUtf(rawBytes, strOffset);
                            offset += 4;
                            break;
                        case TypeFlag.Data:
                            int _dataOffset = (int)(BigEndianConverter.ReadInt32(rawBytes, accessOffset) + DataOffset);
                            int _dataLength = BigEndianConverter.ReadInt32(rawBytes, accessOffset + 4);
                            dataInfo.Add(new DataInfo()
                            {
                                ColumnIndex = i,
                                Offset = _dataOffset,
                                relativeOffset = BigEndianConverter.ReadInt32(rawBytes, accessOffset),
                                Length = _dataLength,
                                DEBUG_COLUMN_OFFSET = offset,
                                Name = Column.Name,
                                dataType = StorageFlag.Constant,
                                originalLength = _dataLength,
                                RowOffset = -1
                            });
                            offset += 8;
                            break;
                        default:
                            throw new Exception(String.Format("Unknown TypeFlag Encountered: {0}", Column.TypeFlag));
                    }
                }
                else if (Column.StorageFlag == StorageFlag.PerRow)
                {
                    //Row access
                    Column.Rows = new List<UTF_Row>();
                    int accessOffset = (int)(rowOffset + rowPosition);
                    for(int a = 0; a < RowCount; a++)
                    {
                        switch (Column.TypeFlag)
                        {
                            case TypeFlag.UInt8:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = rawBytes[accessOffset].ToString(),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.Int8:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = Convert.ToSByte(rawBytes[accessOffset]).ToString(),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.UInt16:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = BigEndianConverter.ReadUInt16(rawBytes, accessOffset).ToString(),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.Int16:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = BigEndianConverter.ReadInt16(rawBytes, accessOffset).ToString(),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.UInt32:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = BigEndianConverter.ReadUInt32(rawBytes, accessOffset).ToString(),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.Int32:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = BigEndianConverter.ReadInt32(rawBytes, accessOffset).ToString(),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.UInt64:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = BigEndianConverter.ReadUInt64(rawBytes, accessOffset).ToString(),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.Int64:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = BigEndianConverter.ReadInt64(rawBytes, accessOffset).ToString(),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.Single:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = BigEndianConverter.ReadSingle(rawBytes, accessOffset).ToString("0.0"),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.Double:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = BigEndianConverter.ReadDouble(rawBytes, accessOffset).ToString("0.0"),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.GUID:
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Bytes = rawBytes.GetRange(rowPosition, 16),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.String:
                                int strOffset = (int)(BigEndianConverter.ReadInt32(rawBytes, accessOffset) + stringsOffset);
                                Column.Rows.Add(new UTF_Row()
                                {
                                    Value = Utils.GetStringUtf(rawBytes, strOffset),
                                    RowIndex = a
                                });
                                break;
                            case TypeFlag.Data:
                                Column.Rows.Add(new UTF_Row()

                                {//This is filled in later
                                    RowIndex = a
                                });
                                int _dataOffset = (int)(BigEndianConverter.ReadInt32(rawBytes, accessOffset) + DataOffset);
                                int _dataLength = BigEndianConverter.ReadInt32(rawBytes, accessOffset + 4);
                                dataInfo.Add(new DataInfo()
                                {
                                    ColumnIndex = i,
                                    Offset = _dataOffset,
                                    relativeOffset = BigEndianConverter.ReadInt32(rawBytes, accessOffset),
                                    Length = _dataLength,
                                    DEBUG_COLUMN_OFFSET = offset,
                                    DEBUG_ROW_OFFSET = accessOffset,
                                    RowIndex = a, 
                                    Name = Column.Name,
                                    dataType = StorageFlag.PerRow,
                                    originalLength = _dataLength,
                                    RowOffset = accessOffset
                                });
                                break;
                            default:
                                throw new Exception(String.Format("Unknown TypeFlag Encountered: {0}", Column.TypeFlag));
                        }
                        accessOffset += (int)RowLength;
                    }

                    //If theres atleast 1 ROW, then increament rowPosition (possible bug fix?)
                    if(Column.Rows.Count > 0)
                    {
                        switch (Column.TypeFlag)
                        {
                            case TypeFlag.UInt8:
                            case TypeFlag.Int8:
                                rowPosition += 1;
                                break;
                            case TypeFlag.Int16:
                            case TypeFlag.UInt16:
                                rowPosition += 2;
                                break;
                            case TypeFlag.Int32:
                            case TypeFlag.UInt32:
                            case TypeFlag.Single:
                            case TypeFlag.String:
                                rowPosition += 4;
                                break;
                            case TypeFlag.Data:
                            case TypeFlag.Double:
                            case TypeFlag.UInt64:
                            case TypeFlag.Int64:
                                rowPosition += 8;
                                break;
                            case TypeFlag.GUID:
                                rowPosition += 16;
                                break;
                        }
                    }

                }
                else
                {
                    throw new Exception(String.Format("Unknow StorageFlag: {0}", Column.StorageFlag));
                }

                utfFile.Columns.Add(Column);
                offset += 5;
            }

            //Order DataInfo list so the length calculations are accurate
            dataInfo = DataInfo.OrderDataInfo(dataInfo, (int)RowCount);

            //Calculate the actual length of data objects and parse them
            //Lengths will only be calculated if they are declared as 0 AND they belong to a UtfTable/AFS2 file (or one of them is next in the data section, but in that case the calclation will confirm its length 0)
            bool isDataBeforeLast = false;
            for(int i = 0; i < dataInfo.Count; i++)
            {
                //Get the length for data that does not have it declared (this is a little inaccurate as it counts padding bytes)
                if(i != dataInfo.Count - 1)
                {
                    //This is not the last dataInfo entry
                    if(dataInfo[i].Length == 0 && IsUtfTableOrAfs2(rawBytes, dataInfo[i].Offset))
                    {
                        if (dataInfo[i + 1].relativeOffset == dataInfo[i].relativeOffset && i == 0)
                        {
                            dataInfo[i].Length = 0;
                            dataInfo[i].DEBUG_MODE = "1_1";
                        }
                        else if(dataInfo[i].relativeOffset == 0 && i != 0)
                        {
                            dataInfo[i].Length = 0;
                            dataInfo[i].DEBUG_MODE = "1_15";
                        }
                        else if (dataInfo[i + 1].relativeOffset == 0)
                        {
                            //Next entry is null, so we need to find the offset for the next data
                            bool foundData = false;
                            for(int a = i + 2; a < dataInfo.Count; a++)
                            {
                                if(dataInfo[a].relativeOffset != 0)
                                {
                                    foundData = true;
                                    isDataBeforeLast = true;
                                    dataInfo[i].Length = (dataInfo[a].Offset - dataInfo[i].Offset);
                                    dataInfo[i].DEBUG_MODE = "1_2";
                                    break;
                                }
                            }

                            if(foundData == false)
                            {
                                //Data was not found, so compare against tableSize instead.
                                int a = tableStartOffset + tableSize;
                                isDataBeforeLast = true;
                                dataInfo[i].Length = a - dataInfo[i].Offset;
                                dataInfo[i].DEBUG_MODE = "1_3";
                            }
                        }
                        else
                        {
                            dataInfo[i].Length = (dataInfo[i + 1].Offset - dataInfo[i].Offset);
                            dataInfo[i].DEBUG_MODE = "1_4";
                        }
                    }
                    else
                    {
                        isDataBeforeLast = true;
                    }
                }
                else
                {
                    //This is the last dataInfo entry
                    if (dataInfo[i].Length == 0 && IsUtfTableOrAfs2(rawBytes, dataInfo[i].Offset))
                    {
                        if (isDataBeforeLast && dataInfo[i].relativeOffset == 0)
                        {
                            dataInfo[i].Length = 0;
                        }
                        else
                        {
                            int a = tableStartOffset + tableSize;
                            dataInfo[i].Length = a - dataInfo[i].Offset;
                            dataInfo[i].DEBUG_MODE = "2_1";
                        }
                        
                    }
                }

                //Parse the data
                if(dataInfo[i].dataIsNull == false)
                {
                    //if (!Directory.Exists("extracted"))
                    //{
                    //    Directory.CreateDirectory("extracted");
                    //}
                    //File.WriteAllBytes(String.Format("extracted/{0}", dataInfo[i].Name), bytes.GetRange(dataInfo[i].Offset, dataInfo[i].Length).ToArray());

                    if (BigEndianConverter.ReadUInt32(rawBytes, dataInfo[i].Offset) == UTF_SIGNATURE)
                    {
                        //Data is a UTF Table
                        if(utfFile.Columns[dataInfo[i].ColumnIndex].StorageFlag == StorageFlag.PerRow)
                        {
                            utfFile.Columns[dataInfo[i].ColumnIndex].Rows[dataInfo[i].RowIndex].UtfTable = UTF_File.LoadUtfTable(rawBytes, dataInfo[i].Length, dataInfo[i].Offset);
                        }
                        else
                        {
                            utfFile.Columns[dataInfo[i].ColumnIndex].UtfTable = UTF_File.LoadUtfTable(rawBytes, dataInfo[i].Length, dataInfo[i].Offset);
                        }
                    }
                    else if(BitConverter.ToInt32(rawBytes, dataInfo[i].Offset) == AFS2_File.AFS2_SIGNATURE && utfFile.Columns[dataInfo[i].ColumnIndex].Name == "AwbFile")
                    {
                        //Data is AFS2 file (awb) on column "AwbFile"
                        if (utfFile.Columns[dataInfo[i].ColumnIndex].StorageFlag == StorageFlag.PerRow)
                        {
                            utfFile.Columns[dataInfo[i].ColumnIndex].Rows[dataInfo[i].RowIndex].Afs2File = AFS2_File.LoadAfs2File(rawBytes, dataInfo[i].Offset, dataInfo[i].Length);
                        }
                        else
                        {
                            utfFile.Columns[dataInfo[i].ColumnIndex].Afs2File = AFS2_File.LoadAfs2File(rawBytes, dataInfo[i].Offset, dataInfo[i].Length);
                        }
                    }
                    else
                    {
                        //Data is of unknown type
                        if(utfFile.Columns[dataInfo[i].ColumnIndex].StorageFlag == StorageFlag.PerRow)
                        {
                            utfFile.Columns[dataInfo[i].ColumnIndex].Rows[dataInfo[i].RowIndex].Bytes = rawBytes.GetRange(dataInfo[i].Offset, dataInfo[i].Length);
                        }
                        else
                        {
                            utfFile.Columns[dataInfo[i].ColumnIndex].Bytes = rawBytes.GetRange(dataInfo[i].Offset, dataInfo[i].Length);
                        }
                    }
                }
                
            }

            if(rowPosition != RowLength)
            {
                throw new Exception("rowPosition is out of sync. \nParse failed.");
            }

            utfFile.DataInfos = dataInfo;

            return utfFile;
        }

        public void Save(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllBytes(path, WriteUtfTable(this, true));
        }

        public static byte[] WriteUtfTable(UTF_File utfFile, bool usePadding = false)
        {
            List<byte> bytes = new List<byte>();

            //Init
            int ColumnCount = (utfFile.Columns != null) ? utfFile.Columns.Count : 0;
            int RowCount = utfFile.RowCount();
            int[] RowLength = new int[RowCount];
            List<StringWriteObject> stringWriter = new List<StringWriteObject>();
            List<DataWriteObject> dataWriter = new List<DataWriteObject>();

            //Header (size 8)
            bytes.AddRange(BigEndianConverter.GetBytes(UTF_SIGNATURE));
            bytes.AddRange(new byte[4]); //Table size, fill this in later

            //UTF Table Header (size 24)
            bytes.Add(utfFile.I_00);
            bytes.Add((byte)utfFile.EncodingType);
            bytes.AddRange(new byte[10]); //Offsets to strings, data, row etc (fill in later)
            stringWriter.Add(new StringWriteObject() { Str = utfFile.TableName, OffsetToReplace = 20 } );
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BigEndianConverter.GetBytes((ushort)ColumnCount)); //24
            bytes.AddRange(BigEndianConverter.GetBytes((ushort)0)); //26
            bytes.AddRange(BigEndianConverter.GetBytes(RowCount)); //28

            //Fields/Columns
            for(int i = 0; i < ColumnCount; i++)
            {
                byte flag = Int4Converter.GetByte((byte)utfFile.Columns[i].TypeFlag, (byte)utfFile.Columns[i].StorageFlag);

                //Column
                bytes.Add(flag); //Flag
                stringWriter.Add(new StringWriteObject() { OffsetToReplace = bytes.Count, Str = utfFile.Columns[i].Name, rowIndex = -2 }); //Ignore the relative offset field for now - fill that in later.
                bytes.AddRange(new byte[4]); //Name offset

                //Constants
                if(utfFile.Columns[i].StorageFlag == StorageFlag.Constant)
                {
                    switch (utfFile.Columns[i].TypeFlag)
                    {
                        case TypeFlag.UInt8:
                            bytes.Add(byte.Parse(utfFile.Columns[i].Constant));
                            break;
                        case TypeFlag.Int8:
                            bytes.Add(byte.Parse(utfFile.Columns[i].Constant));
                            break;
                        case TypeFlag.UInt16:
                            bytes.AddRange(BigEndianConverter.GetBytes(UInt16.Parse(utfFile.Columns[i].Constant)));
                            break;
                        case TypeFlag.Int16:
                            bytes.AddRange(BigEndianConverter.GetBytes(Int16.Parse(utfFile.Columns[i].Constant)));
                            break;
                        case TypeFlag.UInt32:
                            bytes.AddRange(BigEndianConverter.GetBytes(UInt32.Parse(utfFile.Columns[i].Constant)));
                            break;
                        case TypeFlag.Int32:
                            bytes.AddRange(BigEndianConverter.GetBytes(Int32.Parse(utfFile.Columns[i].Constant)));
                            break;
                        case TypeFlag.UInt64:
                            bytes.AddRange(BigEndianConverter.GetBytes(UInt64.Parse(utfFile.Columns[i].Constant)));
                            break;
                        case TypeFlag.Int64:
                            bytes.AddRange(BigEndianConverter.GetBytes(Int64.Parse(utfFile.Columns[i].Constant)));
                            break;
                        case TypeFlag.Single:
                            bytes.AddRange(BigEndianConverter.GetBytes(Single.Parse(utfFile.Columns[i].Constant)));
                            break;
                        case TypeFlag.Double:
                            bytes.AddRange(BigEndianConverter.GetBytes(Double.Parse(utfFile.Columns[i].Constant)));
                            break;
                        case TypeFlag.GUID:
                            if(utfFile.Columns[i].Bytes.Length != 16)
                            {
                                throw new InvalidDataException("Invalid GUID size.");
                            }
                            bytes.AddRange(utfFile.Columns[i].Bytes);
                            break;
                        case TypeFlag.String:
                            stringWriter.Add(new StringWriteObject() { OffsetToReplace = bytes.Count, Str = utfFile.Columns[i].Constant, rowIndex = -1 } );
                            bytes.AddRange(new byte[4]);
                            break;
                        case TypeFlag.Data:
                            int dataLength = 0;
                            byte[] dataBytes = null;

                            if(utfFile.Columns[i].UtfTable != null)
                            {
                                dataBytes = WriteUtfTable(utfFile.Columns[i].UtfTable);
                            }
                            else if (utfFile.Columns[i].Afs2File != null)
                            {
                                byte[] hdr;
                                dataBytes = AFS2_File.WriteAfs2File(utfFile.Columns[i].Afs2File, out hdr);
                            }
                            else
                            {
                                dataBytes = utfFile.Columns[i].Bytes;
                            }

                            //Check if null
                            if (dataBytes != null)
                            {
                                dataLength = dataBytes.Count();
                            }

                            dataWriter.Add(new DataWriteObject() { bytes = dataBytes, OffsetToReplace = bytes.Count, storageFlag = StorageFlag.Constant, rowIndex = -1 });
                            
                            bytes.AddRange(new byte[4]);
                            bytes.AddRange(BigEndianConverter.GetBytes((UInt32)dataLength));

                            break;
                        default:
                            throw new Exception(String.Format("Unknown TypeFlag: {0}", utfFile.Columns[i].TypeFlag));
                    }
                }
                
            }

            //Rows
            bytes = Utils.ReplaceRange(bytes, BigEndianConverter.GetBytes((UInt16)(bytes.Count() - 8)), 10); //Fill in row offset (this needs to be set even if no rows exist)
            if (RowCount > 0)
            {
                for (int a = 0; a < RowCount; a++)
                {
                    for (int i = 0; i < ColumnCount; i++)
                    {
                        if (utfFile.Columns[i].StorageFlag == StorageFlag.PerRow)
                        {
                            switch (utfFile.Columns[i].TypeFlag)
                            {
                                case TypeFlag.UInt8:
                                    bytes.Add(byte.Parse(utfFile.Columns[i].Rows[a].Value));
                                    RowLength[a] += 1;
                                    break;
                                case TypeFlag.Int8:
                                    bytes.Add((byte)SByte.Parse(utfFile.Columns[i].Rows[a].Value));
                                    RowLength[a] += 1;
                                    break;
                                case TypeFlag.UInt16:
                                    bytes.AddRange(BigEndianConverter.GetBytes(UInt16.Parse(utfFile.Columns[i].Rows[a].Value)));
                                    RowLength[a] += 2;
                                    break;
                                case TypeFlag.Int16:
                                    bytes.AddRange(BigEndianConverter.GetBytes(Int16.Parse(utfFile.Columns[i].Rows[a].Value)));
                                    RowLength[a] += 2;
                                    break;
                                case TypeFlag.UInt32:
                                    bytes.AddRange(BigEndianConverter.GetBytes(UInt32.Parse(utfFile.Columns[i].Rows[a].Value)));
                                    RowLength[a] += 4;
                                    break;
                                case TypeFlag.Int32:
                                    bytes.AddRange(BigEndianConverter.GetBytes(Int32.Parse(utfFile.Columns[i].Rows[a].Value)));
                                    RowLength[a] += 4;
                                    break;
                                case TypeFlag.UInt64:
                                    bytes.AddRange(BigEndianConverter.GetBytes(UInt64.Parse(utfFile.Columns[i].Rows[a].Value)));
                                    RowLength[a] += 8;
                                    break;
                                case TypeFlag.Int64:
                                    bytes.AddRange(BigEndianConverter.GetBytes(Int64.Parse(utfFile.Columns[i].Rows[a].Value)));
                                    RowLength[a] += 8;
                                    break;
                                case TypeFlag.Single:
                                    bytes.AddRange(BigEndianConverter.GetBytes(Single.Parse(utfFile.Columns[i].Rows[a].Value)));
                                    RowLength[a] += 4;
                                    break;
                                case TypeFlag.Double:
                                    bytes.AddRange(BigEndianConverter.GetBytes(Double.Parse(utfFile.Columns[i].Rows[a].Value)));
                                    RowLength[a] += 8;
                                    break;
                                case TypeFlag.GUID:
                                    bytes.AddRange(utfFile.Columns[i].Rows[a].Bytes);
                                    RowLength[a] += 16;
                                    break;
                                case TypeFlag.String:
                                    stringWriter.Add(new StringWriteObject() { OffsetToReplace = bytes.Count, Str = utfFile.Columns[i].Rows[a].Value, rowIndex = a });
                                    bytes.AddRange(new byte[4]);
                                    RowLength[a] += 4;
                                    break;
                                case TypeFlag.Data:
                                    int dataLength = 0;
                                    byte[] dataBytes = null;

                                    if (utfFile.Columns[i].Rows[a].UtfTable != null)
                                    {
                                        dataBytes = WriteUtfTable(utfFile.Columns[i].Rows[a].UtfTable);
                                    }
                                    else if (utfFile.Columns[i].Rows[a].Afs2File != null)
                                    {
                                        byte[] hdr;
                                        dataBytes = AFS2_File.WriteAfs2File(utfFile.Columns[i].Rows[a].Afs2File, out hdr);
                                    }
                                    else
                                    {
                                        dataBytes = utfFile.Columns[i].Rows[a].Bytes;
                                    }

                                    //Check if null
                                    if (dataBytes != null)
                                    {
                                        dataLength = dataBytes.Count();
                                    }

                                    dataWriter.Add(new DataWriteObject() { bytes = dataBytes, OffsetToReplace = bytes.Count, storageFlag = StorageFlag.PerRow, rowIndex = a });

                                    bytes.AddRange(new byte[4]);
                                    bytes.AddRange(BigEndianConverter.GetBytes((UInt32)dataLength));
                                    RowLength[a] += 8;

                                    break;
                                default:
                                    throw new Exception(String.Format("Unknown TypeFlag: {0}", utfFile.Columns[i].TypeFlag));
                            }
                        }
                    }
                }

                ValidateRowLengths(RowLength); //Validate row lengths to ensure they are all the same length
                bytes = Utils.ReplaceRange(bytes, BigEndianConverter.GetBytes((UInt16)RowLength[0]), 26); //Fill in row length in header
            }
            

            //Strings
            int stringSectionStart = bytes.Count();
            bytes = Utils.ReplaceRange(bytes, BigEndianConverter.GetBytes((UInt32)(bytes.Count() - 8)), 12);
            for(int i = 0; i < stringWriter.Count; i++)
            {
                int relativeOffset = bytes.Count() - stringSectionStart;
                bytes = Utils.ReplaceRange(bytes, BigEndianConverter.GetBytes((UInt32)relativeOffset), stringWriter[i].OffsetToReplace);

                if (!String.IsNullOrWhiteSpace(stringWriter[i].Str))
                {
                    bytes.AddRange(Encoding.UTF8.GetBytes(stringWriter[i].Str));
                }
                bytes.Add(0); //Null byte is added regardless of if string is empty or not
            }

            if (usePadding)
            {
                int strEndPadding = Utils.CalculatePadding(bytes.Count, 32);
                if (strEndPadding > 0) bytes.AddRange(new byte[strEndPadding]);
            }
            
            //Data
            int dataSectionStart = bytes.Count();
            bytes = Utils.ReplaceRange(bytes, BigEndianConverter.GetBytes((UInt32)(bytes.Count() - 8)), 16);
            for (int i = 0; i < dataWriter.Count; i++)
            {
                if (dataWriter[i].bytes != null)
                {
                    //Some files fill in the offset when its null, others dont...
                    int relativeOffset = bytes.Count() - dataSectionStart;
                    bytes = Utils.ReplaceRange(bytes, BigEndianConverter.GetBytes((UInt32)relativeOffset), dataWriter[i].OffsetToReplace);

                    bytes.AddRange(dataWriter[i].bytes);

                    //Check if padding is required
                    int requiredPadding = Utils.CalculatePadding(bytes.Count, 32);

                    //Padding isn't required, so force it (official tools do this for some reason)
                    if(requiredPadding == 0 && usePadding)
                    {
                        bytes.Add(0);
                        requiredPadding = Utils.CalculatePadding(bytes.Count, 32);
                    }

                    //Add padding
                    if (requiredPadding > 0 && usePadding && bytes.Count > 0)
                    {
                        bytes.AddRange(new byte[requiredPadding]);
                    }
                }
            }

            

            //Padding to 4-byte alignment
            int padding = Utils.CalculatePadding(bytes.Count, 4);
            if(padding > 0)
                bytes.AddRange(new byte[padding]);

            bytes = Utils.ReplaceRange(bytes, BigEndianConverter.GetBytes((UInt32)bytes.Count() - 8), 4); //Fill in table size in header


            return bytes.ToArray();
        }

        public byte[] Write(bool usePadding = false)
        {
            return WriteUtfTable(this, usePadding);
        }

        public static bool IsUtfTableOrAfs2(byte[] rawBytes, int offset)
        {
            if (BigEndianConverter.ReadUInt32(rawBytes, offset) == UTF_SIGNATURE || BigEndianConverter.ReadUInt32(rawBytes, offset) == AFS2_SIGNATURE) return true;
            return false;
        }

        /// <summary>
        /// Validates the rows for this table (if they are out of sync, an exception is raised) and then returns it as an integer.
        /// </summary>
        /// <returns></returns>
        public int RowCount()
        {
            if (Columns == null) return DefaultRowCount;
            int rowCount = -1;

            foreach(var column in Columns)
            {

                if(column.StorageFlag == StorageFlag.PerRow && column.Rows == null)
                {
                    throw new InvalidDataException(String.Format("Invalid rows at column ({0}). StorageType is set to PerRow, but no rows were found!", column.Name));
                }
                else if (column.StorageFlag == StorageFlag.PerRow)
                {
                    if(column.Rows.Count() != rowCount && rowCount != -1)
                    {
                        throw new InvalidDataException(String.Format("Invalid rows at column ({0}). Expected {2} rows, but found {1}.", column.Name, column.Rows.Count(), rowCount));
                    }
                    else
                    {
                        rowCount = column.Rows.Count();
                    }
                }
            }

            if(rowCount == -1)
            {
                return DefaultRowCount;
            }
            return rowCount;
        }
        
        public static void ValidateRowLengths(int[] rowLengths)
        {
            int length = rowLengths[0];
            foreach(int i in rowLengths)
            {
                if(i != length)
                {
                    throw new Exception("RowLength is out of sync, rebuild failed.");
                }
            }
        }

        public static byte[] DecryptUTF(byte[] buf)
        {
            Console.WriteLine("Decrypting UTF...");
            Int32 m, t;
            byte d;

            m = 0x0000655f;
            t = 0x00004115;

            for (int i = 0; i < buf.Length; i++)
            {
                d = buf[i];
                d = (byte)(d ^ (byte)(m & 0xff));
                buf[i] = d;
                m *= t;
            }

            return buf;
        }
        

        //Column Manipulation
        public bool ColumnExists(string name)
        {
            if (Columns == null) return false;

            foreach (var column in Columns)
            {
                if (column.Name == name) return true;
            }

            return false;
        }

        public bool ColumnExists(string columnName, string childColumnName)
        {
            if (Columns == null) return false;

            var parent = GetColumnTable(columnName);
            if (parent == null) return false;

            var child = parent.GetColumn(childColumnName);
            if (child != null) return true;

            return false;
        }

        public bool ColumnTableExists(string name)
        {
            if (Columns == null) return false;

            foreach (var column in Columns)
            {
                if (column.Name == name && column.UtfTable != null) return true;
                if(column.Name == name)
                {
                    if (column.Rows == null) return false;
                    if (column.Rows.Count > 0)
                    {
                        if (column.Rows[0].UtfTable != null) return true;
                    }
                }
            }

            return false;
        }


        public UTF_Column GetColumn(string name)
        {
            if (Columns == null) return null;

            foreach(var column in Columns)
            {
                if (column.Name == name) return column;
            }

            return null;
        }

        public UTF_File GetColumnTable(string name, bool raiseExceptionIfNotFound = false)
        {
            if (Columns == null) return null;

            foreach (var column in Columns)
            {
                if (column.Name == name)
                {
                    if(column.StorageFlag == StorageFlag.Constant)
                    {
                        return column.UtfTable;
                    }
                    else
                    {
                        if (column.NumOfRows != 0) return column.Rows[0].UtfTable;
                    }
                }
            }

            if (raiseExceptionIfNotFound)
                throw new Exception(string.Format("GetColumnTable: Could not column \"{0}\".", name));

            return null;
        }

        public AFS2_File GetColumnAfs2File(string name, bool raiseExceptionIfNotFound = false)
        {
            if (Columns == null) return null;

            foreach (var column in Columns)
            {
                if (column.Name == name)
                {
                    if(column.StorageFlag == StorageFlag.Constant)
                    {
                        return column.Afs2File;
                    }
                    else
                    {
                        if (column.NumOfRows != 0) return column.Rows[0].Afs2File;
                    }
                }
            }

            if (raiseExceptionIfNotFound)
                throw new Exception(string.Format("GetColumnAfs2File: Could not column \"{0}\".", name));

            return null;
        }


        public bool ColumnHasData(string name, int index)
        {
            if (Columns == null) return false;

            foreach (var column in Columns)
            {
                if (column.Name == name)
                {
                    if (column.NumOfRows - 1<= index)
                    {
                        if(column.Rows[index].UtfTable != null || column.Rows[index].Afs2File != null)
                        {
                            return true;
                        }
                        else if(column.Rows[index].Bytes != null)
                        {
                            return (column.Rows[index].Bytes.Length > 0) ? true : false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                }
            }

            return false;
        }
        
        public bool IsColumnPerRow(string name)
        {
            if (Columns == null) return false;

            foreach (var column in Columns)
            {
                if (column.Name == name)
                {
                    if (column.StorageFlag == StorageFlag.PerRow) return true;
                    return false;
                }
            }

            return false;
        }

        public bool IsColumnVariable(string name)
        {
            if (Columns == null) return false;

            foreach (var column in Columns)
            {
                if (column.Name == name)
                {
                    if (column.TypeFlag == TypeFlag.Int16 || column.TypeFlag == TypeFlag.UInt16 ||
                       column.TypeFlag == TypeFlag.Int32 || column.TypeFlag == TypeFlag.UInt32 ||
                        column.TypeFlag == TypeFlag.Int8 || column.TypeFlag == TypeFlag.UInt8 ||
                        column.TypeFlag == TypeFlag.Int64 || column.TypeFlag == TypeFlag.UInt64 ||
                        column.TypeFlag == TypeFlag.Single || column.TypeFlag == TypeFlag.Double)
                    {
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }

        public bool IsColumnVariable(string name, TypeFlag type)
        {
            if (Columns == null) return false;

            foreach (var column in Columns)
            {
                if (column.Name == name)
                {
                    if (column.TypeFlag == type) return true;
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a row on the specified column, optionally sets a value on it, then returns the row index.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="childColumnName">The child column name.</param>
        /// <param name="type">The expected variable type of the childColumn. If this doesn't match an exception will be raised.</param>
        /// <param name="value">Variable or string.</param>
        /// <returns></returns>
        public int CreateRow(string columnName, string childColumnName, TypeFlag type, int expectedRows, string value = null)
        {
            var column = GetColumnTable(columnName);
            if (column == null) throw new Exception(String.Format("Cannot create a row as the column named \"{0}\" does not exist.", columnName));

            var childColumn = column.GetColumn(childColumnName);
            if (childColumn == null) throw new Exception(String.Format("Cannot create a row as the column named \"{0}\" does not exist.", columnName));

            //Flag validation
            if (childColumn.StorageFlag != StorageFlag.PerRow) throw new Exception(String.Format("Column \"{0}\" is an unexpected storage type.", childColumnName));
            if (childColumn.TypeFlag != type) throw new Exception(String.Format("Column \"{0}\" is an unexpected value type.", childColumnName));
            
            //Row count validation
            if(childColumn.NumOfRows != expectedRows)
            {
                throw new Exception(String.Format("Unexpected amount of rows on {1} (Parent: {2}).\nExpected: {0}\nFound: {3}", expectedRows, childColumnName, columnName, childColumn.NumOfRows));
            }

            childColumn.CreateRow(value);
            return childColumn.LastRowIndex;
        }

        /// <summary>
        /// Creates a row on the specified column, optionally sets a value on it, then returns the row index.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="childColumnName">The child column name.</param>
        /// <param name="data">Byte array containg the data</param>
        /// <returns></returns>
        public int CreateRowData(string columnName, string childColumnName, int expectedRows, byte[] data = null)
        {
            var column = GetColumnTable(columnName);
            if (column == null) throw new Exception(String.Format("Cannot create a row as the column named \"{0}\" does not exist.", columnName));

            var childColumn = column.GetColumn(childColumnName);
            if (childColumn == null) throw new Exception(String.Format("Cannot create a row as the column named \"{0}\" does not exist.", columnName));

            //Flag validation
            if (childColumn.StorageFlag != StorageFlag.PerRow) throw new Exception(String.Format("Column \"{0}\" is an unexpected storage type.", childColumnName));
            if (childColumn.TypeFlag != TypeFlag.Data) throw new Exception(String.Format("Column \"{0}\" is an unexpected value type.", childColumnName));

            //Row count validation
            if (childColumn.NumOfRows != expectedRows)
            {
                throw new Exception(String.Format("Unexpected amount of rows on {1} (Parent: {2}).\nExpected: {0}\nFound: {3}", expectedRows, childColumnName, columnName, childColumn.NumOfRows));
            }

            childColumn.CreateRowData(data);
            return childColumn.LastRowIndex;
        }

        /// <summary>
        /// Creates a row on the specified column, optionally sets a value on it, then returns the row index.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="childColumnName">The child column name.</param>
        /// <param name="type">The expected variable type of the childColumn. If this doesn't match an exception will be raised.</param>
        /// <param name="value">Variable or string.</param>
        /// <returns></returns>
        public int AddValue(string columnName, string childColumnName, TypeFlag type, int expectedRows, string value = null)
        {
            var column = GetColumnTable(columnName);
            if (column == null) throw new Exception(String.Format("Cannot create a row as the column named \"{0}\" does not exist.", columnName));

            var childColumn = column.GetColumn(childColumnName);
            if (childColumn == null) throw new Exception(String.Format("Cannot create a row as the column named \"{0}\" does not exist.", childColumnName));

            //Flag validation
            if (childColumn.TypeFlag != type) throw new Exception(String.Format("Column \"{0}\" is an unexpected value type.", childColumnName));

            //Row count validation
            if (childColumn.NumOfRows != expectedRows && childColumn.StorageFlag == StorageFlag.PerRow && expectedRows != -1)
            {
                throw new Exception(String.Format("Unexpected amount of rows on {1} (Parent: {2}).\nExpected: {0}\nFound: {3}", expectedRows, childColumnName, columnName, childColumn.NumOfRows));
            }

            childColumn.AddValue(value, expectedRows);
            return childColumn.LastRowIndex;
        }

        /// <summary>
        /// Creates a row on the specified column, optionally sets data on it, then returns the row index.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="childColumnName">The child column name.</param>
        /// <param name="type">The expected variable type of the childColumn. If this doesn't match an exception will be raised.</param>
        /// <param name="data">Data</param>
        /// <returns></returns>
        public int AddData(string columnName, string childColumnName, int expectedRows, byte[] data = null)
        {
            var column = GetColumnTable(columnName);
            if (column == null) throw new Exception(String.Format("Cannot create a row as the column named \"{0}\" does not exist.", columnName));

            var childColumn = column.GetColumn(childColumnName);
            if (childColumn == null) throw new Exception(String.Format("Cannot create a row as the column named \"{0}\" does not exist.", childColumnName));

            //Flag validation
            if (childColumn.TypeFlag != TypeFlag.Data) throw new Exception(String.Format("Column \"{0}\" is an unexpected value type.", childColumnName));

            //Row count validation
            if (childColumn.NumOfRows != expectedRows && childColumn.StorageFlag == StorageFlag.PerRow && expectedRows != -1)
            {
                throw new Exception(String.Format("Unexpected amount of rows on {1} (Parent: {2}).\nExpected: {0}\nFound: {3}", expectedRows, childColumnName, columnName, childColumn.NumOfRows));
            }

            childColumn.AddData(data, expectedRows);
            return childColumn.LastRowIndex;
        }

        /// <summary>
        /// Creates a row on the specified column, optionally sets a value on it, then returns the row index.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="type">The expected variable type of the childColumn. If this doesn't match an exception will be raised.</param>
        /// <param name="value">Variable or string.</param>
        /// <returns></returns>
        public int AddValue(string columnName, TypeFlag type, int expectedRows, string value = null)
        {
            var column = GetColumn(columnName);
            if (column == null) throw new Exception(String.Format("Cannot create a row as the column named \"{0}\" does not exist.", columnName));
            
            //Flag validation
            if (column.TypeFlag != type) throw new Exception(String.Format("Column \"{0}\" is an unexpected value type.", columnName));

            //Row count validation
            if (column.NumOfRows != expectedRows && column.StorageFlag == StorageFlag.PerRow && expectedRows != -1)
            {
                throw new Exception(String.Format("Unexpected amount of rows on {1}.\nExpected: {0}\nFound: {2}", expectedRows, columnName,  column.NumOfRows));
            }

            column.AddValue(value, expectedRows);
            return column.LastRowIndex;
        }

        /// <summary>
        /// Creates a row on the specified column, optionally sets data on it, then returns the row index.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="type">The expected variable type of the childColumn. If this doesn't match an exception will be raised.</param>
        /// <param name="data">Data</param>
        /// <returns></returns>
        public int AddData(string columnName, int expectedRows, byte[] data = null)
        {
            var column = GetColumn(columnName);
            if (column == null) throw new Exception(String.Format("Cannot create a row as the column named \"{0}\" does not exist.", columnName));
            
            //Flag validation
            if (column.TypeFlag != TypeFlag.Data) throw new Exception(String.Format("Column \"{0}\" is an unexpected value type.", columnName));

            //Row count validation
            if (column.NumOfRows != expectedRows && column.StorageFlag == StorageFlag.PerRow && expectedRows != -1)
            {
                throw new Exception(String.Format("Unexpected amount of rows on {1} .\nExpected: {0}\nFound: {2}", expectedRows, columnName, column.NumOfRows));
            }

            column.AddData(data, expectedRows);
            return column.LastRowIndex;
        }



        //Set/Get
        /// <summary>
        /// Sets a value on the specified column, if the type matches. Supports both PerRow and Constants.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="value">The value to set. Can be any variable type, as long as it matches the type.</param>
        /// <param name="rowIdx">Row index. Ignored when column is of constant type.</param>
        public void SetValue<T>(string columnName, T value, TypeFlag type, int rowIdx)
        {
            var column = GetColumn(columnName);
            if (column == null) throw new Exception(String.Format("Cannot set the value as the column named \"{0}\" does not exist.", columnName));
            if (column.StorageFlag == StorageFlag.PerRow && column.NumOfRows <= rowIdx) throw new Exception(String.Format("Invalid number of rows on column \"{0}\" (Num: {1}, rowIdx: {2})", columnName, column.NumOfRows, rowIdx));

            if (!column.IsVariableType(type))
            {
                throw new Exception(String.Format("Column \"{2}\" is of type {1}, instead of the expected {0}. Cannot set the value.", type, column.TypeFlag, column.Name));
            }

            if (column.StorageFlag == StorageFlag.Constant)
            {
                column.Constant = value.ToString();
            }
            else if (column.StorageFlag == StorageFlag.PerRow)
            {
                column.Rows[rowIdx].Value = value.ToString();
            }
            else
            {
                throw new Exception(String.Format("Column \"{0}\" is not PerRow or Constant. Value cannot be set.", column));
            }
        }

        public T GetValue<T>(string columnName, TypeFlag type, int rowIdx)
        {
            var column = GetColumn(columnName);
            if (column == null) throw new Exception(String.Format("Cannot get the value as the column named \"{0}\" does not exist.", columnName));
            if (column.StorageFlag == StorageFlag.PerRow && column.NumOfRows <= rowIdx) throw new Exception(String.Format("Invalid number of rows on column \"{0}\" (Num: {1}, rowIdx: {2})", columnName, column.NumOfRows, rowIdx));

            if (!column.IsVariableType(type))
            {
                throw new Exception(String.Format("Column \"{2}\" is of type {1}, instead of the expected {0}. Cannot get the value.", type, column.TypeFlag, column.Name));
            }

            if(column.StorageFlag == StorageFlag.Constant)
            {
                return (T)Convert.ChangeType(column.Constant, typeof(T));
            }
            else if (column.StorageFlag == StorageFlag.PerRow)
            {
                return (T)Convert.ChangeType(column.Rows[rowIdx].Value, typeof(T));
            }
            else
            {
                throw new Exception(String.Format("Column \"{0}\" is not PerRow or Constant. Cannot get the value.", column));
            }
        }

        public void SetData(string columnName, byte[] data, int rowIdx)
        {
            var column = GetColumn(columnName);
            if (column == null) throw new Exception(String.Format("Cannot set the data as the column named \"{0}\" does not exist.", columnName));
            if (column.StorageFlag == StorageFlag.PerRow && column.NumOfRows <= rowIdx) throw new Exception(String.Format("Invalid number of rows on column \"{0}\" (Num: {1}, rowIdx: {2})", columnName, column.NumOfRows, rowIdx));
            
            if (column.TypeFlag != TypeFlag.Data)
            {
                throw new Exception(String.Format("Column \"{0}\" is not of type data. Cannot set the data.", columnName));
            }

            if (column.StorageFlag == StorageFlag.Constant)
            {
                column.Bytes = data;
            }
            else if (column.StorageFlag == StorageFlag.PerRow)
            {
                column.Rows[rowIdx].Bytes = data;
            }
            else
            {
                throw new Exception(String.Format("Column \"{0}\" is not PerRow or Constant. Data cannot be set.", column));
            }
        }

        public byte[] GetData(string columnName, int rowIdx)
        {
            var column = GetColumn(columnName);
            if (column == null) throw new Exception(String.Format("Cannot get the data as the column named \"{0}\" does not exist.", columnName));
            if (column.StorageFlag == StorageFlag.PerRow && column.NumOfRows <= rowIdx) throw new Exception(String.Format("Invalid number of rows on column \"{0}\" (Num: {1}, rowIdx: {2})", columnName, column.NumOfRows, rowIdx));

            if(column.TypeFlag != TypeFlag.Data)
            {
                throw new Exception(String.Format("Column \"{0}\" is not of type data. Cannot get the data.", columnName));
            }

            if (column.StorageFlag == StorageFlag.Constant)
            {
                return column.Bytes;
            }
            else if (column.StorageFlag == StorageFlag.PerRow)
            {
                return column.Rows[rowIdx].Bytes;
            }
            else
            {
                throw new Exception(String.Format("Column \"{0}\" is not PerRow or Constant. Cannot get the data.", column));
            }
        }

        public T GetValue<T>(string columnName, string childColumnName, TypeFlag type, int rowIdx)
        {
            var column = GetColumnTable(columnName);
            if (column == null) throw new Exception(String.Format("Cannot get the value as the column named \"{0}\" does not exist.", columnName));

            var childColumn = column.GetColumn(childColumnName);

            if (childColumn.StorageFlag == StorageFlag.PerRow && childColumn.NumOfRows <= rowIdx) throw new Exception(String.Format("Invalid number of rows on column \"{0}\" (Num: {1}, rowIdx: {2})", childColumnName, childColumn.NumOfRows, rowIdx));

            if (!childColumn.IsVariableType(type))
            {
                throw new Exception(String.Format("Column \"{2}\" is of type {1}, instead of the expected {0}. Cannot get the value.", type, childColumn.TypeFlag, childColumn.Name));
            }

            if (childColumn.StorageFlag == StorageFlag.Constant)
            {
                return (T)Convert.ChangeType(childColumn.Constant, typeof(T));
            }
            else if (childColumn.StorageFlag == StorageFlag.PerRow)
            {
                return (T)Convert.ChangeType(childColumn.Rows[rowIdx].Value, typeof(T));
            }
            else
            {
                throw new Exception(String.Format("Column \"{0}\" is not PerRow or Constant. Cannot get the value.", childColumnName));
            }
        }
        
        public byte[] GetData(string columnName, string childColumnName, int rowIdx)
        {
            var column = GetColumnTable(columnName);
            if (column == null) throw new Exception(String.Format("Cannot set the data as the column named \"{0}\" does not exist.", columnName));

            var childColumn = column.GetColumn(childColumnName);
            if (childColumn == null) throw new Exception(String.Format("Cannot set the data as the column named \"{0}\" does not exist.", childColumnName));

            if (childColumn.TypeFlag != TypeFlag.Data)
            {
                throw new Exception(String.Format("Column \"{0}\" is not of type data. Cannot get the data.", childColumnName));
            }

            if (childColumn.StorageFlag == StorageFlag.Constant)
            {
                return childColumn.Bytes;
            }
            else if (childColumn.StorageFlag == StorageFlag.PerRow)
            {
                return childColumn.Rows[rowIdx].Bytes;
            }
            else
            {
                throw new Exception(String.Format("Column \"{0}\" is not PerRow or Constant. Cannot get the data.", childColumnName));
            }
        }

        public StorageFlag GetStorageType(string columnName, string childColumnName)
        {
            var column = GetColumnTable(columnName);
            if (column == null) throw new Exception(String.Format("Cannot get the storage type as the column named \"{0}\" does not exist.", columnName));

            var childColumn = column.GetColumn(childColumnName);
            if (childColumn == null) throw new Exception(String.Format("Cannot get the storage type as the column named \"{0}\" does not exist.", childColumnName));

            return childColumn.StorageFlag;
        }
        
        public TypeFlag GetVariableType(string columnName, string childColumnName)
        {
            var column = GetColumnTable(columnName);
            if (column == null) throw new Exception(String.Format("Cannot get the variable type as the column named \"{0}\" does not exist.", columnName));

            var childColumn = column.GetColumn(childColumnName);
            if (childColumn == null) throw new Exception(String.Format("Cannot get the variable type as the column named \"{0}\" does not exist.", childColumnName));

            return childColumn.TypeFlag;
        }

        public int IndexOfRow(string columnName, string childColumnName, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return -1; //Cannot compare null values
            var column = GetColumnTable(columnName);
            if (column == null) throw new Exception(String.Format("The column named \"{0}\" does not exist.", columnName));

            var childColumn = column.GetColumn(childColumnName);
            if (childColumn == null) throw new Exception(String.Format("The column named \"{0}\" does not exist.", childColumnName));

            if (childColumn.StorageFlag != StorageFlag.PerRow)
            {
                throw new Exception(String.Format("Column \"{0}\" is not of StorageType PerRow.", childColumnName));
            }
            
            for(int i = 0; i < childColumn.NumOfRows; i++)
            {
                if (childColumn.Rows[i].Value == value) return childColumn.Rows[i].RowIndex;
            }

            return -1;
        }

        public int IndexOfRow(string columnName, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return -1; //Cannot compare null values

            var childColumn = GetColumn(columnName);
            if (childColumn == null) throw new Exception(String.Format("The column named \"{0}\" does not exist.", columnName));

            if (childColumn.StorageFlag != StorageFlag.PerRow)
            {
                throw new Exception(String.Format("Column \"{0}\" is not of StorageType PerRow.", columnName));
            }

            for (int i = 0; i < childColumn.NumOfRows; i++)
            {
                if (childColumn.Rows[i].Value == value) return childColumn.Rows[i].RowIndex;
            }

            return -1;
        }


        /// <summary>
        /// Sets a UTF_Table to the specified column at row index 0.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="table"></param>
        public void SetTable(string columnName, UTF_File table)
        {
            var column = GetColumn(columnName);

            if (column == null) throw new Exception(String.Format("Could not find the column \"{0}\".", columnName));
            if (column.Rows == null) column.Rows = new List<UTF_Row>() { new UTF_Row() };
            if (column.Rows.Count == 0) column.Rows.Add(new UTF_Row());

            column.Rows[0].UtfTable = table;
        }

        /// <summary>
        /// Sets a AFS2_File to the specified column at row index 0.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="afs2"></param>
        public void SetAfs2File(string columnName, AFS2_File afs2)
        {
            var column = GetColumn(columnName);

            if (column == null) throw new Exception(String.Format("Could not find the column \"{0}\".", columnName));
            if (column.Rows == null) column.Rows = new List<UTF_Row>() { new UTF_Row() };
            if (column.Rows.Count == 0) column.Rows.Add(new UTF_Row());

            column.Rows[0].Afs2File = afs2;
        }


    }

    [YAXSerializeAs("Column")]
    [Serializable]
    public class UTF_Column
    {
        public UTF_Column() { }

        public UTF_Column(string name, TypeFlag type)
        {
            Name = name;
            StorageFlag = StorageFlag.PerRow;
            TypeFlag = type;
            Rows = new List<UTF_Row>();
        }

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public StorageFlag StorageFlag { get; set; }
        [YAXAttributeForClass]
        public TypeFlag TypeFlag { get; set; }

        //Contained Values/Data
        [YAXDontSerializeIfNull]
        [YAXAttributeFor("Constant")]
        [YAXSerializeAs("Value")]
        public string Constant { get; set; }
        [YAXDontSerializeIfNull]
        public UTF_File UtfTable { get; set; }
        [YAXDontSerializeIfNull]
        public AFS2_File Afs2File { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [YAXAttributeFor("Constant")]
        public byte[] Bytes { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Row")]
        public List<UTF_Row> Rows { get; set; }

        [YAXDontSerialize]
        public int NumOfRows
        {
            get
            {
                if (Rows == null) return 0;
                return Rows.Count;
            }
        }

        [YAXDontSerialize]
        public int LastRowIndex
        {
            get
            {
                if (Rows == null) return -1;
                if (Rows.Count == 0) return -1;
                return Rows.Count - 1;
            }
        }
        

        public bool IsVariableType()
        {
            if (TypeFlag == TypeFlag.Int16 || TypeFlag == TypeFlag.UInt16 ||
                TypeFlag == TypeFlag.Int32 || TypeFlag == TypeFlag.UInt32 ||
                TypeFlag == TypeFlag.Int8 || TypeFlag == TypeFlag.UInt8 ||
                TypeFlag == TypeFlag.Int64 || TypeFlag == TypeFlag.UInt64 ||
                TypeFlag == TypeFlag.Single || TypeFlag == TypeFlag.Double)
            {
                return true;
            }
            return false;
        }

        public bool IsVariableType(TypeFlag type)
        {
            if (TypeFlag == type) return true;
            return false;
        }

        public UTF_File ChildUtfTable()
        {
            if(Rows != null)
            {
                if(NumOfRows == 1)
                {
                    if (Rows[0].UtfTable != null) return Rows[0].UtfTable;
                }
            }
            return null;
        }

        public int IndexOfValueInRow(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return -1;//cannot compare null values
            if (NumOfRows == 0) return -1;

            for(int i = 0; i < NumOfRows; i++)
            {
                if (Rows[i].Value == value) return i;
            }

            return -1;
        }

        /// <summary>
        /// Create a new row and assign the next RowIndex to it. Optionally, a value can also be assigned.
        /// </summary>
        /// <param name="value">Variable or string.</param>
        public void CreateRow(string value = null)
        {
            if(NumOfRows == 0)
            {
                throw new Exception(String.Format("Unexpected amount of rows on column \"{0}\".", Name));
            }
            int newIdx = NumOfRows;
            Rows.Add(new UTF_Row()
            {
                Value = value,
                RowIndex = newIdx
            });
        }

        /// <summary>
        /// Create a new row and assign the next RowIndex to it. Optionally, a value can also be assigned.
        /// </summary>
        /// <param name="data">Byte array containg the data.</param>
        public void CreateRowData(byte[] data = null)
        {
            if (NumOfRows == 0)
            {
                throw new Exception(String.Format("Unexpected amount of rows on column \"{0}\".", Name));
            }
            int newIdx = NumOfRows;
            Rows.Add(new UTF_Row()
            {
                Bytes = data,
                RowIndex = newIdx
            });
        }

        public void AddValue(string value, int rowCount)
        {
            //If type is constant, do the following:
            //value is equal to Constant: do nothing
            //Otherwise, we need to change StorageType to PerRow, Creates rows (with Constant as its value), and then add a new row fo the new value
            if(StorageFlag == StorageFlag.Constant)
            {
                if(value == Constant)
                {
                    return;
                }
                else
                {
                    StorageFlag = StorageFlag.PerRow;
                    Rows = new List<UTF_Row>();

                    //Add constant value on each row
                    for(int i = 0; i < rowCount; i++)
                    {
                        Rows.Add(new UTF_Row()
                        {
                            RowIndex = i,
                            Value = Constant
                        });
                    }

                    //Add new row for the new incoming value
                    Rows.Add(new UTF_Row()
                    {
                        RowIndex = rowCount,
                        Value = value
                    });

                    //Nullify constant
                    Constant = null;
                }
            }
            else if (StorageFlag == StorageFlag.PerRow)
            {
                Rows.Add(new UTF_Row()
                {
                    RowIndex = rowCount,
                    Value = value
                });
            }
        }

        public int AddData(byte[] data, int rowCount)
        {
            if (data != null) data.ToArray();

            //If type is constant, do the following:
            //value is equal to Constant: do nothing
            //Otherwise, we need to change StorageType to PerRow, Creates rows (with Constant as its value), and then add a new row fo the new value
            if (StorageFlag == StorageFlag.Constant)
            {
                if (Utils.CompareArray(data, Bytes))
                {
                    return -1;
                }
                else
                {
                    StorageFlag = StorageFlag.PerRow;
                    Rows = new List<UTF_Row>();

                    //Add constant value on each row
                    for (int i = 0; i < rowCount; i++)
                    {
                        Rows.Add(new UTF_Row()
                        {
                            RowIndex = i,
                            Bytes = (Bytes != null) ? Bytes.ToArray() : null
                        });
                    }

                    //Add new row for the new incoming value
                    Rows.Add(new UTF_Row()
                    {
                        RowIndex = rowCount,
                        Bytes = data
                    });

                    //Nullify constant
                    Bytes = null;
                }
            }
            else if (StorageFlag == StorageFlag.PerRow)
            {
                Rows.Add(new UTF_Row()
                {
                    RowIndex = rowCount,
                    Bytes = data
                });
            }
            return Rows.Count - 1;
        }
        

    }

    [YAXSerializeAs("Row")]
    [Serializable]
    public class UTF_Row
    {
        [YAXAttributeForClass]
        public int RowIndex { get; set; }
        [YAXDontSerializeIfNull]
        [YAXAttributeForClass]
        public string Value { get; set; }
        [YAXDontSerializeIfNull]
        public UTF_File UtfTable { get; set; }
        [YAXDontSerializeIfNull]
        public AFS2_File Afs2File { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [YAXAttributeFor("Data")]
        public byte[] Bytes { get; set; }
    }


    //Enums
    public enum _EncodingType
    {
        UTF8 = 1
    }
    
    public enum StorageFlag
    {
        Zero = 1,
        Constant = 3,
        PerRow = 5
        //All values are flipped, due to the Int4 conversion
    }

    public enum TypeFlag
    {
        UInt8 = 0x00,
        Int8 = 0x01,
        UInt16 = 0x02,
        Int16 = 0x03,
        UInt32 = 0x04,
        Int32 = 0x05,
        UInt64 = 0x06,
        Int64 = 0x07,
        Single = 0x08,
        Double = 0x09,
        String = 0x0a,
        Data = 0x0b,
        GUID = 0x0c,
        Type_UnkD = 0x0d,
        Type_UnkE = 0x0e,
        Type_UnkF = 0x0f
    }

    //
    [Serializable]
    public class DataInfo
    {
        public bool dataIsNull { get
            {
                if (Length > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        public int relativeOffset { get; set; }
        public int ColumnIndex { get; set; }
        public int RowIndex { get; set; }
        [YAXAttributeForClass]
        public int Offset { get; set; }
        [YAXAttributeForClass]
        public int Length { get; set; }
        [YAXAttributeForClass]
        public string Name { get; set; }
        public StorageFlag dataType { get; set; }
        public int originalLength { get; set; }
        [YAXAttributeForClass]
        public int RowOffset { get; set; }
        
        
        //Debug values
        public int DEBUG_COLUMN_OFFSET { get; set; }
        public int DEBUG_ROW_OFFSET { get; set; }
        public string DEBUG_MODE = String.Empty;

        /// <summary>
        /// Orders the given DataInfo collection according to the order the binary ACB files have data stored. (Constant > Row 0 > Row 1 > ...)
        /// </summary>
        public static List<DataInfo> OrderDataInfo(List<DataInfo> dataInfo, int rowCount)
        {
            List<DataInfo> orderedDataInfo = new List<DataInfo>();

            foreach(var info in dataInfo)
            {
                if(info.dataType == StorageFlag.Constant)
                {
                    orderedDataInfo.Add(info);
                }
            }

            for (int i = 0; i < rowCount; i++)
            {
                foreach (var info in dataInfo)
                {
                    if (info.dataType == StorageFlag.PerRow && info.RowIndex == i)
                    {
                        orderedDataInfo.Add(info);
                    }
                }
            }
            

            return orderedDataInfo;
        }

    }

    [Serializable]
    public class DataWriteObject
    {
        public byte[] bytes { get; set; }
        public int OffsetToReplace { get; set; }
        public StorageFlag storageFlag { get; set; }
        public int rowIndex { get; set; }
    }

    [Serializable]
    public class StringWriteObject
    {
        public string Str { get; set; }
        public int OffsetToReplace { get; set; }
        public int rowIndex { get; set; }
    }
}
