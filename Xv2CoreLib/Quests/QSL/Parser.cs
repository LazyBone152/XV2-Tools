using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.QSL
{
    public class Parser
    {
        private string saveLocation;
        private byte[] rawBytes;
        public QSL_File QslFile { get; private set; } = new QSL_File();

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
            int totalStages = BitConverter.ToInt32(rawBytes, 12);
            int offsetToPointerList = BitConverter.ToInt32(rawBytes, 20);
            QslFile.Stages = new List<StageEntry>();

            QslFile.I_10 = BitConverter.ToInt16(rawBytes, 10);

            for (int i = 0; i < totalStages; i++)
            {
                int offsetToStage = BitConverter.ToInt32(rawBytes, offsetToPointerList);
                int offsetToEntries = BitConverter.ToInt32(rawBytes, offsetToStage + 12) + offsetToStage;
                short numberOfEntries = BitConverter.ToInt16(rawBytes, offsetToStage + 10);

                QslFile.Stages.Add(new StageEntry()
                {
                    StageID = BitConverter.ToInt32(rawBytes, offsetToStage + 0),
                    I_04 = BitConverter_Ex.ToInt16Array(rawBytes, offsetToStage + 4, 3),
                    SubEntries = ReadPositionEntries(rawBytes, offsetToEntries, numberOfEntries)
                });

                offsetToPointerList += 4;
            }
        }

        private List<PositionEntry> ReadPositionEntries(byte[] _bytes, int offset, int count)
        {
            List<PositionEntry> posEntries = new List<PositionEntry>();

            for (int i = 0; i < count; i++)
            {
                posEntries.Add(new PositionEntry()
                {
                    Position = Utils.GetString(_bytes.ToList(), offset, 32),
                    Type = (QslPositionType)BitConverter.ToInt16(_bytes, offset + 32),
                    ID = BitConverter.ToUInt16(_bytes, offset + 34),
                    ChanceDialogue = BitConverter.ToUInt16(_bytes, offset + 36),
                    I_38 = BitConverter.ToUInt16(_bytes, offset + 38),
                    QML_Change = BitConverter.ToUInt16(_bytes, offset + 40),
                    DefaultPose = BitConverter.ToUInt16(_bytes, offset + 42),
                    TalkingPose = BitConverter.ToUInt16(_bytes, offset + 44),
                    EffectPose = BitConverter.ToUInt16(_bytes, offset + 46),
                    I_48 = BitConverter.ToUInt16(_bytes, offset + 48),
                    I_50 = BitConverter_Ex.ToUInt16Array(_bytes, offset + 50, 7)
                });

                offset += 64;
            }

            return posEntries;
        }

        private void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(QSL_File));
            serializer.SerializeToFile(QslFile, saveLocation + ".xml");
        }
    }
}
