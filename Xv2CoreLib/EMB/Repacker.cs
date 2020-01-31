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
    class Repacker
    {
        string directoryLocation;
        string saveLocation;
        EmbIndex EmbIndexXml { get; set; }
        List<byte> bytes = new List<byte>() { 35, 69, 77, 66, 254, 255, 32, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public Repacker(string fileLocation) {
            directoryLocation = fileLocation;
            saveLocation = String.Format("{0}/{1}.emb", Path.GetDirectoryName(fileLocation), Path.GetFileName(fileLocation));
            if (ReadXmlFile()) {
                WriteBinaryEmb();
            }
        }
        public Repacker(string fileLocation, EmbIndex embIndex)
        {
            directoryLocation = fileLocation;
            saveLocation = String.Format("{0}/{1}.emb", Path.GetDirectoryName(fileLocation), Path.GetFileName(fileLocation));
            EmbIndexXml = embIndex;
            WriteBinaryEmb();
        }

        bool ReadXmlFile() {
            if (!File.Exists(directoryLocation + "/EmbIndex.xml")){
                Console.WriteLine("Could not find \"EmbIndex.xml\"\n" +
                    "Extraction failed.");
                Console.ReadLine();
                return false;
            }
            try
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EmbIndex));
                EmbIndexXml = (EmbIndex)serializer.DeserializeFromFile(directoryLocation + "/EmbIndex.xml");
                return true;
            }
            catch
            {
                Console.WriteLine("Could not read \"EmbIndex.xml\". It is corrupted!\n" +
                    "Extraction failed.");
                Console.ReadLine();
                return false;
            }
        }

        void WriteBinaryEmb() {
            List<int> dataOffsets = new List<int>();
            List<int> stringOffsets = new List<int>();
            List<byte[]> files = new List<byte[]>();

            //Getting all file paths;
            int totalEntries = EmbIndexXml.Entry.Count();


            for (int i = 0; i < totalEntries; i++) {
                try
                {
                    files.Add(File.ReadAllBytes(String.Format("{0}/{1}", directoryLocation, EmbIndexXml.Entry[i].Name)));
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine(String.Format("The file with the name \"{0}\" at Index {1} could not be found in the directory!\nRepack failed.", EmbIndexXml.Entry[i].Name, EmbIndexXml.Entry[i].Index));
                    Console.ReadLine();
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine(String.Format("The file at Index {1} does not have a name!\nRepack failed.", EmbIndexXml.Entry[i].Name, EmbIndexXml.Entry[i].Index));
                    Console.ReadLine();
                    return;
                }
            }

            for (int i = 0; i < totalEntries; i++)//Adding pointer blank spaces, and setting size
            {
                dataOffsets.Add(bytes.Count());
                bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                bytes.AddRange(BitConverter.GetBytes(files[i].Count()));
            }

            if(EmbIndexXml.UseFileNames == true)
            {
                for (int i = 0; i < totalEntries; i++)
                {
                    stringOffsets.Add(bytes.Count());
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                }
            }
            

            //Padding between string pointers and first entry
            float fileSize = Convert.ToSingle(bytes.Count());
            int addedSize = 0;
            while (fileSize / 64 != Math.Floor(fileSize / 64)) {
                addedSize++;
                fileSize++;
            }
            for (int a = 0; a < addedSize; a++)
            {
                bytes.Add(0);
            }

            int reducedOffset = 32;
            for (int i = 0; i < totalEntries; i++)//Updates data offsets, adds data entries
            {
                
                float size = Convert.ToSingle(files[i].Count());
                int addedPadding = 0;
                while(size / 64 != Math.Floor(size/64)) {//Adding the required padding
                    size++;
                    addedPadding++;
                }

                
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - reducedOffset),dataOffsets[i]);
                bytes.AddRange(files[i]);
                reducedOffset += 8;

                for (int a = 0; a < addedPadding; a++) {
                    bytes.Add(0);
                }
            }
            
            reducedOffset = 0;
            if(EmbIndexXml.UseFileNames == true)
            {
                for (int i = 0; i < totalEntries; i++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), stringOffsets[i]);
                    bytes.AddRange(Encoding.ASCII.GetBytes(EmbIndexXml.Entry[i].Name)); bytes.Add(0);
                }
            }
            

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(EmbIndexXml.I_08), 8);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(EmbIndexXml.I_10), 10);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((short)totalEntries), 12);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(dataOffsets[0]), 24);
            if(EmbIndexXml.UseFileNames == true)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(stringOffsets[0]), 28);
            }

            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

    }
}
