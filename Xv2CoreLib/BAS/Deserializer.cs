using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BAS
{
    public class Deserializer
    {
        string saveLocation;
        BAS_File basFile;
        public List<byte> bytes = new List<byte>() { 35, 66, 65, 83, 254, 255, 16, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(BAS_File), YAXSerializationOptions.DontSerializeNullObjects);
            basFile = (BAS_File)serializer.DeserializeFromFile(location);
            
            WriteBas();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(BAS_File _basFile)
        {
            basFile = _basFile;
            WriteBas();
        }

        public Deserializer(BAS_File _basFile, string path)
        {
            basFile = _basFile;
            WriteBas();
            File.WriteAllBytes(path, bytes.ToArray());
        }

        public void WriteBas()
        {
            List<int> EntryOffsets = new List<int>();

            int count = (basFile.Entries != null) ? basFile.Entries.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(16));

            if(basFile.Entries != null)
            {
                for(int i = 0; i < basFile.Entries.Count(); i++)
                {
                    int subEntryCount = (basFile.Entries[i].SubEntries != null) ? basFile.Entries[i].SubEntries.Count() : 0;
                    bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(subEntryCount));
                    EntryOffsets.Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                }

                for (int i = 0; i < basFile.Entries.Count(); i++)
                {
                    if(basFile.Entries[i].SubEntries != null)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), EntryOffsets[i]);

                        for(int a = 0; a < basFile.Entries[i].SubEntries.Count(); a++)
                        {
                            //Name Str
                            if(basFile.Entries[i].SubEntries[a].Name.Count() > 8)
                            {
                                Console.WriteLine(String.Format("The name \"{0}\" exceeds the maximum length of 8!", basFile.Entries[i].SubEntries[a].Name));
                                Utils.WaitForInputThenQuit();
                            }
                            bytes.AddRange(Encoding.ASCII.GetBytes(basFile.Entries[i].SubEntries[a].Name));
                            int remainingSpace = 8 - basFile.Entries[i].SubEntries[a].Name.Count();
                            for(int z = 0; z < remainingSpace; z++)
                            {
                                bytes.Add(0);
                            }

                            //Data
                            bytes.AddRange(BitConverter_Ex.GetBytes_Bool32(basFile.Entries[i].SubEntries[a].I_08));
                            bytes.AddRange(BitConverter.GetBytes((int)basFile.Entries[i].SubEntries[a].I_12));
                            bytes.AddRange(BitConverter.GetBytes((int)basFile.Entries[i].SubEntries[a].I_16));
                            bytes.AddRange(BitConverter.GetBytes((int)basFile.Entries[i].SubEntries[a].I_20));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_24));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_28));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_32));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_36));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_40));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_44));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_48));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_52));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_56));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_60));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].I_64));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].F_68));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].F_72));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].F_76));
                            bytes.AddRange(BitConverter.GetBytes(basFile.Entries[i].SubEntries[a].F_80));
                        }
                    }
                }


            }


        }

    }
}
