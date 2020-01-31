using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using YAXLib;
using Xv2CoreLib;

namespace EmbPack_LB.EMB
{
    public class Extractor
    {
        const int embSignature = 1112360227;

        //Signatures above
        string saveLocation;
        byte[] rawBytes;
        List<byte> bytes;

        EmbIndex EmbIndexXml = new EmbIndex() {Entry = new List<EmbEntry>() };

        //header info
        int totalEntries;
        int contentsOffset;
        int fileNameTableOffset;

        public Extractor(string fileLocation) {
            rawBytes = File.ReadAllBytes(fileLocation);
            bytes = rawBytes.ToList();
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(fileLocation), Path.GetFileNameWithoutExtension(fileLocation));
            Validation();
            totalEntries = BitConverter.ToInt32(rawBytes, 12);
            contentsOffset = BitConverter.ToInt32(rawBytes, 24);
            fileNameTableOffset = BitConverter.ToInt32(rawBytes, 28);
            ParseFile();
        }

        void Validation () {
            if (BitConverter.ToInt32(rawBytes, 0) != embSignature)
            {
                Console.WriteLine("File validation failed.");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        void ParseFile() {
            List<int> dataContentsOffsets = new List<int>();
            List<int> dataContentsSize = new List<int>();
            List<int> stringOffsets = new List<int>();

            //Header data (saving it to XML)
            EmbIndexXml.I_08 = BitConverter.ToUInt16(rawBytes, 8);
            EmbIndexXml.I_10 = BitConverter.ToUInt16(rawBytes, 10);
            EmbIndexXml.UseFileNames = (fileNameTableOffset == 0) ? false : true;

            //Create directory to extract files to
            if (!Directory.Exists(saveLocation)) {
                Directory.CreateDirectory(saveLocation);
            }
            
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
                byte[] EntryToExtract = bytes.GetRange(dataContentsOffsets[i], dataContentsSize[i]).ToArray();
                File.WriteAllBytes(String.Format("{0}/{1}", saveLocation, fileName), EntryToExtract);

                
                //Adding entry to XML
                EmbIndexXml.Entry.Add(new EmbEntry()
                {
                    Index = i,
                    Name = fileName
                });

                //Displaying something on Consle
                Console.WriteLine(fileName + " extracted.");
            }

            WriteXmlFile();
        }

        void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(EmbIndex));
            serializer.SerializeToFile(EmbIndexXml, String.Format("{0}/EmbIndex.xml",saveLocation));
        }


    }
}
