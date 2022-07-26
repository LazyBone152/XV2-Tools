using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BCM
{
    public class Deserializer
    {
        BCM_File bcmFile { get; set; }
        public List<byte> bytes = new List<byte>() { 35, 66, 67, 77, 254, 255, 0, 0 };
        string saveLocation { get; set; }

        int TotalEntryCount = 0;

        //Loop
        List<PtrToWrite> PtrToWriteList = new List<PtrToWrite>();
        List<string> IndexList = new List<string>();


        public Deserializer (string location)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(BCM_File), YAXSerializationOptions.DontSerializeNullObjects);
            bcmFile = (BCM_File)serializer.DeserializeFromFile(location);
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            if (Validation())
            {
                WriteBinaryFile();
                File.WriteAllBytes(saveLocation, bytes.ToArray());
            }
            
        }

        public Deserializer(BCM_File _bcmFile, string _saveLocation)
        {
            bcmFile = _bcmFile;
            saveLocation = _saveLocation;
            if (Validation())
            {
                WriteBinaryFile();
                File.WriteAllBytes(saveLocation, bytes.ToArray());
            }
        }

        public Deserializer(BCM_File _bcmFile)
        {
            bcmFile = _bcmFile;
            if (Validation())
            {
                WriteBinaryFile();
            }
        }

        private bool Validation()
        {
            if(bcmFile.BCMEntries.Count() != 1)
            {
                Console.WriteLine("Incorrect hierarchy setup. There can only be 1 root BCMEntry!\nDeserialization failed.");
                Console.ReadLine();
                return false;
            }

            if(bcmFile.BCMEntries[0].LoopAsChild != null)
            {
                Console.WriteLine("Child_GoTo_Idx is not valid for the root BCMEntry!\nDeserialization failed.");
                Console.ReadLine();
                return false;
            }

            if (bcmFile.BCMEntries[0].LoopAsSibling != null)
            {
                Console.WriteLine("Sibling_GoTo_Idx is not valid for the root BCMEntry!/nDeserialization failed.");
                Console.ReadLine();
                return false;
            }

            return true;
        }

        private void WriteBinaryFile()
        {
            //Header
            bytes.AddRange(BitConverter.GetBytes(0));
            bytes.AddRange(BitConverter.GetBytes(16));

            //BCM Entries
            SortEntry(bcmFile.BCMEntries, 0, true, 0, false);
            
            //Count
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(TotalEntryCount), 8);

            //GoTo loops
            FillInGoTos();

        }

        private void SortEntry(List<BCM_Entry> Entries, int parentOffset, bool useRoot, int rootOffset, bool rootLevel)
        {
            int previousSibling = 0;
            for(int i = 0; i < Entries.Count(); i++)
            {
                if(i > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), previousSibling);
                }
                previousSibling = bytes.Count() + 48;
                int ChilfOffsetToFill = bytes.Count() + 52;

                WriteBcmEntry(Entries[i], parentOffset, rootOffset);

                if(Entries[i].BCMEntries != null)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), ChilfOffsetToFill);
                    if(useRoot == true)
                    {
                        SortEntry(Entries[i].BCMEntries, 0, false, bytes.Count(), true);
                    }
                    else
                    {
                        SortEntry(Entries[i].BCMEntries, ChilfOffsetToFill - 52, false, rootOffset, false);
                    }
                }
                
                if(rootLevel== true)
                {
                    rootOffset = bytes.Count();
                }

            }
        }

        private void WriteBcmEntry(BCM_Entry bcmEntry, int parentOffset, int rootOffset)
        {
            ValidateEntry(bcmEntry);
            IndexList.Add(bcmEntry.Index);
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_00));
            bytes.AddRange(BitConverter.GetBytes((uint)bcmEntry.I_04));
            bytes.AddRange(BitConverter.GetBytes((uint)bcmEntry.I_08));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_12));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_16));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_20));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_22));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_24));
            bytes.AddRange(BitConverter.GetBytes((uint)bcmEntry.I_28));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_32));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_34));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_36));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_38));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_40));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_42));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_44));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_46));

            if (bcmEntry.LoopAsSibling != null)
            {
                PtrToWriteList.Add(new PtrToWrite()
                {
                    Idx = bcmEntry.LoopAsSibling,
                    OffsetToFill = bytes.Count()
                });
            }
            bytes.AddRange(BitConverter.GetBytes(0));

            if (bcmEntry.LoopAsChild != null)
            {
                PtrToWriteList.Add(new PtrToWrite()
                {
                    Idx = bcmEntry.LoopAsChild,
                    OffsetToFill = bytes.Count()
                });
            }
            bytes.AddRange(BitConverter.GetBytes(0));

            bytes.AddRange(BitConverter.GetBytes(parentOffset));
            bytes.AddRange(BitConverter.GetBytes(rootOffset));


            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_64));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_68));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_72));
            bytes.AddRange(BitConverter.GetBytes((int)bcmEntry.I_76));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_80));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_84));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_88));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_92));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.F_96));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_100));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_102));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_104));
            bytes.AddRange(BitConverter.GetBytes(bcmEntry.I_108));

            

            TotalEntryCount++;
        }

        private void FillInGoTos()
        {
            for(int i = 0; i < PtrToWriteList.Count(); i++)
            {
                if(IndexList.IndexOf(PtrToWriteList[i].Idx) == -1)
                {
                    Console.WriteLine(String.Format("Cannot complete GoTo loop. Idx {0} does not exist in the BCM file.", PtrToWriteList[i].Idx));
                    Utils.WaitForInputThenQuit();
                }

                int pointer = (IndexList.IndexOf(PtrToWriteList[i].Idx) * 112) + 16;
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(pointer), PtrToWriteList[i].OffsetToFill);

            }
        }

        private int GetBcmEntryOffset (int index)
        {

            if(index == -1)
            {
                return 0;
            }

            index = index * 112;
            return index + 16;

        }

        private void ValidateEntry(BCM_Entry entry)
        {
            //Checks for children if it has a Child GoTo
            if (entry.LoopAsChild != null && entry.BCMEntries != null)
            {
                throw new InvalidDataException(String.Format("Invalid Loop_As_Child tag on Idx {0}.\nCannot set the tag (Loop_As_Child=\"{1}\") as the BCM Entry at Idx {0} has actual child entries.", entry.Index, entry.LoopAsChild));
            }

            if (entry.BCMEntries != null)
            {
                bool siblingLoop = false;
                string idx = null;
                string loopIdx = null;

                foreach (var child in entry.BCMEntries)
                {
                    if (siblingLoop)
                    {
                        throw new InvalidDataException(String.Format("Invalid Loop_As_Sibling tag on Idx {0}.\nCannot set the tag (Sibling_GoTo_Idx=\"{1}\") as the BCM Entry at Idx {0} has an actual sibling.", idx, loopIdx));
                    }

                    if (child.LoopAsSibling != null)
                    {
                        siblingLoop = true;
                        idx = child.Index;
                        loopIdx = child.LoopAsSibling;
                    }
                }
            }
        }


        //Helper

        private struct PtrToWrite
        {
            public string Idx { get; set; }
            public int OffsetToFill { get; set; }
        }

    }
}
