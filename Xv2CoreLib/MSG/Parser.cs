using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using YAXLib;
using System.IO;

namespace Xv2CoreLib.MSG
{
    public class Parser
    {
        string saveLocation;
        List<byte> bytes;
        byte[] rawBytes;
        public MSG_File msg_File { get; private set; } = new MSG_File();
        bool unicode_names = false;
        bool unicode_msg = false;

        //State
        bool writeXml = false;

        public Parser(string location, bool _writeXml)
        {
            writeXml = _writeXml;
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            bytes = rawBytes.ToList();
            UnicodeCheck();
            Parse();
            if (writeXml == true)
            {
                WriteXmlFile();
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            bytes = rawBytes.ToList();
            if (bytes != null)
            {
                UnicodeCheck();
                Parse();
            }
            else
            {
                msg_File = null;
            }
        }

        public MSG_File GetMsgFile()
        {
            return msg_File;
        }

        void UnicodeCheck()
        {
            int names = BitConverter.ToInt16(rawBytes, 4);
            int msg = BitConverter.ToInt16(rawBytes, 6);

            if (names == 256)
            {
                unicode_names = true;
                msg_File.unicode_names = true;
            }
            if (msg == 1)
            {
                unicode_msg = true;
                msg_File.unicode_msg = true;
            }
        }

        void Parse()
        {
            int entryCount = BitConverter.ToInt32(rawBytes, 8);
            int nameSectionOffset = BitConverter.ToInt32(rawBytes, 12);
            int idSectionOffset = BitConverter.ToInt32(rawBytes, 16);
            int linesSectionOffset = BitConverter.ToInt32(rawBytes, 20);
            int stringsSectionOffset = BitConverter.ToInt32(rawBytes, 28);
            
            msg_File.MSG_Entries = new List<MSG_Entry>();
            

            for (int i = 0; i < entryCount; i++)
            {
                msg_File.MSG_Entries.Add(new MSG_Entry());
                //Name Section
                int sizeNames;

                if (unicode_names == false)
                {
                    sizeNames = BitConverter.ToInt32(rawBytes, nameSectionOffset + 4);
                }
                else
                {
                    sizeNames = BitConverter.ToInt32(rawBytes, nameSectionOffset + 8);
                }

                int nameStringOffset = BitConverter.ToInt32(rawBytes, nameSectionOffset);

                msg_File.MSG_Entries[i].DebugIndex = i;
                msg_File.MSG_Entries[i].Name = Utils.GetString(bytes, nameStringOffset, sizeNames, unicode_names);
                msg_File.MSG_Entries[i].I_12 = BitConverter.ToInt32(rawBytes, nameSectionOffset + 12);
                nameSectionOffset += 16;

                //ID Section
                msg_File.MSG_Entries[i].Index = BitConverter.ToInt32(rawBytes, idSectionOffset).ToString();
                idSectionOffset += 4;

                //Line and String Section
                int countOfStrings = BitConverter.ToInt32(rawBytes, linesSectionOffset + 0);
                int offsetToStringSection = BitConverter.ToInt32(rawBytes, linesSectionOffset + 4);
                linesSectionOffset += 8;
                msg_File.MSG_Entries[i].Msg_Content = new List<Msg_Line>();

                for (int a = 0; a < countOfStrings; a++)
                {
                    int offsetToString = BitConverter.ToInt32(rawBytes, offsetToStringSection);

                    int sizeMsg;

                    if (unicode_msg == false)
                    {
                        sizeMsg = BitConverter.ToInt32(rawBytes, offsetToStringSection + 4);
                    }
                    else
                    {
                        sizeMsg = BitConverter.ToInt32(rawBytes, offsetToStringSection + 8);
                    }

                    msg_File.MSG_Entries[i].Msg_Content.Add(new Msg_Line()
                    {
                        Text = Utils.GetString(bytes, offsetToString, sizeMsg, unicode_msg),
                        I_12 = BitConverter.ToInt32(rawBytes, offsetToStringSection + 12)
                    });

                    offsetToStringSection += 16;
                }

                stringsSectionOffset += 16;
            }
        }

        void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(MSG_File));
            serializer.SerializeToFile(msg_File, saveLocation + ".xml");
        }
    }
}
