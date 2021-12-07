using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.BAC
{
    public class Parser
    {
        //Debug
        public List<string> UsedValues { get; private set; }

        string saveLocation { get; set; }
        public BAC_File bacFile { get; private set; } = new BAC_File();
        byte[] rawBytes { get; set; }
        List<byte> bytes { get; set; }

        //Offsets for every BacType present in the file, in order of appearence (for calc of type size)
        List<int> BacType_Offsets = new List<int>();

        public Parser(byte[] _rawBytes)
        {
            UsedValues = new List<string>();
            rawBytes = _rawBytes;
            bytes = rawBytes.ToList();
            Validation();
            InitOffsetList();
            ParseBac();
        }


        public Parser(string location, bool writeXml)
        {
            UsedValues = new List<string>();
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);
            bytes = rawBytes.ToList();
            Validation();
            InitOffsetList();
            ParseBac();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(BAC_File));
                serializer.SerializeToFile(bacFile, saveLocation + ".xml");
            }
        }

        public Parser(string location, List<string> debugList)
        {
            UsedValues = debugList;
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);
            bytes = rawBytes.ToList();
            Validation();
            InitOffsetList();
            ParseBac();
        }


        private void Validation()
        {
            if(BitConverter.ToInt32(rawBytes, 8) != 0 && BitConverter.ToInt32(rawBytes, 12) != 0 && BitConverter.ToInt32(rawBytes, 16) == 0)
            {
                throw new InvalidDataException("Xenoverse 1 BAC not supported.");
            }
        }

        public BAC_File GetBacFile()
        {
            return bacFile;
        }

        private void InitOffsetList()
        {
            int offset = BitConverter.ToInt32(rawBytes, 16);
            int count = BitConverter.ToInt32(rawBytes, 8);
            int totalEntryIndex = 0;

            //I need to seperate the "Flag" value, since it is atleast 2 multiple values (the last byte is a "Use this or CMN Bac" flag)
            //Instead of checking the Flag for this, simply check if any types exist on the BacEntry. Perfect compatibility (but still seperate the flags)
            for (int i = 0; i < count; i++)
            {
                int typeListOffset = BitConverter.ToInt32(rawBytes, offset + 8);
                int typeListCount = BitConverter.ToInt16(rawBytes, offset + 4);

                if (BitConverter.ToInt32(rawBytes, offset + 4) != 0)
                {
                    for (int a = 0; a < typeListCount; a++)
                    {
                        int thisType = BitConverter.ToInt16(rawBytes, typeListOffset + 0);
                        int thisTypeCount = BitConverter.ToInt16(rawBytes, typeListOffset + 2);
                        int thisTypeOffset = BitConverter.ToInt32(rawBytes, typeListOffset + 8);


                        BacType_Offsets.Add(thisTypeOffset);

                        typeListOffset += 16;
                    }
                }

                totalEntryIndex++;
                offset += 16;
            }
            BacType_Offsets.Add(rawBytes.Count());

        }

        private void ParseBac()
        {
            int offset = BitConverter.ToInt32(rawBytes, 16);
            int count = BitConverter.ToInt32(rawBytes, 8);

            bacFile.I_20 = BitConverter_Ex.ToInt32Array(rawBytes, 20, 3);
            bacFile.F_32 = BitConverter_Ex.ToFloat32Array(rawBytes, 32, 12);
            bacFile.I_80 = BitConverter_Ex.ToInt32Array(rawBytes, 80, 4);
            bacFile.BacEntries = AsyncObservableCollection<BAC_Entry>.Create();
            
            for (int i = 0; i < count; i++)
            {
                int typeListOffset = BitConverter.ToInt32(rawBytes, offset + 8);
                int typeListCount = BitConverter.ToInt16(rawBytes, offset + 4);

                bacFile.BacEntries.Add(new BAC_Entry()
                {
                    Flag = (BAC_Entry.Flags)BitConverter.ToUInt32(rawBytes, offset + 0),
                    Index = i.ToString()
                });

                for (int a = 0; a < typeListCount; a++)
                {
                    int thisType = BitConverter.ToInt16(rawBytes, typeListOffset + 0);
                    int thisTypeCount = BitConverter.ToInt16(rawBytes, typeListOffset + 2);
                    int thisTypeOffset = BitConverter.ToInt32(rawBytes, typeListOffset + 8);
                    
                    
                    if(thisTypeOffset != 0)
                    {
                        switch (thisType)
                        {
                            case 0:
                                bacFile.BacEntries[i].Type0 = BAC_Type0.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 1:
                                bacFile.BacEntries[i].Type1 = BAC_Type1.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 2:
                                bacFile.BacEntries[i].Type2 = BAC_Type2.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 3:
                                bacFile.BacEntries[i].Type3 = BAC_Type3.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 4:
                                bacFile.BacEntries[i].Type4 = BAC_Type4.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 5:
                                bacFile.BacEntries[i].Type5 = BAC_Type5.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 6:
                                bacFile.BacEntries[i].Type6 = BAC_Type6.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 7:
                                bacFile.BacEntries[i].Type7 = BAC_Type7.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 8:
                                bacFile.BacEntries[i].Type8 = BAC_Type8.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 9:
                                bacFile.BacEntries[i].Type9 = BAC_Type9.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 10:
                                bacFile.BacEntries[i].Type10 = BAC_Type10.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 11:
                                bacFile.BacEntries[i].Type11 = BAC_Type11.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 12:
                                bacFile.BacEntries[i].Type12 = BAC_Type12.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 13:
                                bacFile.BacEntries[i].Type13 = BAC_Type13.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 14:
                                bacFile.BacEntries[i].Type14 = BAC_Type14.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 15:
                                bacFile.BacEntries[i].Type15 = BAC_Type15.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 16:
                                bacFile.BacEntries[i].Type16 = BAC_Type16.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 17:
                                bacFile.BacEntries[i].Type17 = BAC_Type17.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount, IsType17Small(thisTypeOffset, thisTypeCount, i));
                                break;
                            case 18:
                                bacFile.BacEntries[i].Type18 = BAC_Type18.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 19:
                                bacFile.BacEntries[i].Type19 = BAC_Type19.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 20:
                                bacFile.BacEntries[i].Type20 = BAC_Type20.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 21:
                                bacFile.BacEntries[i].Type21 = BAC_Type21.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 22:
                                bacFile.BacEntries[i].Type22 = BAC_Type22.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 23:
                                bacFile.BacEntries[i].Type23 = BAC_Type23.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 24:
                                bacFile.BacEntries[i].Type24 = BAC_Type24.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 25:
                                bacFile.BacEntries[i].Type25 = BAC_Type25.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 26:
                                bacFile.BacEntries[i].Type26 = BAC_Type26.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 27:
                                bacFile.BacEntries[i].Type27 = BAC_Type27.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            default:
                                throw new InvalidDataException(String.Format("Parse failed. Unknown BacType encountered ({0})\nOffset: {1}\nCount: {2}.", thisType, thisTypeOffset, thisTypeCount));
                        }
                    } else
                    {
                        if(bacFile.BacEntries[i].TypeDummy == null)
                        {
                            bacFile.BacEntries[i].TypeDummy = new List<int>();
                        }
                        bacFile.BacEntries[i].TypeDummy.Add(thisType);
                    }
                    

                    typeListOffset += 16;
                }


                offset += 16;
            }


        }

        //Utility

        private bool IsType17Small(int offset, int count, int bacIndex)
        {
            int expectedSizeForFull17 = 32 * count;
            int expectedSizeForSmall17 = 20 * count;
            int nextEntryOffset = BacType_Offsets[BacType_Offsets.IndexOf(offset) + 1];
            int size = nextEntryOffset - offset;

            //Calc entry size
            if (nextEntryOffset - offset == expectedSizeForSmall17)
            {
                return true;
            }
            else if(nextEntryOffset - offset == expectedSizeForFull17)
            {
                return false;
            }
            else
            {
                //Unknown size. For debugging purposes only.
                throw new Exception(String.Format("Unknown BacType17 size! (Index = {0}, Size = {1}, Offset = {2})", bacIndex, size / count, offset));
            }


        }

    }
}
