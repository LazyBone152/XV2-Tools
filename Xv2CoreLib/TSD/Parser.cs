using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using System.Net;

namespace Xv2CoreLib.TSD
{
    public class Parser
    {
        //magic numbers
        const Int64 magicNumber = -8526495041129795056;

        //info
        string saveLocation;
        byte[] rawBytes;
        public TSD_File tsd_File { get; private set; }

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

            //tsd_File.CreateSysFlags("SysFlagsRaw.xml");
        }

        public Parser(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
            Parse();
        }

        public void Parse()
        {
            tsd_File = new TSD_File();
            tsd_File.Triggers = new List<TSD_Trigger>();
            tsd_File.Events = new List<TSD_Event>();
            tsd_File.Globals = new List<TSD_Global>();
            tsd_File.Constants = new List<TSD_Constant>();
            tsd_File.Zones = new List<TSD_Zone>();

            //Offsets and counts
            int currentOffset = 0;
            int currentSection = 0;

            for (int i = 0; i < 5; i++)
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
                        currentOffset = ParseSection3(currentOffset);
                        break;
                    case 4:
                        currentOffset = ParseSection4(currentOffset);
                        break;
                    case 5:
                        currentOffset = ParseSection5(currentOffset);
                        break;
                }
            }
        }
        

        void SaveXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(TSD_File));
            serializer.SerializeToFile(tsd_File, saveLocation + ".xml");
        }



        //Section Parsers

        private int ParseSection1(int currentOffset)
        {
            while (EndOfSectionCheck(currentOffset) == false)
            {
                tsd_File.Triggers.Add(new TSD_Trigger()
                {
                    Index = BitConverter.ToInt32(rawBytes, currentOffset + 0).ToString(),
                    I_04 = BitConverter.ToInt32(rawBytes, currentOffset + 4),
                    I_08 = BitConverter.ToInt32(rawBytes, currentOffset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, currentOffset + 12),
                    I_16 = BitConverter.ToInt32(rawBytes, currentOffset + 16),
                    I_20 = BitConverter.ToInt32(rawBytes, currentOffset + 20),
                    I_24 = BitConverter.ToInt32(rawBytes, currentOffset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, currentOffset + 28),
                    Condition = StringEx.GetString(rawBytes, currentOffset + 36, false, StringEx.EncodingType.UTF8, BitConverter.ToInt32(rawBytes, currentOffset + 32), false)
                });

                currentOffset += 36 + BitConverter.ToInt32(rawBytes, currentOffset + 32);
            }
            return currentOffset;
        }
        
        private int ParseSection2(int currentOffset)
        {
            while (EndOfSectionCheck(currentOffset) == false)
            {
                int _pos = tsd_File.Events.Count();
                int addedOffset = 0;

                tsd_File.Events.Add(new TSD_Event());

                tsd_File.Events[_pos].Index = BitConverter.ToInt32(rawBytes, currentOffset + 0).ToString();
                tsd_File.Events[_pos].I_04 = BitConverter.ToInt32(rawBytes, currentOffset + 4);
                addedOffset += 12;


                int stringSize = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4);

                tsd_File.Events[_pos].Str1 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.UTF8, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += stringSize + 4;

                stringSize = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4);
                tsd_File.Events[_pos].Str2 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.UTF8, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += stringSize + 4;

                stringSize = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4);
                tsd_File.Events[_pos].Str3 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.UTF8, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += stringSize + 4;

                stringSize = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4);
                tsd_File.Events[_pos].Str4 = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.UTF8, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                addedOffset += stringSize + 4;

                stringSize = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4);
                string args = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.UTF8, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);
                tsd_File.Events[_pos].Arguments = EventArguments.Read(args);
                addedOffset += stringSize + 4;

                //Read TNL ID array
                int count = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4);
                List<int> tnlIds = BitConverter_Ex.ToInt32Array(rawBytes, currentOffset + addedOffset, count).ToList();
                addedOffset += (4 * count);
                tsd_File.Events[_pos].TNL_IDs = ArrayConvert.ConvertToStringList(tnlIds);

                currentOffset += addedOffset;
            }
            return currentOffset;
        }

        private int ParseSection3(int currentOffset)
        {
            while (EndOfSectionCheck(currentOffset) == false)
            {
                int totalSize = 0;
                int _pos = tsd_File.Globals.Count();
                tsd_File.Globals.Add(ParseSec3Or4<TSD_Global>(currentOffset, out totalSize));
                currentOffset += totalSize;
            }
            return currentOffset;
        }

        private int ParseSection4(int currentOffset)
        {
            while (EndOfSectionCheck(currentOffset) == false)
            {
                int totalSize = 0;
                int _pos = tsd_File.Constants.Count();
                tsd_File.Constants.Add(ParseSec3Or4<TSD_Constant>(currentOffset, out totalSize));
                currentOffset += totalSize;
            }
            return currentOffset;
        }

        private int ParseSection5(int currentOffset)
        {
            while (EndOfSectionCheck(currentOffset) == false)
            {

                int _pos = tsd_File.Zones.Count();
                tsd_File.Zones.Add(new TSD_Zone());
                tsd_File.Zones[_pos].Index = BitConverter.ToInt32(rawBytes, currentOffset).ToString();
                tsd_File.Zones[_pos].I_04 = BitConverter.ToInt32(rawBytes, currentOffset + 4);
                tsd_File.Zones[_pos].Str = StringEx.GetString(rawBytes, currentOffset + 12, false, StringEx.EncodingType.UTF8, BitConverter.ToInt32(rawBytes, currentOffset + 8), false);
                currentOffset += 12 + tsd_File.Zones[_pos].Str.Length;
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
            else if (BitConverter.ToInt64(rawBytes, offset) == magicNumber)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        private T ParseSec3Or4<T>(int currentOffset, out int totalSize) where T : ISec_3_4, new()
        {
            totalSize = 0;
            int addedOffset = 4;
            T _section = new T();

            _section.Index = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.UTF8, BitConverter.ToInt32(rawBytes, currentOffset), false);

            addedOffset += _section.Index.Length;

            _section.Type = BitConverter.ToInt32(rawBytes, currentOffset + addedOffset);

            addedOffset += 8; //Unk_I and Flag Size

            _section.Str = StringEx.GetString(rawBytes, currentOffset + addedOffset, false, StringEx.EncodingType.UTF8, BitConverter.ToInt32(rawBytes, currentOffset + addedOffset - 4), false);

            addedOffset += _section.Str.Length;
            
            totalSize += addedOffset;

            return _section;
        }
        

    }
}
