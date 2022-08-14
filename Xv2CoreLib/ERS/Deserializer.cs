using System;
using System.Collections.Generic;
using System.Text;
using YAXLib;
using System.IO;

namespace Xv2CoreLib.ERS
{
    public class Deserializer
    {

        string saveLocation;
        public ERS_File ers_File { get; private set; }
        public List<byte> bytes = new List<byte>() { 35, 69, 82, 83, 254, 255, 24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 24, 0, 0, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            ReadXmlFile(location);
            Validation();
            WriteBinaryErsFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(ERS_File _ersFile, string location)
        {
            saveLocation = location;
            ers_File = _ersFile;
            Validation();
            WriteBinaryErsFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(ERS_File _ersFile)
        {
            ers_File = _ersFile;
            Validation();
            WriteBinaryErsFile();
        }

        private void Validation()
        {
            for(int i = 0; i < ers_File.Entries.Count; i++)
            {
                if(ers_File.Entries[i].SubEntries != null)
                {
                    ers_File.Entries[i].SubEntries = Sorting.SortEntries(ers_File.Entries[i].SubEntries);
                }
            }
        }

        void ReadXmlFile(string path)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(ERS_File), YAXSerializationOptions.DontSerializeNullObjects);
            ers_File = (ERS_File)serializer.DeserializeFromFile(path);
            
        }

        void WriteBinaryErsFile()
        {
            List<int> mainTableOffsetToEntry = new List<int>();//offset to entry pointer list

            int totalMainTables = 0;
            for (int i = 0; i < ers_File.Entries.Count; i++)
            {
                //gets the magic number for the header, and so I can do a loop on the main table offsets
                if (ushort.Parse(ers_File.Entries[i].Index) > totalMainTables)
                {
                    totalMainTables = ushort.Parse(ers_File.Entries[i].Index);
                }
            }
            totalMainTables++;
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(totalMainTables), 12);



            for (int i = 0; i < totalMainTables * 4; i += 4)
            {
                //Creating initial table pointer section
                bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
            }

            for (int i = 0; i < ers_File.Entries.Count; i++)
            {
                int initialTableOffset = ushort.Parse(ers_File.Entries[i].Index) * 4;
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 24 + initialTableOffset);
                ers_File.Entries[i].offset = bytes.Count;
                bytes.AddRange(BitConverter.GetBytes(ushort.Parse(ers_File.Entries[i].Index)));
                bytes.AddRange(new List<byte> { 0, 0, 0, 0, 0, 0, 0, 0 });
                int subentryCount = (ers_File.Entries[i].SubEntries != null) ? GetTotalSubEntryCount(ers_File.Entries[i].SubEntries) : 0;
                subentryCount = (ers_File.Entries[i].Dummy != null) ? ers_File.Entries[i].Dummy.Count : subentryCount;

                bytes.AddRange(BitConverter.GetBytes((short)subentryCount));
                mainTableOffsetToEntry.Add(bytes.Count);
                bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
            }

            for (int i = 0; i < ers_File.Entries.Count; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - ers_File.Entries[i].offset), ers_File.Entries[i].offset + 12);
                if(ers_File.Entries[i].SubEntries != null)
                {
                    int count = GetTotalSubEntryCount(ers_File.Entries[i].SubEntries);
                    for(int h = 0; h < count; h++)
                    {
                        bool nullEntry = true;
                        for (int a = 0; a < ers_File.Entries[i].SubEntries.Count; a++)
                        {
                            if(int.Parse(ers_File.Entries[i].SubEntries[a].Index) == h)
                            {
                                ers_File.Entries[i].SubEntries[a].offset = bytes.Count;
                                bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                                nullEntry = false;
                                break;
                            }
                        }

                        if (nullEntry)
                        {
                            bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                        }
                    }
                    
                }
                else if (ers_File.Entries[i].Dummy != null)
                {
                    int dummy_next = 0;
                    foreach(string s in ers_File.Entries[i].Dummy)
                    {
                        int ID = int.Parse(s);
                        if(ID != dummy_next)
                        {
                            throw new InvalidDataException(String.Format("Invalid \"Dummy\" order."));
                        }
                        dummy_next++;
                        bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                    }
                }
                
            }

            for (int i = 0; i < ers_File.Entries.Count; i++)
            {
                if(ers_File.Entries[i].SubEntries != null)
                {
                    for (int a = 0; a < ers_File.Entries[i].SubEntries.Count; a++)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - ers_File.Entries[i].offset), ers_File.Entries[i].SubEntries[a].offset);

                        bytes.AddRange(BitConverter.GetBytes(int.Parse(ers_File.Entries[i].SubEntries[a].Index)));

                        //Name
                        if(ers_File.Entries[i].SubEntries[a].Str_04 == "NULL" || string.IsNullOrWhiteSpace(ers_File.Entries[i].SubEntries[a].Str_04))
                        {
                            bytes.AddRange(new byte[8]);
                        }
                        else
                        {
                            if (ers_File.Entries[i].SubEntries[a].Str_04.Length > 8)
                                throw new InvalidDataException("Name cannot be larger than 8 characters.");

                            bytes.AddRange(Utils.GetStringBytes(ers_File.Entries[i].SubEntries[a].Str_04, 8));
                        }

                        ers_File.Entries[i].SubEntries[a].offsetToString = bytes.Count;
                        bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                    }
                }
            }

            for (int i = 0; i < ers_File.Entries.Count; i++)
            {
                if(ers_File.Entries[i].SubEntries != null)
                {
                    for (int a = 0; a < ers_File.Entries[i].SubEntries.Count; a++)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - ers_File.Entries[i].SubEntries[a].offsetToString + 12), ers_File.Entries[i].SubEntries[a].offsetToString);
                        bytes.AddRange(Encoding.ASCII.GetBytes(ers_File.Entries[i].SubEntries[a].FILE_PATH));
                        bytes.Add(0);
                    }
                }
            }
            

        }

        private int GetTotalSubEntryCount(List<ERS_MainTableEntry> subEntry)
        {
            int count = 0;
            foreach(var e in subEntry)
            {
                if(count < int.Parse(e.Index))
                {
                    count = int.Parse(e.Index);
                }
            }
            return count + 1;
        }

    }
}
