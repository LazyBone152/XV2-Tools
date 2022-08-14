using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.QSF
{
    public class Parser
    {
        private string saveLocation;
        private byte[] rawBytes;
        private QSF_File qsf_File = new QSF_File();

        public Parser(string location, bool writeXml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            Parse();

            if (writeXml == true)
            {
                SaveXmlFile();
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            Parse();
        }

        public QSF_File GetQsfFile()
        {
            return qsf_File;
        }

        private void Parse() 
        {
            int tableCount = BitConverter.ToInt32(rawBytes, 8);

            qsf_File.I_12 = BitConverter.ToInt32(rawBytes, 12);
            qsf_File.QuestTypes = new List<QSF_QuestType>();

            int offset = 16;

            for (int i = 0; i < tableCount; i++) 
            {
                qsf_File.QuestTypes.Add(new QSF_QuestType());
                qsf_File.QuestTypes[i].Type = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset) + offset);
                qsf_File.QuestTypes[i].I_12 = BitConverter.ToInt32(rawBytes, offset + 12);
                int DataCount = BitConverter.ToInt32(rawBytes, offset + 4);
                int DataOffset = BitConverter.ToInt32(rawBytes, offset + 8) + offset + 8;

                if (DataCount > 0) 
                {
                    qsf_File.QuestTypes[i].QuestGroups = new List<QSF_QuestGroup>();
                    for (int a = 0; a < DataCount; a++) 
                    {
                        qsf_File.QuestTypes[i].QuestGroups.Add(new QSF_QuestGroup());
                        qsf_File.QuestTypes[i].QuestGroups[a].Index = a;
                        int EntryCount = BitConverter.ToInt32(rawBytes, DataOffset + 0);
                        int EntryOffset = BitConverter.ToInt32(rawBytes, DataOffset + 4) + DataOffset + 4;

                        if (EntryCount > 0)
                        {
                            qsf_File.QuestTypes[i].QuestGroups[a].QuestEntries = new List<QuestEntry>();

                            for (int e = 0; e < EntryCount; e++)
                            {
                                qsf_File.QuestTypes[i].QuestGroups[a].QuestEntries.Add(new QuestEntry());
                                qsf_File.QuestTypes[i].QuestGroups[a].QuestEntries[e].Alias_ID = e + 1;
                                qsf_File.QuestTypes[i].QuestGroups[a].QuestEntries[e].QuestID = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, EntryOffset) + EntryOffset);
                                EntryOffset += 4;
                            }
                        }

                        DataOffset += 8;
                    }
                }

                offset += 16;
            }
        }

        private void SaveXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(QSF_File));
            serializer.SerializeToFile(qsf_File, saveLocation + ".xml");
        }

    }
}
