using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.QBT
{
    public class Parser
    {
        private string saveLocation;
        private byte[] rawBytes;
        public QBT_File QbtFile { get; private set; }

        public Parser(string location, bool _writeXml)
        {
            rawBytes = File.ReadAllBytes(location);
            saveLocation = location;
            ParseFile();

            if (_writeXml == true)
            {
                WriteXmlFile();
            }
        }

        public Parser(byte[] bytes)
        {
            rawBytes = bytes;
            ParseFile();
        }

        private void ParseFile()
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

            QbtFile = new QBT_File()
            {
                NormalDialogues = ReadTableSection(type0Offset, type0Count, quoteOffset, stringOffset),
                InteractiveDialogues = ReadTableSection(type1Offset, type1Count, quoteOffset, stringOffset),
                SpecialDialogues = ReadTableSection(type2Offset, type2Count, quoteOffset, stringOffset),
            };
        }

        private List<DialogueEntry> ReadTableSection(int offset, int count, int quoteSectionOffset, int stringSectionOffset)
        {
            List<DialogueEntry> tableList = new List<DialogueEntry>();

            if (count > 0)
            {
                int[] order = new int[30] { 0, 2, 6, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56, 58, 60, 62 };

                for (int i = 0; i < count; i++)
                {
                    int quoteIndex = BitConverter.ToInt16(rawBytes, offset + 4);
                    int quoteCount = BitConverter.ToInt16(rawBytes, offset + 8);

                    tableList.Add(new DialogueEntry()
                    {
                        I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                        InteractionType = (QbtInteractionType)BitConverter.ToInt32(rawBytes, offset + 12),
                        InteractionParam = BitConverter.ToInt32(rawBytes, offset + 16),
                        SpecialEvent = (QbtEvent)BitConverter.ToInt32(rawBytes, offset + 20),
                        SpecialOnEventEnd = BitConverter.ToInt32(rawBytes, offset + 24),
                        I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                        CharaID = BitConverter.ToUInt16(rawBytes, offset + 32),
                        I_34 = BitConverter.ToUInt16(rawBytes, offset + 34),
                        I_36 = BitConverter_Ex.ToInt32Array(rawBytes, offset + 36, 7),
                        DialogueEntries = ReadQuoteEntries(quoteIndex, quoteCount, quoteSectionOffset, stringSectionOffset),
                        ID = BitConverter.ToUInt16(rawBytes, quoteIndex * 20 + quoteSectionOffset)
                    });
                    offset += 64;
                }
                return tableList;
            }
            else
            {
                return tableList;
            }
        }

        private List<QuoteData> ReadQuoteEntries(int index, int count, int quoteSectionOffset, int stringSectionOffset)
        {
            List<QuoteData> quoteList = new List<QuoteData>();
            int offset = index * 20 + quoteSectionOffset;

            for (int i = 0; i < count; i++)
            {
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
                    MSG_Name = StringEx.GetString(rawBytes, 32 * stringIndex + stringSectionOffset, maxSize: 32)
                });

                offset += 20;
            }
            return quoteList;
        }

        private void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(QBT_File));
            serializer.SerializeToFile(QbtFile, saveLocation + ".xml");
        }
    }
}
