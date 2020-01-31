using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CSO
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        List<byte> bytes;

        public CSO_File csoFile { get; private set; } = new CSO_File();


        public Parser(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
            bytes = rawBytes.ToList();
            ParseFile();
        }

        public Parser(string fileLocation, bool writeXml)
        {
            rawBytes = File.ReadAllBytes(fileLocation);
            bytes = rawBytes.ToList();
            saveLocation = String.Format("{0}.xml", fileLocation);
            ParseFile();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(CSO_File));
                serializer.SerializeToFile(csoFile, saveLocation);
            }
        }

        public CSO_File GetCsoFile()
        {
            return csoFile;
        }

        private void ParseFile()
        {
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 12);

            if(count > 0)
            {
                csoFile.CsoEntries = new List<CSO_Entry>();

                for (int i = 0; i < count; i++)
                {
                    csoFile.CsoEntries.Add(new CSO_Entry()
                    {
                        I_00 = BitConverter.ToUInt32(rawBytes, offset + 0).ToString(),
                        I_04 = BitConverter.ToUInt32(rawBytes, offset + 4),
                        Str_08 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                        Str_12 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 12)),
                        Str_16 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                        Str_20 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 20))
                    });
                    offset += 32;
                }
            }
            

        }
    }
}
