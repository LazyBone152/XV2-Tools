using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using YAXLib;
using Xv2CoreLib;

namespace Xv2CoreLib.EMB_CLASS
{
    public class Deserializer
    {
        string saveLocation;
        EMB_File embFile { get; set; }
        public List<byte> bytes = new List<byte>() { 35, 69, 77, 66, 254, 255, 32, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public Deserializer(string fileLocation)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(fileLocation), Path.GetFileNameWithoutExtension(fileLocation));
            YAXSerializer serializer = new YAXSerializer(typeof(EMB_File), YAXSerializationOptions.DontSerializeNullObjects);
            embFile = (EMB_File)serializer.DeserializeFromFile(fileLocation);
            WriteBinaryEmb();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(string fileLocation, EMB_File _embFile)
        {
            saveLocation = (Path.GetExtension(fileLocation) == ".xml") ? String.Format("{0}/{1}", Path.GetDirectoryName(fileLocation), Path.GetFileNameWithoutExtension(fileLocation)) : fileLocation;
            embFile = _embFile;
            WriteBinaryEmb();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EMB_File _embFile)
        {
            embFile = _embFile;
            WriteBinaryEmb();
        }

        void WriteBinaryEmb()
        {
            List<int> dataOffsets = new List<int>();
            List<int> stringOffsets = new List<int>();
            List<byte[]> files = new List<byte[]>();

            if(embFile.Entry != null)
            {
                //Getting all file paths;
                int totalEntries = embFile.Entry.Count;


                for (int i = 0; i < totalEntries; i++)
                {
                    files.Add(embFile.Entry[i].Data);
                }

                for (int i = 0; i < totalEntries; i++)//Adding pointer blank spaces, and setting size
                {
                    dataOffsets.Add(bytes.Count());
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                    bytes.AddRange(BitConverter.GetBytes(files[i].Count()));
                }

                if (embFile.UseFileNames == true)
                {
                    for (int i = 0; i < totalEntries; i++)
                    {
                        stringOffsets.Add(bytes.Count());
                        bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                    }
                }


                //Padding between string pointers and first entry
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 64)]);

                int reducedOffset = 32;
                for (int i = 0; i < totalEntries; i++)//Updates data offsets, adds data entries
                {
                    int addedPadding = Utils.CalculatePadding(bytes.Count, 64);

                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - reducedOffset), dataOffsets[i]);
                    bytes.AddRange(files[i]);
                    reducedOffset += 8;

                    for (int a = 0; a < addedPadding; a++)
                    {
                        bytes.Add(0);
                    }
                }

                reducedOffset = 0;
                if (embFile.UseFileNames == true)
                {
                    for (int i = 0; i < totalEntries; i++)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), stringOffsets[i]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(embFile.Entry[i].Name)); bytes.Add(0);
                    }
                }


                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(embFile.I_08), 8);
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(embFile.I_10), 10);
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((short)totalEntries), 12);
                if (dataOffsets.Count > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(dataOffsets[0]), 24);
                }
                if (embFile.UseFileNames == true && stringOffsets.Count > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(stringOffsets[0]), 28);
                }

            }
            
            
        }

    }
}
