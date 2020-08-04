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
        
        //NEW
        public static ObservableCollection<T> SortEntries<T>(ObservableCollection<T> entries) where T : IInstallable, new()
        {
            //ObservableCollect doesn't have a Sort method, so we need to do our own (bad) one here.

            if (entries != null)
            {
                var sortedEntries = SortEntries(entries.ToList());

                //To preserve the original ObservableCollection object, copy the sorted entries back into it rather than creating a new one:
                for (int i = 0; i < entries.Count; i++)
                {
                    entries[i] = sortedEntries[i];
                }
            }

            return entries;

        }

        public static List<T> SortEntries<T>(List<T> entries) where T : IInstallable, new()
        {
            entries.Sort((x, y) => x.SortID - y.SortID);
            return entries;
        }

    }
    
}
