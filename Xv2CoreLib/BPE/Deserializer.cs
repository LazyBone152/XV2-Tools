using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BPE
{
    public class Deserializer
    {
        string saveLocation;
        BPE_File bpeFile;
        public List<byte> bytes = new List<byte>() { 35, 66, 80, 69, 254, 255, 24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        //Offsets/Counts
        int EntryCount { get; set; }
        List<int> MainEntryOffsets = new List<int>();

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(BPE_File), YAXSerializationOptions.DontSerializeNullObjects);
            bpeFile = (BPE_File)serializer.DeserializeFromFile(location);
            bpeFile.SortEntries();
            EntryCount = (bpeFile.Entries != null) ? int.Parse(bpeFile.Entries[bpeFile.Entries.Count() - 1].Index) + 1 : 0;
            WriteBpe();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(BPE_File _bpeFile, string location)
        {
            saveLocation = location;
            bpeFile = _bpeFile;
            bpeFile.SortEntries();
            EntryCount = (bpeFile.Entries != null) ? int.Parse(bpeFile.Entries[bpeFile.Entries.Count() - 1].Index) + 1 : 0;
            WriteBpe();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(BPE_File _bpeFile)
        {
            bpeFile = _bpeFile;
            bpeFile.SortEntries();
            EntryCount = (bpeFile.Entries != null) ? int.Parse(bpeFile.Entries[bpeFile.Entries.Count() - 1].Index) + 1 : 0;
            WriteBpe();
        }

        private void WriteBpe()
        {
            bytes.AddRange(BitConverter.GetBytes((short)EntryCount));
            bytes.AddRange(BitConverter.GetBytes(24));

            //Pointer List
            for(int a = 0; a < EntryCount; a++)
            {
                for (int i = 0; i < bpeFile.Entries.Count(); i++)
                {
                    if (a == int.Parse(bpeFile.Entries[i].Index))
                    {
                        MainEntryOffsets.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);
                        break;
                    }
                    if (i == bpeFile.Entries.Count() - 1)
                    {
                        bytes.AddRange(new byte[4]);
                        break;
                    }

                }
            }

            //Entries
            for(int i = 0; i < bpeFile.Entries.Count(); i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), MainEntryOffsets[i]);

                //Entry Data
                bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].I_06));
                bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].I_08));
                bytes.AddRange(BitConverter.GetBytes((short)bpeFile.Entries[i].SubEntries.Count()));
                bytes.AddRange(BitConverter.GetBytes(16));

                List<int> TypeOffsets = new List<int>();
                //SubEntries
                for(int a = 0; a < bpeFile.Entries[i].SubEntries.Count(); a++)
                {
                    TypeValidation(bpeFile.Entries[i].SubEntries[a], bpeFile.Entries[i]);
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].GetBpeType()));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].I_04));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].I_06));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].I_08));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].I_10));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].F_12));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].F_16));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].F_20));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].F_24));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].F_28));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].F_32));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].I_36));
                    bytes.AddRange(BitConverter.GetBytes(bpeFile.Entries[i].SubEntries[a].I_40));
                    bytes.AddRange(BitConverter.GetBytes((short)GetTypeCount(bpeFile.Entries[i].SubEntries[a].GetBpeType(), bpeFile.Entries[i].SubEntries[a])));
                    TypeOffsets.Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                }

                for(int a = 0; a < bpeFile.Entries[i].SubEntries.Count(); a++)
                {
                    switch (bpeFile.Entries[i].SubEntries[a].GetBpeType())
                    {
                        case 0:
                            WriteType0(bpeFile.Entries[i].SubEntries[a].Type0, TypeOffsets[a]);
                            break;
                        case 1:
                            WriteType1(bpeFile.Entries[i].SubEntries[a].Type1, TypeOffsets[a]);
                            break;
                        case 2:
                            WriteType2(bpeFile.Entries[i].SubEntries[a].Type2, TypeOffsets[a]);
                            break;
                        case 3:
                            WriteType3(bpeFile.Entries[i].SubEntries[a].Type3, TypeOffsets[a]);
                            break;
                        case 4:
                            WriteType4(bpeFile.Entries[i].SubEntries[a].Type4, TypeOffsets[a]);
                            break;
                        case 5:
                            WriteType5(bpeFile.Entries[i].SubEntries[a].Type5, TypeOffsets[a]);
                            break;
                        case 6:
                            WriteType6(bpeFile.Entries[i].SubEntries[a].Type6, TypeOffsets[a]);
                            break;
                        case 7:
                            WriteType7(bpeFile.Entries[i].SubEntries[a].Type7, TypeOffsets[a]);
                            break;
                        case 8:
                            WriteType8(bpeFile.Entries[i].SubEntries[a].Type8, TypeOffsets[a]);
                            break;
                        case 9:
                            WriteType9(bpeFile.Entries[i].SubEntries[a].Type9, TypeOffsets[a]);
                            break;
                    }
                }

            }

        }



        //Type Writers
        private void WriteType0 (List<BPE_Type0> type, int offsetToFill)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 44), offsetToFill);

            for(int i = 0; i < type.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_04));
            }

        }

        private void WriteType1(List<BPE_Type1> type, int offsetToFill)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 44), offsetToFill);

            for (int i = 0; i < type.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_04));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
            }

        }

        private void WriteType2(List<BPE_Type2> type, int offsetToFill)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 44), offsetToFill);

            for (int i = 0; i < type.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_24));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_28));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_32));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_36));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_40));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_44));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_48));
            }

        }

        private void WriteType3(List<BPE_Type3> type, int offsetToFill)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 44), offsetToFill);

            for (int i = 0; i < type.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
            }

        }

        private void WriteType4(List<BPE_Type4> type, int offsetToFill)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 44), offsetToFill);

            for (int i = 0; i < type.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
            }

        }

        private void WriteType5(List<BPE_Type5> type, int offsetToFill)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 44), offsetToFill);

            for (int i = 0; i < type.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_04));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_24));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_28));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_32));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_36));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_40));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_44));
            }

        }

        private void WriteType6(List<BPE_Type6> type, int offsetToFill)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 44), offsetToFill);

            for (int i = 0; i < type.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_04));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_24));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_28));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_32));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_36));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_40));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_44));
            }

        }

        private void WriteType7(List<BPE_Type7> type, int offsetToFill)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 44), offsetToFill);

            for (int i = 0; i < type.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_04));
            }

        }

        private void WriteType8(List<BPE_Type8> type, int offsetToFill)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 44), offsetToFill);

            for (int i = 0; i < type.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(type[i].I_08));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_24));
            }

        }

        private void WriteType9(List<BPE_Type9> type, int offsetToFill)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 44), offsetToFill);

            for (int i = 0; i < type.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_04));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_24));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_28));
                bytes.AddRange(BitConverter.GetBytes(type[i].F_32));
            }

        }

        //Utility
        private int GetTypeCount(int type, BPE_SubEntry subEntry)
        {
            switch (type)
            {
                case 0:
                    return subEntry.Type0.Count();
                case 1:
                    return subEntry.Type1.Count();
                case 2:
                    return subEntry.Type2.Count();
                case 3:
                    return subEntry.Type3.Count();
                case 4:
                    return subEntry.Type4.Count();
                case 5:
                    return subEntry.Type5.Count();
                case 6:
                    return subEntry.Type6.Count();
                case 7:
                    return subEntry.Type7.Count();
                case 8:
                    return subEntry.Type8.Count();
                case 9:
                    return subEntry.Type9.Count();
                default:
                    return 0;
            }
        }

        private void TypeValidation(BPE_SubEntry subEntry, BPE_Entry entry)
        {
            switch (subEntry.GetBpeType())
            {
                case 0:
                    if(subEntry.Type0 == null)
                    {
                        TypeDoesntExist(entry, subEntry.I_00);
                    }
                    break;
                case 1:
                    if (subEntry.Type1 == null)
                    {
                        TypeDoesntExist(entry, subEntry.I_00);
                    }
                    break;
                case 2:
                    if (subEntry.Type2 == null)
                    {
                        TypeDoesntExist(entry, subEntry.I_00);
                    }
                    break;
                case 3:
                    if (subEntry.Type3 == null)
                    {
                        TypeDoesntExist(entry, subEntry.I_00);
                    }
                    break;
                case 4:
                    if (subEntry.Type4 == null)
                    {
                        TypeDoesntExist(entry, subEntry.I_00);
                    }
                    break;
                case 5:
                    if (subEntry.Type5 == null)
                    {
                        TypeDoesntExist(entry, subEntry.I_00);
                    }
                    break;
                case 6:
                    if (subEntry.Type6 == null)
                    {
                        TypeDoesntExist(entry, subEntry.I_00);
                    }
                    break;
                case 7:
                    if (subEntry.Type7 == null)
                    {
                        TypeDoesntExist(entry, subEntry.I_00);
                    }
                    break;
                case 8:
                    if (subEntry.Type8 == null)
                    {
                        TypeDoesntExist(entry, subEntry.I_00);
                    }
                    break;
                case 9:
                    if (subEntry.Type9 == null)
                    {
                        TypeDoesntExist(entry, subEntry.I_00);
                    }
                    break;
                default:
                    Console.WriteLine(String.Format("{0} is not a valid BPE_Type (BPE_Entry: Index = {1})", subEntry.I_00, entry.Index));
                    Utils.WaitForInputThenQuit();
                    break;

            }
        }

        private void TypeDoesntExist(BPE_Entry entry, string type)
        {
            Console.WriteLine(String.Format("BPE Type{0} was not found on BPE_Entry (index = {1})", type, entry.Index));
            Utils.WaitForInputThenQuit();
        }
    }
}
