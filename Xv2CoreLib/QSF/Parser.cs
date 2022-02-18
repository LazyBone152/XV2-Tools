using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.QSF
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        QSF_File qsf_File = new QSF_File();

        //State
        bool writeXml = false;

        public Parser(string location, bool _writeXml)
        {
            writeXml = _writeXml;
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

        void Parse() 
        {
            int tableCount = BitConverter.ToInt32(rawBytes, 8);

            qsf_File.I_12 = BitConverter.ToInt32(rawBytes, 12);
            qsf_File.Tables = new List<TableSection>();

            int offset = 16;
            for (int i = 0; i < tableCount; i++) 
            {
                qsf_File.Tables.Add(new TableSection());
                qsf_File.Tables[i].Type = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset) + offset);
                qsf_File.Tables[i].I_12 = BitConverter.ToInt32(rawBytes, offset + 12);
                int DataCount = BitConverter.ToInt32(rawBytes, offset + 4);
                int DataOffset = BitConverter.ToInt32(rawBytes, offset + 8) + offset + 8;

                if (DataCount > 0) 
                {
                    qsf_File.Tables[i].TableEntry = new List<DataSection>();
                    for (int a = 0; a < DataCount; a++) 
                    {
                        qsf_File.Tables[i].TableEntry.Add(new DataSection());
                        int EntryCount = BitConverter.ToInt32(rawBytes, DataOffset + 0);
                        int EntryOffset = BitConverter.ToInt32(rawBytes, DataOffset + 4) + DataOffset + 4;

                        if (EntryCount > 0)
                        {
                            qsf_File.Tables[i].TableEntry[a].TableSubEntry = new List<QuestEntry>();

                            for (int e = 0; e < EntryCount; e++)
                            {
                                qsf_File.Tables[i].TableEntry[a].TableSubEntry.Add(new QuestEntry());
                                qsf_File.Tables[i].TableEntry[a].TableSubEntry[e].Alias_ID = e + 1;
                                qsf_File.Tables[i].TableEntry[a].TableSubEntry[e].QuestID = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, EntryOffset) + EntryOffset);
                                Console.WriteLine(qsf_File.Tables[i].TableEntry[a].TableSubEntry[e].QuestID);
                                EntryOffset += 4;
                            }
                        }

                        DataOffset += 8;
                    }
                }

                offset += 16;

            }

        }

        void SaveXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(QSF_File));
            serializer.SerializeToFile(qsf_File, saveLocation + ".xml");
        }

    }
}
