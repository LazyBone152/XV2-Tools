using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.QSL
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        QSL_File qsl_File = new QSL_File();

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
            if (writeXml==true) {
                WriteXmlFile();
            }
        }

        public QSL_File GetQslFile() {
            while (isFinished == false) { }
            return qsl_File;
        }

        void ParseFile()
        {
            int totalStages = BitConverter.ToInt32(rawBytes, 12);
            int offsetToPointerList = BitConverter.ToInt32(rawBytes, 20);
            qsl_File.Stages = new List<Stage>();

            qsl_File.I_10 = BitConverter.ToInt16(rawBytes, 10);

            for (int i = 0; i < totalStages; i++) {
                int offsetToStage = BitConverter.ToInt32(rawBytes, offsetToPointerList);
                int offsetToEntries = BitConverter.ToInt32(rawBytes, offsetToStage + 12) + offsetToStage;
                short numberOfEntries = BitConverter.ToInt16(rawBytes, offsetToStage + 10);

                qsl_File.Stages.Add(new Stage() {
                    StageID = BitConverter.ToInt32(rawBytes, offsetToStage + 0),
                    I_04 = new short[3] {
                        BitConverter.ToInt16(rawBytes, offsetToStage + 4),
                        BitConverter.ToInt16(rawBytes, offsetToStage + 6),
                        BitConverter.ToInt16(rawBytes, offsetToStage + 8),
                    },
                    Entries = ReadPositionEntries(rawBytes, offsetToEntries, numberOfEntries)
                });

                offsetToPointerList += 4;
            }

        }


        private List<Position_Entry> ReadPositionEntries(byte[] _bytes, int offset, int count) {
            List<Position_Entry> posEntries = new List<Position_Entry>();

            for (int i = 0; i < count; i++) {
                posEntries.Add(new Position_Entry()
                {
                    MapString = Utils.GetString(_bytes.ToList(), offset, 32),
                    I_32 = BitConverter.ToInt16(_bytes, offset + 32),
                    I_34 = BitConverter.ToInt32(_bytes, offset + 34),
                    I_38 = new short[13] {
                        BitConverter.ToInt16(rawBytes, offset + 38 ),
                        BitConverter.ToInt16(rawBytes, offset + 40 ),
                        BitConverter.ToInt16(rawBytes, offset + 42 ),
                        BitConverter.ToInt16(rawBytes, offset + 44 ),
                        BitConverter.ToInt16(rawBytes, offset + 46 ),
                        BitConverter.ToInt16(rawBytes, offset + 48 ),
                        BitConverter.ToInt16(rawBytes, offset + 50 ),
                        BitConverter.ToInt16(rawBytes, offset + 52 ),
                        BitConverter.ToInt16(rawBytes, offset + 54 ),
                        BitConverter.ToInt16(rawBytes, offset + 56 ),
                        BitConverter.ToInt16(rawBytes, offset + 58 ),
                        BitConverter.ToInt16(rawBytes, offset + 60 ),
                        BitConverter.ToInt16(rawBytes, offset + 62 )
                }
                });
                offset += 64;
            }
            return posEntries;
        }

        void WriteXmlFile() {
            YAXSerializer serializer = new YAXSerializer(typeof(QSL_File));
            serializer.SerializeToFile(qsl_File, saveLocation + ".xml");
        }
    }
}
