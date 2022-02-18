using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YAXLib;

namespace Xv2CoreLib.CMS
{
    public class Deserializer
    {
        string saveLocation;
        CMS_File cmsFile;
        public List<byte> bytes = new List<byte>() { 35, 67, 77, 83, 254, 255, 0, 0 };

        //Offset lists
        int EntryCount { get; set; }
        List<int> MainEntryOffsets = new List<int>();

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(CMS_File), YAXSerializationOptions.DontSerializeNullObjects);
            cmsFile = (CMS_File)serializer.DeserializeFromFile(location);
            Validation();
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(CMS_File _cmsFile, string location)
        {
            saveLocation = location;
            cmsFile = _cmsFile;
            Validation();
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(CMS_File _cmsFile)
        {
            cmsFile = _cmsFile;
            Validation();
            Write();
        }

        private void Validation()
        {
            cmsFile.SortEntries();
        }

        private void Write()
        {
            int count = (cmsFile.CMS_Entries != null) ? cmsFile.CMS_Entries.Count() : 0;
            List<List<int>> strOffsets = new List<List<int>>();
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(16));

            if(count > 0)
            {
                //CMS Entries
                for(int i = 0; i < count; i++)
                {
                    if(cmsFile.CMS_Entries[i].Str_04.Length != 3)
                    {
                        throw new InvalidDataException($"CMS Entry Name is invalid. Must be 3 characters! ({cmsFile.CMS_Entries[i].ShortName}");
                    }

                    strOffsets.Add(new List<int>());
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(cmsFile.CMS_Entries[i].Index)));
                    bytes.AddRange(Encoding.ASCII.GetBytes(cmsFile.CMS_Entries[i].Str_04));
                    bytes.AddRange(new byte[4 - cmsFile.CMS_Entries[i].Str_04.Length]);
                    bytes.AddRange(BitConverter.GetBytes(cmsFile.CMS_Entries[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(cmsFile.CMS_Entries[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(cmsFile.CMS_Entries[i].I_20));
                    bytes.AddRange(BitConverter.GetBytes(cmsFile.CMS_Entries[i].I_22));
                    bytes.AddRange(BitConverter.GetBytes(cmsFile.CMS_Entries[i].I_24));
                    bytes.AddRange(BitConverter.GetBytes(cmsFile.CMS_Entries[i].I_26));
                    bytes.AddRange(BitConverter.GetBytes(cmsFile.CMS_Entries[i].I_28));
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[8]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[8]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[12]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);

                }

                //STR Entries
                for(int i = 0; i < count; i++)
                {
                    if(cmsFile.CMS_Entries[i].Str_32 != "NULL" && !string.IsNullOrWhiteSpace(cmsFile.CMS_Entries[i].Str_32))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][0]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(cmsFile.CMS_Entries[i].Str_32));
                        bytes.Add(0);
                    }
                    if (cmsFile.CMS_Entries[i].Str_36 != "NULL" && !string.IsNullOrWhiteSpace(cmsFile.CMS_Entries[i].Str_36))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][1]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(cmsFile.CMS_Entries[i].Str_36));
                        bytes.Add(0);
                    }
                    if (cmsFile.CMS_Entries[i].Str_44 != "NULL" && !string.IsNullOrWhiteSpace(cmsFile.CMS_Entries[i].Str_44))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][2]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(cmsFile.CMS_Entries[i].Str_44));
                        bytes.Add(0);
                    }
                    if (cmsFile.CMS_Entries[i].Str_48 != "NULL" && !string.IsNullOrWhiteSpace(cmsFile.CMS_Entries[i].Str_48))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][3]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(cmsFile.CMS_Entries[i].Str_48));
                        bytes.Add(0);
                    }
                    if (cmsFile.CMS_Entries[i].Str_56 != "NULL" && !string.IsNullOrWhiteSpace(cmsFile.CMS_Entries[i].Str_56))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][4]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(cmsFile.CMS_Entries[i].Str_56));
                        bytes.Add(0);
                    }
                    if (cmsFile.CMS_Entries[i].Str_60 != "NULL" && !string.IsNullOrWhiteSpace(cmsFile.CMS_Entries[i].Str_60))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][5]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(cmsFile.CMS_Entries[i].Str_60));
                        bytes.Add(0);
                    }
                    if (cmsFile.CMS_Entries[i].Str_64 != "NULL" && !string.IsNullOrWhiteSpace(cmsFile.CMS_Entries[i].Str_64))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][6]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(cmsFile.CMS_Entries[i].Str_64));
                        bytes.Add(0);
                    }
                    if (cmsFile.CMS_Entries[i].Str_68 != "NULL" && !string.IsNullOrWhiteSpace(cmsFile.CMS_Entries[i].Str_68))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][7]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(cmsFile.CMS_Entries[i].Str_68));
                        bytes.Add(0);
                    }
                    if (cmsFile.CMS_Entries[i].Str_80 != "NULL" && !string.IsNullOrWhiteSpace(cmsFile.CMS_Entries[i].Str_80))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][8]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(cmsFile.CMS_Entries[i].Str_80));
                        bytes.Add(0);
                    }
                }

            }


        }

    }
}
