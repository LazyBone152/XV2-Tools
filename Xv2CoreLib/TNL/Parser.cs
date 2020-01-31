using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.TNL
{
    public class Parser
    {
        //magic numbers
        const Int64 magicNumber = -8526495041129795056;

        //info
        string saveLocation;
        byte[] rawBytes;
        public TNL_File tnl_File { get; private set; }

        //State
        bool writeXml = false;


        public Parser(string location, bool _writeWml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            writeXml = _writeWml;
            Parse();
            if (writeXml == true)
            {
                SaveXmlFile();
            }
        }

        public Parser(byte[] bytes)
        {
            rawBytes = bytes;
            Parse();
        }

        public void Parse()
        {
            tnl_File = new TNL_File();
            tnl_File.Characters = new List<TNL_Character>();
            tnl_File.Teachers = new List<TNL_Teacher>();
            tnl_File.Objects = new List<TNL_Object>();
            tnl_File.Actions = new List<TNL_Action>();

            //Offsets and counts
            int currentOffset = 0;
            int currentSection = 0;

            for (int i = 0; i < 4; i++)
            {
                if (currentOffset > 0)
                {
                    currentOffset += 8;
                }
                currentSection = rawBytes[currentOffset];
                currentOffset++;

                switch (currentSection)
                {
                    case 1:
                        currentOffset = ParseSection1(currentOffset);
                        break;
                    case 2:
                        currentOffset = ParseSection2(currentOffset);
                        break;
                    case 3:
                        currentOffset = ParseSection4(currentOffset);
                        break;
                    case 4:
                        currentOffset = ParseSection3(currentOffset);
                        break;
                }
            }

        }

        private void SaveXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(TNL_File));
            serializer.SerializeToFile(tnl_File, saveLocation + ".xml");
        }


        //Section Parsers


        private int ParseSection1(int currentOffset)
        {
            while (EndOfSectionCheck(currentOffset) == false) {
                int _pos = tnl_File.Characters.Count();
                int addedOffset = 0;
                tnl_File.Characters.Add(new TNL_Character());


                tnl_File.Characters[_pos].I32_1 = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset).ToString();
                addedOffset += 4;
                tnl_File.Characters[_pos].I8_1 = rawBytes[currentOffset + addedOffset];
                tnl_File.Characters[_pos].I8_2 = rawBytes[currentOffset + addedOffset + 1];
                tnl_File.Characters[_pos].I8_3 = rawBytes[currentOffset + addedOffset + 2];
                addedOffset += 7;
                tnl_File.Characters[_pos].Str1 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Characters[_pos].Str1.Length;
                tnl_File.Characters[_pos].I16_1 = BitConverter.ToInt16(rawBytes, currentOffset + addedOffset);
                tnl_File.Characters[_pos].I16_2 = BitConverter.ToInt16(rawBytes, currentOffset + addedOffset + 2);
                addedOffset += 8;
                tnl_File.Characters[_pos].Str2 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Characters[_pos].Str2.Length;
                tnl_File.Characters[_pos].I32_2 = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset).ToString();
                addedOffset += 8;
                tnl_File.Characters[_pos].Str3 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Characters[_pos].Str3.Length + 4;
                tnl_File.Characters[_pos].Str4 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Characters[_pos].Str4.Length;
                tnl_File.Characters[_pos].I32_3 = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset).ToString();
                currentOffset += addedOffset + 4;
            }
            return currentOffset;
        }

        private int ParseSection2(int currentOffset)
        {
            while (EndOfSectionCheck(currentOffset) == false)
            {
                int _pos = tnl_File.Teachers.Count();
                int addedOffset = 0;
                tnl_File.Teachers.Add(new TNL_Teacher());


                tnl_File.Teachers[_pos].I32_1 = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset).ToString();
                addedOffset += 4;
                tnl_File.Teachers[_pos].I8_1 = rawBytes[currentOffset + addedOffset];
                tnl_File.Teachers[_pos].I8_2 = rawBytes[currentOffset + addedOffset + 1];
                tnl_File.Teachers[_pos].I8_3 = rawBytes[currentOffset + addedOffset + 2];
                addedOffset += 7;
                tnl_File.Teachers[_pos].Str1 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Teachers[_pos].Str1.Length;
                tnl_File.Teachers[_pos].I16_1 = BitConverter.ToInt16(rawBytes, currentOffset + addedOffset);
                tnl_File.Teachers[_pos].I16_2 = BitConverter.ToInt16(rawBytes, currentOffset + addedOffset + 2);
                addedOffset += 8;
                tnl_File.Teachers[_pos].Str2 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Teachers[_pos].Str2.Length;
                tnl_File.Teachers[_pos].I32_2 = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset).ToString();
                addedOffset += 8;
                tnl_File.Teachers[_pos].Str3 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Teachers[_pos].Str3.Length + 4;
                tnl_File.Teachers[_pos].Str4 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Teachers[_pos].Str4.Length;
                tnl_File.Teachers[_pos].I32_3 = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset).ToString();
                currentOffset += addedOffset + 4;
            }
            return currentOffset;
        }

        private int ParseSection3(int currentOffset)
        {
            while (EndOfSectionCheck(currentOffset) == false)
            {
                int _pos = tnl_File.Objects.Count();
                int addedOffset = 0;
                tnl_File.Objects.Add(new TNL_Object());

                tnl_File.Objects[_pos].Index = BitConverter.ToInt32(rawBytes, currentOffset).ToString();
                addedOffset += 8;
                tnl_File.Objects[_pos].Str1 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Objects[_pos].Str1.Length;

                tnl_File.Objects[_pos].I32_2 = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset).ToString();
                addedOffset += 8;

                tnl_File.Objects[_pos].Str2 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Objects[_pos].Str2.Length + 4;

                tnl_File.Objects[_pos].Str3 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Objects[_pos].Str3.Length + 4;

                tnl_File.Objects[_pos].Str4 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Objects[_pos].Str4.Length;

                tnl_File.Objects[_pos].I32_3 = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset).ToString();
                tnl_File.Objects[_pos].I32_4 = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset + 4).ToString();
                tnl_File.Objects[_pos].I32_5 = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset + 8).ToString();
                currentOffset += addedOffset + 12;
            }
            return currentOffset;
        }

        private int ParseSection4(int currentOffset)
        {
            while (EndOfSectionCheck(currentOffset) == false)
            {
                int _pos = tnl_File.Actions.Count();
                int addedOffset = 0;
                tnl_File.Actions.Add(new TNL_Action());

                tnl_File.Actions[_pos].Index = BitConverter.ToInt32(rawBytes, currentOffset).ToString();
                addedOffset += 8;

                tnl_File.Actions[_pos].Str1 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Actions[_pos].Str1.Length + 4;
                tnl_File.Actions[_pos].Str2 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Actions[_pos].Str2.Length + 4;
                tnl_File.Actions[_pos].Str3 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += tnl_File.Actions[_pos].Str3.Length + 4;

                string args = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.ASCII, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                tnl_File.Actions[_pos].Arguments = EventArguments.Read(args);
                addedOffset += args.Length;
                currentOffset += addedOffset;

            }
            return currentOffset;
        }

        //Utility
        /// <summary>
        /// Checks if the magic number is present at the specified offset or if the currentOffset is equal to the size of the byte array..
        /// </summary>
        private bool EndOfSectionCheck(int offset)
        {
            if (rawBytes.Count() == offset)
            {
                return true;
            }
            else
            if (BitConverter.ToInt64(rawBytes, offset) == magicNumber)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        

    }
}
