using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.QML
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        public QML_File qml_File = new QML_File();

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

        void ParseFile() 
        {
            int totalEntries = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 12);
            qml_File.Entries = new List<QML_Entry>();

            for (int i = 0; i < totalEntries; i++)
            {
                qml_File.Entries.Add(new QML_Entry());

                for (int a = 0; a < 76; a++)
                {
                    qml_File.Entries[i].SortID = BitConverter.ToInt32(rawBytes, offset);
                    qml_File.Entries[i].I_04 = BitConverter.ToInt32(rawBytes, offset + 4);
                    qml_File.Entries[i].I_08 = BitConverter.ToInt32(rawBytes, offset + 8);
                    qml_File.Entries[i].I_12 = BitConverter.ToInt32(rawBytes, offset + 12);
                    qml_File.Entries[i].I_16 = BitConverter.ToInt32(rawBytes, offset + 16);
                    qml_File.Entries[i].I_20 = BitConverter.ToInt32(rawBytes, offset + 20);
                    qml_File.Entries[i].I_24 = BitConverter.ToInt32(rawBytes, offset + 24);
                    qml_File.Entries[i].I_28 = BitConverter.ToInt32(rawBytes, offset + 28);
                    qml_File.Entries[i].I_32 = BitConverter.ToInt32(rawBytes, offset + 32);
                    qml_File.Entries[i].I_36 = BitConverter.ToInt32(rawBytes, offset + 36);
                    qml_File.Entries[i].I_40 = BitConverter.ToInt32(rawBytes, offset + 40);
                    qml_File.Entries[i].I_44 = BitConverter.ToInt32(rawBytes, offset + 44);
                    qml_File.Entries[i].I_48 = new short[5];

                    for (int e = 0; e < 10; e += 2) {

                        qml_File.Entries[i].I_48[e/2] = BitConverter.ToInt16(rawBytes, offset + 48 + e);
                    }

                    qml_File.Entries[i].SkillSet = new Skills()
                    {
                        I_00 = BitConverter.ToUInt16(rawBytes, offset + 58),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 60),
                        I_04 = BitConverter.ToUInt16(rawBytes, offset + 62),
                        I_06 = BitConverter.ToUInt16(rawBytes, offset + 64),
                        I_08 = BitConverter.ToUInt16(rawBytes, offset + 66),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 68),
                        I_12 = BitConverter.ToUInt16(rawBytes, offset + 70),
                        I_14 = BitConverter.ToUInt16(rawBytes, offset + 72),
                        I_16 = BitConverter.ToUInt16(rawBytes, offset + 74),
                    };

                }

                offset += 76;
            }
        }

        void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(QML_File));
            serializer.SerializeToFile(qml_File, saveLocation + ".xml");
        }
    }
}
