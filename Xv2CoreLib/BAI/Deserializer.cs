using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BAI
{
    public class Deserializer
    {
        string saveLocation;
        BAI_File baiFile;
        public List<byte> bytes = new List<byte>() { 35, 66, 65, 73, 254, 255, 3, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(BAI_File), YAXSerializationOptions.DontSerializeNullObjects);
            baiFile = (BAI_File)serializer.DeserializeFromFile(location);
            
            WriteBai();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(BAI_File _baiFile)
        {
            baiFile = _baiFile;
            WriteBai();
        }

        public Deserializer(BAI_File _baiFile, string path)
        {
            baiFile = _baiFile;
            WriteBai();
            File.WriteAllBytes(path, bytes.ToArray());
        }

        public void WriteBai()
        {
            List<int> EntryOffsets = new List<int>();

            int count = (baiFile.Entries != null) ? baiFile.Entries.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(16));

            if(baiFile.Entries != null)
            {
                for(int i = 0; i < baiFile.Entries.Count(); i++)
                {
                    int subEntryCount = (baiFile.Entries[i].SubEntries != null) ? baiFile.Entries[i].SubEntries.Count() : 0;
                    bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(subEntryCount));
                    EntryOffsets.Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                }

                for (int i = 0; i < baiFile.Entries.Count(); i++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), EntryOffsets[i]);
                    if (baiFile.Entries[i].SubEntries != null)
                    {
                        for(int a = 0; a < baiFile.Entries[i].SubEntries.Count(); a++)
                        {
                            //Name Str
                            if(baiFile.Entries[i].SubEntries[a].Name.Count() > 8)
                            {
                                Console.WriteLine(String.Format("The name \"{0}\" exceeds the maximum length of 8!", baiFile.Entries[i].SubEntries[a].Name));
                                Utils.WaitForInputThenQuit();
                            }
                            bytes.AddRange(Encoding.ASCII.GetBytes(baiFile.Entries[i].SubEntries[a].Name));
                            int remainingSpace = 8 - baiFile.Entries[i].SubEntries[a].Name.Count();
                            for(int z = 0; z < remainingSpace; z++)
                            {
                                bytes.Add(0);
                            }

                            //Data
                            bytes.AddRange(BitConverter_Ex.GetBytes_Bool32(baiFile.Entries[i].SubEntries[a].I_08));
                            bytes.AddRange(BitConverter.GetBytes((int)baiFile.Entries[i].SubEntries[a].I_12));
                            bytes.AddRange(BitConverter.GetBytes((int)baiFile.Entries[i].SubEntries[a].I_16));
                            bytes.AddRange(BitConverter.GetBytes((int)baiFile.Entries[i].SubEntries[a].I_20));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_24));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_28));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_32));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_36));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_40));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_44));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_48));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_52));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_56));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_60));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].I_64));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].F_68));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].F_72));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].F_76));
                            bytes.AddRange(BitConverter.GetBytes(baiFile.Entries[i].SubEntries[a].F_80));
                        }
                    }
                }


            }


        }

    }
}
