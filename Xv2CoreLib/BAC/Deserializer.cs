using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BAC
{
    public class Deserializer
    {
        string saveLocation;
        BAC_File bacFile;
        public List<byte> bytes { get; private set; } = new List<byte>() { 35, 66, 65, 67, 254, 255, 0, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(BAC_File), YAXSerializationOptions.DontSerializeNullObjects);
            bacFile = (BAC_File)serializer.DeserializeFromFile(location);

            WriteBac();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(BAC_File _bacFile, string location)
        {
            saveLocation = location;
            bacFile = _bacFile;
            WriteBac();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(BAC_File _bacFile)
        {
            bacFile = _bacFile;
            WriteBac();
        }


        private void WriteBac()
        {
            bacFile.SortEntries();

            int count = (bacFile.BacEntries != null) ? bacFile.BacEntries.Count() : 0;
            List<int> BacEntryOffsets = new List<int>();
            List<List<int>> TypeOffsets = new List<List<int>>();
            
            //Header
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(96));
            bytes.AddRange(BitConverter_Ex.GetBytes(bacFile.I_20));
            bytes.AddRange(BitConverter_Ex.GetBytes(bacFile.F_32));
            bytes.AddRange(BitConverter_Ex.GetBytes(bacFile.I_80));

            //Bac_Entries
            for(int i = 0; i < count; i++)
            {
                if (bacFile.BacEntries[i].TypeDummy == null)
                {
                    bacFile.BacEntries[i].TypeDummy = new List<int>();
                }

                bytes.AddRange(BitConverter.GetBytes((uint)bacFile.BacEntries[i].Flag));
                bytes.AddRange(BitConverter.GetBytes((short)GetSubEntryCount(bacFile.BacEntries[i])));
                bytes.AddRange(BitConverter.GetBytes((short)0));
                BacEntryOffsets.Add(bytes.Count());
                bytes.AddRange(new byte[8]);

                
            }

            //Bac_SubEntries
            for(int i = 0; i < count; i++)
            {
                TypeOffsets.Add(new List<int>());
                int subEntryCount = GetSubEntryCount(bacFile.BacEntries[i]);
                int[] types = GetSubEntryTypes(bacFile.BacEntries[i]);

                if (subEntryCount > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), BacEntryOffsets[i]);

                    for(int a = 0; a < subEntryCount; a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes((short)types[a]));
                        bytes.AddRange(BitConverter.GetBytes((short)GetTypeCount(bacFile.BacEntries[i], types[a])));
                        bytes.AddRange(new byte[4]);
                        TypeOffsets[i].Add(bytes.Count());
                        bytes.AddRange(new byte[8]);
                    }

                }
            }

            //Bac Types
            for(int i = 0; i < count; i++)
            {
                int subEntryCount = GetSubEntryCount(bacFile.BacEntries[i]);
                int[] types = GetSubEntryTypes(bacFile.BacEntries[i]);

                for(int a = 0; a < subEntryCount; a++)
                {
                    if(bacFile.BacEntries[i].TypeDummy.IndexOf(types[a]) == -1)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), TypeOffsets[i][a]);

                        switch (types[a])
                        {
                            case 0:
                                bytes.AddRange(BAC_Type0.Write(bacFile.BacEntries[i].Type0));
                                break;
                            case 1:
                                bytes.AddRange(BAC_Type1.Write(bacFile.BacEntries[i].Type1));
                                break;
                            case 2:
                                bytes.AddRange(BAC_Type2.Write(bacFile.BacEntries[i].Type2));
                                break;
                            case 3:
                                bytes.AddRange(BAC_Type3.Write(bacFile.BacEntries[i].Type3));
                                break;
                            case 4:
                                bytes.AddRange(BAC_Type4.Write(bacFile.BacEntries[i].Type4));
                                break;
                            case 5:
                                bytes.AddRange(BAC_Type5.Write(bacFile.BacEntries[i].Type5));
                                break;
                            case 6:
                                bytes.AddRange(BAC_Type6.Write(bacFile.BacEntries[i].Type6));
                                break;
                            case 7:
                                bytes.AddRange(BAC_Type7.Write(bacFile.BacEntries[i].Type7));
                                break;
                            case 8:
                                bytes.AddRange(BAC_Type8.Write(bacFile.BacEntries[i].Type8));
                                break;
                            case 9:
                                bytes.AddRange(BAC_Type9.Write(bacFile.BacEntries[i].Type9));
                                break;
                            case 10:
                                bytes.AddRange(BAC_Type10.Write(bacFile.BacEntries[i].Type10));
                                break;
                            case 11:
                                bytes.AddRange(BAC_Type11.Write(bacFile.BacEntries[i].Type11));
                                break;
                            case 12:
                                bytes.AddRange(BAC_Type12.Write(bacFile.BacEntries[i].Type12));
                                break;
                            case 13:
                                bytes.AddRange(BAC_Type13.Write(bacFile.BacEntries[i].Type13));
                                break;
                            case 14:
                                bytes.AddRange(BAC_Type14.Write(bacFile.BacEntries[i].Type14));
                                break;
                            case 15:
                                bytes.AddRange(BAC_Type15.Write(bacFile.BacEntries[i].Type15));
                                break;
                            case 16:
                                bytes.AddRange(BAC_Type16.Write(bacFile.BacEntries[i].Type16));
                                break;
                            case 17:
                                bytes.AddRange(BAC_Type17.Write(bacFile.BacEntries[i].Type17));
                                break;
                            case 18:
                                bytes.AddRange(BAC_Type18.Write(bacFile.BacEntries[i].Type18));
                                break;
                            case 19:
                                bytes.AddRange(BAC_Type19.Write(bacFile.BacEntries[i].Type19));
                                break;
                            case 20:
                                bytes.AddRange(BAC_Type20.Write(bacFile.BacEntries[i].Type20));
                                break;
                            case 21:
                                bytes.AddRange(BAC_Type21.Write(bacFile.BacEntries[i].Type21));
                                break;
                            case 22:
                                bytes.AddRange(BAC_Type22.Write(bacFile.BacEntries[i].Type22));
                                break;
                            case 23:
                                bytes.AddRange(BAC_Type23.Write(bacFile.BacEntries[i].Type23));
                                break;
                            case 24:
                                bytes.AddRange(BAC_Type24.Write(bacFile.BacEntries[i].Type24));
                                break;
                            case 25:
                                bytes.AddRange(BAC_Type25.Write(bacFile.BacEntries[i].Type25));
                                break;
                            case 26:
                                bytes.AddRange(BAC_Type26.Write(bacFile.BacEntries[i].Type26));
                                break;
                        }
                    }
                }

            }
            

        }


        //Utility

        private int GetSubEntryCount(BAC_Entry bacEntry)
        {
            int count = 0;
            if(bacEntry.Type0 != null)
            {
                bacEntry.TypeDummy.Remove(0);
                count++;
            }
            if (bacEntry.Type1 != null)
            {
                bacEntry.TypeDummy.Remove(1);
                count++;
            }
            if (bacEntry.Type2 != null)
            {
                bacEntry.TypeDummy.Remove(2);
                count++;
            }
            if (bacEntry.Type3 != null)
            {
                bacEntry.TypeDummy.Remove(3);
                count++;
            }
            if (bacEntry.Type4 != null)
            {
                bacEntry.TypeDummy.Remove(4);
                count++;
            }
            if (bacEntry.Type5 != null)
            {
                bacEntry.TypeDummy.Remove(5);
                count++;
            }
            if (bacEntry.Type6 != null)
            {
                bacEntry.TypeDummy.Remove(6);
                count++;
            }
            if (bacEntry.Type7 != null)
            {
                bacEntry.TypeDummy.Remove(7);
                count++;
            }
            if (bacEntry.Type8 != null)
            {
                bacEntry.TypeDummy.Remove(8);
                count++;
            }
            if (bacEntry.Type9 != null)
            {
                bacEntry.TypeDummy.Remove(9);
                count++;
            }
            if (bacEntry.Type10 != null)
            {
                bacEntry.TypeDummy.Remove(10);
                count++;
            }
            if (bacEntry.Type11 != null)
            {
                bacEntry.TypeDummy.Remove(11);
                count++;
            }
            if (bacEntry.Type12 != null)
            {
                bacEntry.TypeDummy.Remove(12);
                count++;
            }
            if (bacEntry.Type13 != null)
            {
                bacEntry.TypeDummy.Remove(13);
                count++;
            }
            if (bacEntry.Type14 != null)
            {
                bacEntry.TypeDummy.Remove(14);
                count++;
            }
            if (bacEntry.Type15 != null)
            {
                bacEntry.TypeDummy.Remove(15);
                count++;
            }
            if (bacEntry.Type16 != null)
            {
                bacEntry.TypeDummy.Remove(16);
                count++;
            }
            if (bacEntry.Type17 != null)
            {
                bacEntry.TypeDummy.Remove(17);
                count++;
            }
            if (bacEntry.Type18 != null)
            {
                bacEntry.TypeDummy.Remove(18);
                count++;
            }
            if (bacEntry.Type19 != null)
            {
                bacEntry.TypeDummy.Remove(19);
                count++;
            }
            if (bacEntry.Type20 != null)
            {
                bacEntry.TypeDummy.Remove(20);
                count++;
            }
            if (bacEntry.Type21 != null)
            {
                bacEntry.TypeDummy.Remove(21);
                count++;
            }
            if (bacEntry.Type22 != null)
            {
                bacEntry.TypeDummy.Remove(22);
                count++;
            }
            if (bacEntry.Type23 != null)
            {
                bacEntry.TypeDummy.Remove(23);
                count++;
            }
            if (bacEntry.Type24 != null)
            {
                bacEntry.TypeDummy.Remove(24);
                count++;
            }
            if (bacEntry.Type25 != null)
            {
                bacEntry.TypeDummy.Remove(25);
                count++;
            }
            if (bacEntry.Type26 != null)
            {
                bacEntry.TypeDummy.Remove(26);
                count++;
            }

            if (bacEntry.TypeDummy != null)
            {
                count += bacEntry.TypeDummy.Count();
            }

            return count;
        }

        private int[] GetSubEntryTypes(BAC_Entry bacEntry)
        {
            List<int> types = new List<int>();

            if(bacEntry.Type0 != null)
            {
                types.Add(0);
            }
            if (bacEntry.Type1 != null)
            {
                types.Add(1);
            }
            if (bacEntry.Type2 != null)
            {
                types.Add(2);
            }
            if (bacEntry.Type3 != null)
            {
                types.Add(3);
            }
            if (bacEntry.Type4 != null)
            {
                types.Add(4);
            }
            if (bacEntry.Type5 != null)
            {
                types.Add(5);
            }
            if (bacEntry.Type6 != null)
            {
                types.Add(6);
            }
            if (bacEntry.Type7 != null)
            {
                types.Add(7);
            }
            if (bacEntry.Type8 != null)
            {
                types.Add(8);
            }
            if (bacEntry.Type9 != null)
            {
                types.Add(9);
            }
            if (bacEntry.Type10 != null)
            {
                types.Add(10);
            }
            if (bacEntry.Type11 != null)
            {
                types.Add(11);
            }
            if (bacEntry.Type12 != null)
            {
                types.Add(12);
            }
            if (bacEntry.Type13 != null)
            {
                types.Add(13);
            }
            if (bacEntry.Type14 != null)
            {
                types.Add(14);
            }
            if (bacEntry.Type15 != null)
            {
                types.Add(15);
            }
            if (bacEntry.Type16 != null)
            {
                types.Add(16);
            }
            if (bacEntry.Type17 != null)
            {
                types.Add(17);
            }
            if (bacEntry.Type18 != null)
            {
                types.Add(18);
            }
            if (bacEntry.Type19 != null)
            {
                types.Add(19);
            }
            if (bacEntry.Type20 != null)
            {
                types.Add(20);
            }
            if (bacEntry.Type21 != null)
            {
                types.Add(21);
            }
            if (bacEntry.Type22 != null)
            {
                types.Add(22);
            }
            if (bacEntry.Type23 != null)
            {
                types.Add(23);
            }
            if (bacEntry.Type24 != null)
            {
                types.Add(24);
            }
            if (bacEntry.Type25 != null)
            {
                types.Add(25);
            }
            if (bacEntry.Type26 != null)
            {
                types.Add(26);
            }
            if (bacEntry.TypeDummy != null)
            {
                foreach(var i in bacEntry.TypeDummy)
                {
                    if(types.IndexOf(i) == -1)
                    {
                        types.Add(i);
                    }
                }
            }

            types.Sort();

            return types.ToArray();
        }

        private int GetTypeCount(BAC_Entry bacEntry, int type)
        {
            if(bacEntry.TypeDummy.IndexOf(type) != -1)
            {
                return 0;
            }

            if(type == 0)
            {
                return (bacEntry.Type0 != null) ? bacEntry.Type0.Count() : 0;
            }
            else if (type == 1)
            {
                return (bacEntry.Type1 != null) ? bacEntry.Type1.Count() : 0;
            }
            else if (type == 2)
            {
                return (bacEntry.Type2 != null) ? bacEntry.Type2.Count() : 0;
            }
            else if (type == 3)
            {
                return (bacEntry.Type3 != null) ? bacEntry.Type3.Count() : 0;
            }
            else if (type == 4)
            {
                return (bacEntry.Type4 != null) ? bacEntry.Type4.Count() : 0;
            }
            else if (type == 5)
            {
                return (bacEntry.Type5 != null) ? bacEntry.Type5.Count() : 0;
            }
            else if (type == 6)
            {
                return (bacEntry.Type6 != null) ? bacEntry.Type6.Count() : 0;
            }
            else if (type == 7)
            {
                return (bacEntry.Type7 != null) ? bacEntry.Type7.Count() : 0;
            }
            else if (type == 8)
            {
                return (bacEntry.Type8 != null) ? bacEntry.Type8.Count() : 0;
            }
            else if (type == 9)
            {
                return (bacEntry.Type9 != null) ? bacEntry.Type9.Count() : 0;
            }
            else if (type == 10)
            {
                return (bacEntry.Type10 != null) ? bacEntry.Type10.Count() : 0;
            }
            else if (type == 11)
            {
                return (bacEntry.Type11 != null) ? bacEntry.Type11.Count() : 0;
            }
            else if (type == 12)
            {
                return (bacEntry.Type12 != null) ? bacEntry.Type12.Count() : 0;
            }
            else if (type == 13)
            {
                return (bacEntry.Type13 != null) ? bacEntry.Type13.Count() : 0;
            }
            else if (type == 14)
            {
                return (bacEntry.Type14 != null) ? bacEntry.Type14.Count() : 0;
            }
            else if (type == 15)
            {
                return (bacEntry.Type15 != null) ? bacEntry.Type15.Count() : 0;
            }
            else if (type == 16)
            {
                return (bacEntry.Type16 != null) ? bacEntry.Type16.Count() : 0;
            }
            else if (type == 17)
            {
                return (bacEntry.Type17 != null) ? bacEntry.Type17.Count() : 0;
            }
            else if (type == 18)
            {
                return (bacEntry.Type18 != null) ? bacEntry.Type18.Count() : 0;
            }
            else if (type == 19)
            {
                return (bacEntry.Type19 != null) ? bacEntry.Type19.Count() : 0;
            }
            else if (type == 20)
            {
                return (bacEntry.Type20 != null) ? bacEntry.Type20.Count() : 0;
            }
            else if (type == 21)
            {
                return (bacEntry.Type21 != null) ? bacEntry.Type21.Count() : 0;
            }
            else if (type == 22)
            {
                return (bacEntry.Type22 != null) ? bacEntry.Type22.Count() : 0;
            }
            else if (type == 23)
            {
                return (bacEntry.Type23 != null) ? bacEntry.Type23.Count() : 0;
            }
            else if (type == 24)
            {
                return (bacEntry.Type24 != null) ? bacEntry.Type24.Count() : 0;
            }
            else if (type == 25)
            {
                return (bacEntry.Type25 != null) ? bacEntry.Type25.Count() : 0;
            }
            else if (type == 26)
            {
                return (bacEntry.Type26 != null) ? bacEntry.Type26.Count() : 0;
            }
            else
            {
                return 0;
            }

        }

        private List<BAC_Type17> ValidateType17Size(List<BAC_Type17> type17)
        {
            //If one of the Type17 entries is of Full type, it will convert all of them to be Full.

            bool isFull = false;

            foreach(var e in type17)
            {
                if(e.F_20 != null)
                {
                    isFull = true;
                    break;
                }
            }

            if (isFull)
            {
                for (int i = 0; i < type17.Count(); i++)
                {
                    if (type17[i].F_20 == null)
                    {
                        type17[i].F_20 = new float[3];
                    }
                }
            }

            return type17;
        }

    }
}
