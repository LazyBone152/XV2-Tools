using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using YAXLib;
using Xv2CoreLib;
using System.Collections.ObjectModel;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.EMB_CLASS
{
    public class Parser
    {
        const int embSignature = 1112360227;

        //Signatures above
        string saveLocation;
        byte[] rawBytes;
        List<byte> bytes;

        public EMB_File embFile { get; private set; } = new EMB_File() {Entry = AsyncObservableCollection<EmbEntry>.Create() };

        //header info
        int totalEntries;
        int contentsOffset;
        int fileNameTableOffset;

        public Parser(string fileLocation, bool writeXml) {
            rawBytes = File.ReadAllBytes(fileLocation);
            bytes = rawBytes.ToList();
            saveLocation = String.Format("{0}.xml", fileLocation);
            Validation();
            totalEntries = BitConverter.ToInt32(rawBytes, 12);
            contentsOffset = BitConverter.ToInt32(rawBytes, 24);
            fileNameTableOffset = BitConverter.ToInt32(rawBytes, 28);
            ParseFile();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EMB_File));
                serializer.SerializeToFile(embFile, saveLocation);
            }
        }

        public Parser(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
            bytes = rawBytes.ToList();
            Validation();
            totalEntries = BitConverter.ToInt32(rawBytes, 12);
            contentsOffset = BitConverter.ToInt32(rawBytes, 24);
            fileNameTableOffset = BitConverter.ToInt32(rawBytes, 28);
            ParseFile();
        }

        public EMB_File GetEmbFile()
        {
            return embFile;
        }

        void Validation ()
        {
            if (BitConverter.ToInt32(rawBytes, 0) != embSignature)
            {
                Console.WriteLine("#EMB signature not found.");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        void ParseFile() {
            List<int> dataContentsOffsets = new List<int>();
            List<int> dataContentsSize = new List<int>();
            List<int> stringOffsets = new List<int>();

            //Header data (saving it to XML)
            embFile.I_08 = BitConverter.ToUInt16(rawBytes, 8);
            embFile.I_10 = BitConverter.ToUInt16(rawBytes, 10);
            embFile.UseFileNames = (fileNameTableOffset == 0) ? false : true;
            
            //Gets offset and size for file contents (offset is absolute of course)
            for (int i = 0; i < totalEntries * 8; i+=8) {
                dataContentsOffsets.Add(BitConverter.ToInt32(rawBytes, contentsOffset + i) + contentsOffset + i);
                dataContentsSize.Add(BitConverter.ToInt32(rawBytes, contentsOffset + i + 4));
            }
            
            //Gets string offsets
            for (int i = 0; i < totalEntries * 4; i+=4)
            {
                if(fileNameTableOffset != 0)
                {
                    stringOffsets.Add(BitConverter.ToInt32(rawBytes, fileNameTableOffset + i));
                }
            }
            
            for (int i = 0; i < totalEntries; i++) {
                //Extracting File
                
                string fileName = (fileNameTableOffset != 0) ? Utils.GetString(bytes, stringOffsets[i]) : String.Format("DATA{0}.dds", i);
                
                embFile.Entry.Add(new EmbEntry()
                {
                    Index = i.ToString(),
                    Name = fileName,
                    Data = bytes.GetRange(dataContentsOffsets[i], dataContentsSize[i])
                });
            }
            
        }
    }
}
