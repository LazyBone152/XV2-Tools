using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.QBT
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        QBT_File qbt_File;

        //State
        bool isFinished = false;
        bool writeXml = false;

        public Parser(string location, bool _writeXml)
        {
            writeXml = _writeXml;
            rawBytes = File.ReadAllBytes(location);
            saveLocation = location;
            ParseFile();
            isFinished = true;
            if (writeXml == true) {
                WriteXmlFile();
            }
        }

        public QBT_File GetQbtFile()
        {
            while (isFinished == false) { }
            return qbt_File;
        }


        void ParseFile()
        {
            int tableEntries = BitConverter.ToInt16(rawBytes, 12);
            int quoteEntries = BitConverter.ToInt16(rawBytes, 14);
            int quoteOffset = tableEntries * 64 + 36;
            int stringOffset = quoteEntries * 20 + quoteOffset;
            int type0Count = BitConverter.ToInt16(rawBytes, 16);
            int type1Count = BitConverter.ToInt16(rawBytes, 18);
            int type2Count = BitConverter.ToInt16(rawBytes, 20);
            int type0Offset = BitConverter.ToInt16(rawBytes, 24);
            int type1Offset = BitConverter.ToInt16(rawBytes, 28);
            int type2Offset = BitConverter.ToInt16(rawBytes, 32);


            bool I_04_Check = false;
            if (BitConverter.ToInt16(rawBytes, 4) == -2)
            {
                I_04_Check = true;
            }
            else
            {
                I_04_Check = false;
            }


            qbt_File = new QBT_File()
            {
                I_04 = I_04_Check,
                Type0 = ReadTableSection(type0Offset, type0Count, quoteOffset, stringOffset),
                Type1 = ReadTableSection(type1Offset, type1Count, quoteOffset, stringOffset),
                Type2 = ReadTableSection(type2Offset, type2Count, quoteOffset, stringOffset),
            };
            
        }

        private List<TableEntry> ReadTableSection(int offset, int count, int quoteSectionOffset, int stringSectionOffset) {

            if (count > 0)
            {
                List<TableEntry> tableList = new List<TableEntry>();
                int[] order = new int[30] { 0, 2, 6, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56, 58, 60, 62 };

                for (int i = 0; i < count; i++)
                {
                    int quoteCount = BitConverter.ToInt16(rawBytes, offset + 8);
                    int quoteIndex = BitConverter.ToInt16(rawBytes, offset + 4);

                    tableList.Add(new TableEntry()
                    {
                        I_00 = new short[30] {
                        BitConverter.ToInt16(rawBytes, offset + 0),
                        BitConverter.ToInt16(rawBytes, offset + 2),
                        BitConverter.ToInt16(rawBytes, offset + 6),
                        BitConverter.ToInt16(rawBytes, offset + 10),
                        BitConverter.ToInt16(rawBytes, offset + 12),
                        BitConverter.ToInt16(rawBytes, offset + 14),
                        BitConverter.ToInt16(rawBytes, offset + 16),
                        BitConverter.ToInt16(rawBytes, offset + 18),
                        BitConverter.ToInt16(rawBytes, offset + 20),
                        BitConverter.ToInt16(rawBytes, offset + 22),
                        BitConverter.ToInt16(rawBytes, offset + 24),
                        BitConverter.ToInt16(rawBytes, offset + 26),
                        BitConverter.ToInt16(rawBytes, offset + 28),
                        BitConverter.ToInt16(rawBytes, offset + 30),
                        BitConverter.ToInt16(rawBytes, offset + 32),
                        BitConverter.ToInt16(rawBytes, offset + 34),
                        BitConverter.ToInt16(rawBytes, offset + 36),
                        BitConverter.ToInt16(rawBytes, offset + 38),
                        BitConverter.ToInt16(rawBytes, offset + 40),
                        BitConverter.ToInt16(rawBytes, offset + 42),
                        BitConverter.ToInt16(rawBytes, offset + 44),
                        BitConverter.ToInt16(rawBytes, offset + 46),
                        BitConverter.ToInt16(rawBytes, offset + 48),
                        BitConverter.ToInt16(rawBytes, offset + 50),
                        BitConverter.ToInt16(rawBytes, offset + 52),
                        BitConverter.ToInt16(rawBytes, offset + 54),
                        BitConverter.ToInt16(rawBytes, offset + 56),
                        BitConverter.ToInt16(rawBytes, offset + 58),
                        BitConverter.ToInt16(rawBytes, offset + 60),
                        BitConverter.ToInt16(rawBytes, offset + 62),
                    },
                        DialogueEntries = ReadQuoteEntries(quoteIndex, quoteCount, quoteSectionOffset, stringSectionOffset),
                        QBT_ID = BitConverter.ToInt16(rawBytes, quoteIndex * 20 + quoteSectionOffset)
                    });
                    offset += 64;
                }
                return tableList;
            }
            else
            {
                return null;
            }

            
        }

        private List<QuoteData> ReadQuoteEntries(int index, int count, int quoteSectionOffset, int stringSectionOffset) {
            List<QuoteData> quoteList = new List<QuoteData>();
            int offset = index * 20 + quoteSectionOffset;

            for (int i = 0; i < count; i++) {
                int stringIndex = BitConverter.ToInt16(rawBytes, offset + 18);
                quoteList.Add(new QuoteData()
                {
                    I_02 = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    I_06 = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToInt16(rawBytes, offset + 16),
                    Str_18 = StringEx.GetString(rawBytes, 32 * stringIndex + stringSectionOffset, maxSize:32)
                });

                offset += 20;
            }
            return quoteList;
        }

        void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(QBT_File));
            serializer.SerializeToFile(qbt_File, saveLocation + ".xml");

        }
    }
}
