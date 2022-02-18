using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.CSO
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;

        public CSO_File csoFile { get; private set; } = new CSO_File();


        public Parser(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
            ParseFile();
        }

        public Parser(string fileLocation, bool writeXml)
        {
            rawBytes = File.ReadAllBytes(fileLocation);
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
                        CharaID = BitConverter.ToInt32(rawBytes, offset + 0),
                        Costume = BitConverter.ToUInt32(rawBytes, offset + 4),
                        SePath = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8), false),
                        VoxPath = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 12), false),
                        AmkPath = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16), false),
                        SkillCharaCode = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 20), false)
                    });
                    offset += 32;
                }
            }
            

        }
    }
}
