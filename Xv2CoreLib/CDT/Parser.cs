using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CDT
{
    public class Parser
    {
        private string saveLocation;
        private byte[] rawBytes;
        public CDT_File cdtFile { get; private set; } = new CDT_File();

        public Parser(string path, bool _writeXml)
        {
            saveLocation = path;
            rawBytes = File.ReadAllBytes(path);

            Parse();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(CDT_File));
                serializer.SerializeToFile(cdtFile, saveLocation + ".xml");
            }
        }

        public Parser(byte[] bytes)
        {
            rawBytes = bytes;

            Parse();
        }

        private void Parse()
        {
            uint count = BitConverter.ToUInt32(rawBytes, 4);
            cdtFile.I_08 = BitConverter.ToUInt32(rawBytes, 8);
            cdtFile.I_12 = BitConverter.ToUInt32(rawBytes, 12);
            int offset = 16;

            if (count > 0)
            {
                cdtFile.Entries = new List<CDT_Entry>();

                for(int i = 0; i < count; i++)
                {
                    cdtFile.Entries.Add(new CDT_Entry() { ID = BitConverter.ToInt32(rawBytes, offset).ToString() });

                    cdtFile.Entries[i].TextLeftLength = BitConverter.ToInt32(rawBytes, offset + 4);
                    cdtFile.Entries[i].TextLeft = StringEx.GetString(rawBytes, offset + 8, false, StringEx.EncodingType.Unicode, (int)cdtFile.Entries[i].TextLeftLength);
                    offset += (int)cdtFile.Entries[i].TextLeftLength - 2;
                    cdtFile.Entries[i].TextRightLength = BitConverter.ToInt32(rawBytes, (int)(offset + 10));
                    cdtFile.Entries[i].TextRight = StringEx.GetString(rawBytes, offset + 14, false, StringEx.EncodingType.Unicode, (int)cdtFile.Entries[i].TextRightLength);
                    offset += (int)cdtFile.Entries[i].TextRightLength - 2;
                    cdtFile.Entries[i].I_16 = BitConverter.ToInt32(rawBytes, offset + 16);
                    cdtFile.Entries[i].I_20 = BitConverter.ToInt32(rawBytes, offset + 20);
                    cdtFile.Entries[i].I_24 = BitConverter.ToInt32(rawBytes, offset + 24);

                    offset += 28;
                }
            }
        }
    }
}
