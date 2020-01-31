using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Xv2CoreLib
{
    public class Sorting
    {
        /// <summary>
        /// Returns -5000 if an index cannot be allocated.
        /// </summary>
        /// <param name="entries1"></param>
        /// <param name="entries2"></param>
        /// <param name="minIndex"></param>
        /// <param name="maxIndex"></param>
        /// <returns></returns>
        public static int GetUnusedIndex<T>(IEnumerable<T> entries1, IEnumerable<T> entries2, int minIndex = 0, int maxIndex = -1) where T : IInstallable
        {
            if (maxIndex == -1) maxIndex = UInt16.MaxValue - 1;
            List<int> UsedIndexes = new List<int>();

            //Create UsedIndexes
            if (entries1 != null)
            {
                foreach (var e in entries1)
                {
                    try
                    {
                        UsedIndexes.Add(e.SortID);
                    }
                    catch
                    {

                    }
                }
            }
            if (entries2 != null)
            {
                foreach (var e in entries2)
                {
                    try
                    {
                        UsedIndexes.Add(e.SortID);
                    }
                    catch
                    {

                    }
                }
            }

            //Create UnusedIndexes
            int idx = 0;
            while (true)
            {
                //If maxIndex has been reached
                if (idx > maxIndex && maxIndex != -1)
                {
                    return -5000;
                }

                //If the index is not used the idx is greater than minIndex
                if (!UsedIndexes.Contains(idx) && idx >= minIndex)
                {
                    return idx;
                }
                idx++;
                if (idx > int.MaxValue - 5000) return -1; //Safe-guard code (very unlikely case)
            }
        }


        public static List<int> GetUnusedIndexes(IEnumerable<ISortable_String> entries1, IEnumerable<ISortable_String> entries2, int count, int minIndex = 0, int maxIndex = -1)
        {
            List<int> UnusedIndexes = new List<int>();
            List<int> UsedIndexes = new List<int>();

            //Create UsedIndexes
            if (entries1 != null)
            {
                foreach (var e in entries1)
                {
                    try
                    {
                        UsedIndexes.Add(int.Parse(e.Index));
                    }
                    catch
                    {

                    }
                }
            }
            if(entries2 != null)
            {
                foreach (var e in entries2)
                {
                    try
                    {
                        UsedIndexes.Add(int.Parse(e.Index));
                    }
                    catch
                    {

                    }
                }
            }

            //Create UnusedIndexes
            int idx = 0;
            while (count > 0)
            {
                //If maxIndex has been reached
                if (idx > maxIndex && maxIndex != -1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        UnusedIndexes.Add(-1);
                    }
                    break;
                }

                //If the index is not used the idx is greater than minIndex
                if (!UsedIndexes.Contains(idx) && idx > minIndex + 1)
                {
                    UnusedIndexes.Add(idx);
                    count--;
                }
                idx++;
            }

            return UnusedIndexes;
        }

        public static List<int> GetUnusedIndexes(IEnumerable<ISortable_String> entries, int count, int minIndex = 0, int maxIndex = -1)
        {
            List<int> UnusedIndexes = new List<int>();
            List<int> UsedIndexes = new List<int>();

            //Create UsedIndexes
            if(entries != null)
            {
                foreach (var e in entries)
                {
                    try
                    {
                        UsedIndexes.Add(int.Parse(e.Index));
                    }
                    catch
                    {

                    }
                }
            }

            //Create UnusedIndexes
            int idx = 0;
            while(count > 0)
            {
                //If maxIndex has been reached
                if(idx > maxIndex && maxIndex != -1)
                {
                    for(int i = 0; i < count; i++)
                    {
                        UnusedIndexes.Add(-1);
                    }
                    break;
                }

                //If the index is not used the idx is greater than minIndex
                if (!UsedIndexes.Contains(idx) && idx > minIndex + 1)
                {
                    UnusedIndexes.Add(idx);
                    count--;
                }
                idx++;
            }

            return UnusedIndexes;
        }

        public static void CheckForDuplicates<T>(List<T> entries, string xml_section) where T : ISortable_Legacy, new()
        {
            if(entries != null)
            {
                for(int i = 0; i < entries.Count; i++)
                {
                    for(int a = 0; a < entries.Count(); a++)
                    {
                        if(a != i && entries[i].Index == entries[a].Index)
                        {
                            Console.WriteLine(String.Format("{0} (Index = {1}) has been declared multiple times. Duplicates are not allowed!\nDeserialization failed.", xml_section, entries[i].Index));
                            Utils.WaitForInputThenQuit();
                        }
                    }
                }
            }
        }

        public static void CheckForDuplicates_UShortIndex<T>(List<T> entries, string xml_section) where T : ISortable_UShort, new()
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    for (int a = 0; a < entries.Count(); a++)
                    {
                        if (a != i && entries[i].Index == entries[a].Index)
                        {
                            Console.WriteLine(String.Format("{0} (Index = {1}) has been declared multiple times. Duplicates are not allowed!\nDeserialization failed.", xml_section, entries[i].Index));
                            Utils.WaitForInputThenQuit();
                        }
                    }
                }
            }
        }

        public static void CheckForDuplicates_StringIndex<T>(List<T> entries, string xml_section) where T : ISortable_String, new()
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    for (int a = 0; a < entries.Count(); a++)
                    {
                        if (a != i && entries[i].Index == entries[a].Index)
                        {
                            Console.WriteLine(String.Format("{0} (Index = {1}) has been declared multiple times. Duplicates are not allowed!\nDeserialization failed.", xml_section, entries[i].Index));
                            Utils.WaitForInputThenQuit();
                        }
                    }
                }
            }
        }


        public static List<T> SortEntries_ShortIndex<T>(List<T> entries) where T : ISortable_Legacy, new()
        {
            if(entries != null)
            {
                List<int> UsedIds = new List<int>();
                List<T> SortedEntries = new List<T>();
                foreach (var e in entries)
                {
                    UsedIds.Add(e.Index);
                }
                UsedIds.Sort();
                for (int i = 0; i < UsedIds.Count(); i++)
                {
                    foreach (var e in entries)
                    {
                        if (e.Index == UsedIds[i])
                        {
                            SortedEntries.Add(e);
                            break;
                        }
                    }
                }
                return SortedEntries;
            }
            else
            {
                return null;
            }
            
        }

        public static List<T> SortEntries_UShortIndex<T>(List<T> entries) where T : ISortable_UShort, new()
        {
            if (entries != null)
            {
                List<int> UsedIds = new List<int>();
                List<T> SortedEntries = new List<T>();
                foreach (var e in entries)
                {
                    UsedIds.Add(e.Index);
                }
                UsedIds.Sort();
                for (int i = 0; i < UsedIds.Count(); i++)
                {
                    foreach (var e in entries)
                    {
                        if (e.Index == UsedIds[i])
                        {
                            SortedEntries.Add(e);
                            break;
                        }
                    }
                }
                return SortedEntries;
            }
            else
            {
                return null;
            }

        }


        public static List<T> SortEntries_IntIndex<T>(List<T> entries) where T : ISortable_Int, new()
        {
            if (entries != null)
            {
                List<int> UsedIds = new List<int>();
                List<T> SortedEntries = new List<T>();
                foreach (var e in entries)
                {
                    UsedIds.Add(e.Index);
                }
                UsedIds.Sort();
                for (int i = 0; i < UsedIds.Count(); i++)
                {
                    foreach (var e in entries)
                    {
                        if (e.Index == UsedIds[i])
                        {
                            SortedEntries.Add(e);
                            break;
                        }
                    }
                }
                return SortedEntries;
            }
            else
            {
                return null;
            }

        }

        public static ObservableCollection<T> SortEntries_IntIndex<T>(ObservableCollection<T> entries) where T : ISortable_Int, new()
        {
            if (entries != null)
            {
                List<int> UsedIds = new List<int>();
                ObservableCollection<T> SortedEntries = new ObservableCollection<T>();
                foreach (var e in entries)
                {
                    UsedIds.Add(e.Index);
                }
                UsedIds.Sort();
                for (int i = 0; i < UsedIds.Count(); i++)
                {
                    foreach (var e in entries)
                    {
                        if (e.Index == UsedIds[i])
                        {
                            //MessageBox.Show(e.Index.ToString());
                            SortedEntries.Add(e);
                            break;
                        }
                    }
                }
                return SortedEntries;
            }
            else
            {
                return null;
            }

        }

        public static List<T> SortEntries_StringIndex<T>(List<T> entries) where T : ISortable_String, new()
        {
            if (entries != null)
            {
                List<int> UsedIds = new List<int>();
                List<T> SortedEntries = new List<T>();
                foreach (var e in entries)
                {
                    UsedIds.Add(int.Parse(e.Index));
                }
                UsedIds.Sort();
                for (int i = 0; i < UsedIds.Count(); i++)
                {
                    foreach (var e in entries)
                    {
                        if (int.Parse(e.Index) == UsedIds[i])
                        {
                            SortedEntries.Add(e);
                            break;
                        }
                    }
                }
                return SortedEntries;
            }
            else
            {
                return null;
            }

        }

        public static ObservableCollection<T> SortEntries_StringIndex<T>(ObservableCollection<T> entries) where T : ISortable_String, new()
        {
            if (entries != null)
            {
                List<int> UsedIds = new List<int>();
                ObservableCollection<T> SortedEntries = new ObservableCollection<T>();
                foreach (var e in entries)
                {
                    UsedIds.Add(int.Parse(e.Index));
                }
                UsedIds.Sort();
                for (int i = 0; i < UsedIds.Count(); i++)
                {
                    foreach (var e in entries)
                    {
                        if (int.Parse(e.Index) == UsedIds[i])
                        {
                            SortedEntries.Add(e);
                            break;
                        }
                    }
                }
                return SortedEntries;
            }
            else
            {
                return null;
            }

        }

        //NEW
        public static ObservableCollection<T> SortEntries<T>(ObservableCollection<T> entries) where T : IInstallable, new()
        {
            //ObservableCollect doesn't have a Sort method, so we cannot do a in-place sort and a new collection must be created.

            if (entries != null)
            {
                return new ObservableCollection<T>(SortEntries(entries.ToList()));
            }
            else
            {
                return null;
            }

        }

        public static List<T> SortEntries<T>(List<T> entries) where T : IInstallable, new()
        {
            entries.Sort((x, y) => x.SortID - y.SortID);
            return entries;
        }

    }

    public interface ISortable_Legacy
    {
        short Index { get; set; }
    }
    public interface ISortable_UShort
    {
        ushort Index { get; set; }
    }

    public interface ISortable_Int
    {
        int Index { get; set; }
    }

    public interface ISortable_String
    {
        string Index { get; set; }
    }
}
