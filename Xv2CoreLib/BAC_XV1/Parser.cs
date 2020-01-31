using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BAC_XV1
{
    public class Parser
    {
        //Debug
        public List<string> UsedValues { get; private set; }

        public BAC_File bacFile { get; private set; }
        private List<byte> bytes { get; set; }
        private byte[] rawBytes { get; set; }

        //Offsets for every BacType present in the file, in order of appearence (for calc of type size)
        List<int> BacType_Offsets = new List<int>();

        public Parser(string filePath, bool writeXml)
        {
            UsedValues = new List<string>();
            bacFile = new BAC_File();
            rawBytes = File.ReadAllBytes(filePath);
            bytes = rawBytes.ToList();
            ValidateFile();
            InitOffsetList();
            ParseBac();
            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(BAC_File));
                serializer.SerializeToFile(bacFile, filePath + ".xml");
            }
        }
        
        public BAC_File GetBacFile()
        {
            return bacFile;
        }

        private void ValidateFile()
        {
            if(BitConverter.ToInt32(rawBytes, 12) == 0 && BitConverter.ToInt32(rawBytes, 16) != 0)
            {
                throw new Exception("BAC file was not in Xenoverse 1 format.");
            }
        }

        private void InitOffsetList()
        {
            int offset = BitConverter.ToInt32(rawBytes, 12);
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
                        int thisTypeOffset = BitConverter.ToInt32(rawBytes, typeListOffset + 4);


                        BacType_Offsets.Add(thisTypeOffset);

                        typeListOffset += 8;
                    }
                }

                totalEntryIndex++;
                offset += 16;
            }
            BacType_Offsets.Add(rawBytes.Count());

        }

        private void ParseBac()
        {
            int offset = BitConverter.ToInt32(rawBytes, 12);
            int count = BitConverter.ToInt32(rawBytes, 8);

            bacFile.I_20 = BitConverter_Ex.ToInt32Array(rawBytes, 20, 3);
            bacFile.F_32 = BitConverter_Ex.ToFloat32Array(rawBytes, 32, 12);
            bacFile.I_80 = BitConverter_Ex.ToInt32Array(rawBytes, 80, 4);
            bacFile.BacEntries = new List<BAC_Entry>();

            for (int i = 0; i < count; i++)
            {
                int typeListOffset = BitConverter.ToInt32(rawBytes, offset + 8);
                int typeListCount = BitConverter.ToInt16(rawBytes, offset + 4);

                bacFile.BacEntries.Add(new BAC_Entry()
                {
                    Flag = (BAC.BAC_Entry.Flags)BitConverter.ToUInt32(rawBytes, offset + 0),
                    Index = i.ToString()
                });

                for (int a = 0; a < typeListCount; a++)
                {
                    int thisType = BitConverter.ToInt16(rawBytes, typeListOffset + 0);
                    int thisTypeCount = BitConverter.ToInt16(rawBytes, typeListOffset + 2);
                    int thisTypeOffset = BitConverter.ToInt32(rawBytes, typeListOffset + 4);


                    if (thisTypeOffset != 0)
                    {
                        switch (thisType)
                        {
                            case 0:
                                bacFile.BacEntries[i].Type0 = BAC.BAC_Type0.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 1:
                                bacFile.BacEntries[i].Type1 = BAC.BAC_Type1.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 2:
                                bacFile.BacEntries[i].Type2 = BAC.BAC_Type2.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 3:
                                bacFile.BacEntries[i].Type3 = BAC.BAC_Type3.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 4:
                                bacFile.BacEntries[i].Type4 = BAC.BAC_Type4.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 5:
                                bacFile.BacEntries[i].Type5 = BAC.BAC_Type5.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 6:
                                bacFile.BacEntries[i].Type6 = BAC.BAC_Type6.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 7:
                                bacFile.BacEntries[i].Type7 = BAC.BAC_Type7.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 8:
                                bacFile.BacEntries[i].Type8 = BAC.BAC_Type8.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 9:
                                bacFile.BacEntries[i].Type9 = BAC.BAC_Type9.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 10:
                                bacFile.BacEntries[i].Type10 = BAC_Type10.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 11:
                                bacFile.BacEntries[i].Type11 = BAC.BAC_Type11.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 12:
                                bacFile.BacEntries[i].Type12 = BAC.BAC_Type12.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 13:
                                bacFile.BacEntries[i].Type13 = BAC.BAC_Type13.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 14:
                                bacFile.BacEntries[i].Type14 = BAC.BAC_Type14.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 15:
                                bacFile.BacEntries[i].Type15 = BAC_Type15.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 16:
                                bacFile.BacEntries[i].Type16 = BAC.BAC_Type16.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 17:
                                bacFile.BacEntries[i].Type17 = BAC.BAC_Type17.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount, IsType17Small(thisTypeOffset, thisTypeCount, i));
                                break;
                            case 18:
                                bacFile.BacEntries[i].Type18 = BAC.BAC_Type18.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 19:
                                bacFile.BacEntries[i].Type19 = BAC.BAC_Type19.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 20:
                                bacFile.BacEntries[i].Type20 = BAC.BAC_Type20.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 21:
                                bacFile.BacEntries[i].Type21 = BAC.BAC_Type21.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 22:
                                bacFile.BacEntries[i].Type22 = BAC.BAC_Type22.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 23:
                                bacFile.BacEntries[i].Type23 = BAC.BAC_Type23.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            case 24:
                                bacFile.BacEntries[i].Type24 = BAC.BAC_Type24.Read(rawBytes, bytes, thisTypeOffset, thisTypeCount);
                                break;
                            default:
                                Console.WriteLine(String.Format("Parse failed. Unknown BacType encountered ({0})\nOffset: {1}\nCount: {2}.", thisType, thisTypeOffset, thisTypeCount));
                                Utils.WaitForInputThenQuit();
                                break;
                        }
                    }
                    else
                    {
                        if (bacFile.BacEntries[i].TypeDummy == null)
                        {
                            bacFile.BacEntries[i].TypeDummy = new List<int>();
                        }
                        bacFile.BacEntries[i].TypeDummy.Add(thisType);
                    }


                    typeListOffset += 8;
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
            else if (nextEntryOffset - offset == expectedSizeForFull17)
            {
                return false;
            }
            else
            {
                //Unknown size. For debugging purposes only.
                Console.WriteLine(offset);
                Console.WriteLine(String.Format("Unknown BacType17 size! (Index = {0}, Size = {1})", bacIndex, size / count));
                Utils.WaitForInputThenQuit();
                return false;
            }


        }



    }
}
