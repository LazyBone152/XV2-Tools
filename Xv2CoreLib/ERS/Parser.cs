using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.ERS
{
    public class Parser
    {

        string saveLocation;
        List<byte> bytes;
        byte[] rawBytes;
        public ERS_File ersFile { get; private set; } = new ERS_File();

        public Parser(string location, bool writeXml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            bytes = rawBytes.ToList();
            ersFile.Entries = new List<ERS_MainTable>();
            ParseMainTable();
            if (writeXml)
            {
                WriteXmlFile();
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            bytes = rawBytes.ToList();
            ersFile.Entries = new List<ERS_MainTable>();
            ParseMainTable();
        }

        void ParseMainTable()
        {
            List<int> mainTableOffsets = new List<int>(); // "mainTable" lists are in sync 
            int totalEntries = BitConverter.ToInt32(rawBytes, 12);

            for (int i = 0; i < totalEntries * 4; i+=4)
            {
                if (BitConverter.ToInt32(rawBytes, 24 + i) != 0)
                {
                    mainTableOffsets.Add(BitConverter.ToInt32(rawBytes, 24 + i));
                }
            }
            
            mainTableOffsets.Sort();

            for (int i = 0; i < mainTableOffsets.Count(); i++)
            {
                //iterating over Main Tables
                ersFile.Entries.Add(new ERS_MainTable() { SubEntries = new List<ERS_MainTableEntry>() });
                ersFile.Entries[i].Index = BitConverter.ToUInt16(rawBytes, mainTableOffsets[i]).ToString();
                ersFile.Entries[i].SubEntries = new List<ERS_MainTableEntry>();

                int offsetForEntries = BitConverter.ToInt32(rawBytes, mainTableOffsets[i] + 12) + mainTableOffsets[i];
                int numberOfEntries = BitConverter.ToInt16(rawBytes, mainTableOffsets[i] + 10);
                int addedOffset = 0;

                for (int a = 0; a < numberOfEntries; a++)
                {
                    //iterating over entries of current Main Table
                    int currentOffset = BitConverter.ToInt32(rawBytes, offsetForEntries + addedOffset) + mainTableOffsets[i];//gets offset for the entry and stores it as an int (absolute offset)
                    if (BitConverter.ToInt32(rawBytes, offsetForEntries + addedOffset) != 0)
                    {
                        //Code below is operating on the MainTableEntry itself
                        ersFile.Entries[i].SubEntries.Add(new ERS_MainTableEntry()
                        {
                            Index = BitConverter.ToInt32(rawBytes, currentOffset).ToString(),
                            Str_04 = Utils.GetString(bytes, currentOffset + 4, 8),
                            FILE_PATH = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, currentOffset + 12) + currentOffset)
                        });

                        if(a == numberOfEntries - 1)
                        {
                            ersFile.Entries[i].Dummy = null;
                        }
                    }
                    else
                    { 
                        if(ersFile.Entries[i].Dummy == null)
                        {
                            ersFile.Entries[i].Dummy = new List<string>();
                        }
                        ersFile.Entries[i].Dummy.Add(a.ToString());
                    }
                    addedOffset += 4;
                }

                if(ersFile.Entries[i].SubEntries.Count > 0)
                {
                    ersFile.Entries[i].Dummy = null;
                }
            }
            
            
        }
        
        void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(ERS_File));
            serializer.SerializeToFile(ersFile, saveLocation + ".xml");
        }
    }
}
