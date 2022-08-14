using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YAXLib;

namespace Xv2CoreLib.QSF
{
    public class Deserializer
    {
        private string saveLocation;
        private QSF_File qsf_File;
        public List<byte> bytes = new List<byte>() { 70, 83, 81, 35, 0, 0, 0, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(QSF_File), YAXSerializationOptions.DontSerializeNullObjects);
            qsf_File = (QSF_File)serializer.DeserializeFromFile(location); WriteFile();
            SaveBinaryFile();
        }

        public Deserializer(QSF_File _qsfFile, string location)
        {
            saveLocation = location;
            qsf_File = _qsfFile;
            WriteFile();
            SaveBinaryFile();
        }

        public Deserializer(QSF_File _qsfFile)
        {
            qsf_File = _qsfFile;
            WriteFile();
        }

        void WriteFile()
        {
            //offsets
            List<int> offsetToTypeString = new List<int>();
            List<int> tableSectionOffsets = new List<int>();
            List<int> dataSectionOffsets = new List<int>();
            List<int> entryOffsets = new List<int>();

            bytes.AddRange(BitConverter.GetBytes(qsf_File.QuestTypes.Count()));
            bytes.AddRange(BitConverter.GetBytes(qsf_File.I_12));

            for (int i = 0; i < qsf_File.QuestTypes.Count(); i++) 
            {
                offsetToTypeString.Add(bytes.Count());
                bytes.AddRange(new byte[4]);

                if (qsf_File.QuestTypes[i].QuestGroups != null)
                {
                    bytes.AddRange(BitConverter.GetBytes(qsf_File.QuestTypes[i].QuestGroups.Count()));
                }
                else 
                {
                    bytes.AddRange(new byte[4]);
                }

                tableSectionOffsets.Add(bytes.Count());
                bytes.AddRange(new byte[4]);

                bytes.AddRange(BitConverter.GetBytes(qsf_File.QuestTypes[i].I_12));
            }

            for (int i = 0; i < qsf_File.QuestTypes.Count(); i++) 
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - tableSectionOffsets[i]), tableSectionOffsets[i]);

                for (int a = 0; a < qsf_File.QuestTypes[i].QuestGroups.Count(); a++)
                {
                    bytes.AddRange(BitConverter.GetBytes(qsf_File.QuestTypes[i].QuestGroups[a].QuestEntries.Count()));
                    dataSectionOffsets.Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                }
            }

            int access = 0;
            for (int i = 0; i < qsf_File.QuestTypes.Count(); i++)
            {
                for (int a = 0; a < qsf_File.QuestTypes[i].QuestGroups.Count(); a++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - dataSectionOffsets[access]), dataSectionOffsets[access]);
                    access++;
                    for (int e = 0; e < qsf_File.QuestTypes[i].QuestGroups[a].QuestEntries.Count(); e++) 
                    {
                        
                        entryOffsets.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);
                        
                    }
                }
            }
            access = 0;
            for (int i = 0; i < qsf_File.QuestTypes.Count(); i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToTypeString[i]), offsetToTypeString[i]);
                bytes.AddRange(Encoding.ASCII.GetBytes(qsf_File.QuestTypes[i].Type));
                bytes.Add(0);

                for (int a = 0; a < qsf_File.QuestTypes[i].QuestGroups.Count(); a++)
                {
                    for (int e = 0; e < qsf_File.QuestTypes[i].QuestGroups[a].QuestEntries.Count(); e++) {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - entryOffsets[access]), entryOffsets[access]);
                        access++;
                        bytes.AddRange(Encoding.ASCII.GetBytes(qsf_File.QuestTypes[i].QuestGroups[a].QuestEntries[e].QuestID));
                        bytes.Add(0);
                    }
                }
            }

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 4);

        }

        void SaveBinaryFile()
        {
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

    }
}
