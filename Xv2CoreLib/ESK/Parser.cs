using System;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.ESK
{
    public class Parser
    {
        public ESK_File eskFile { private set; get; }
        byte[] rawBytes { get; set; }


        public Parser(string location, bool writeXml)
        {
            eskFile = new ESK_File();
            rawBytes = File.ReadAllBytes(location);
            Parse();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(ESK_File));
                serializer.SerializeToFile(eskFile, location + ".xml");
            }

        }

        public Parser(byte[] _bytes)
        {
            eskFile = new ESK_File();
            rawBytes = _bytes;
            Parse();
        }

        private void Parse()
        {
            //Header
            eskFile.Version = BitConverter.ToUInt16(rawBytes, 8);
            eskFile.I_10 = BitConverter.ToUInt16(rawBytes, 10);
            eskFile.I_12 = BitConverter.ToInt32(rawBytes, 12);
            eskFile.I_24 = BitConverter.ToInt32(rawBytes, 24);

            //Skeleton
            eskFile.Skeleton = ESK_Skeleton.Read(rawBytes, BitConverter.ToInt32(rawBytes, 16), true);
        }

    }
}
