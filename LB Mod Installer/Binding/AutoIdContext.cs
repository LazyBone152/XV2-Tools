using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib;

namespace LB_Mod_Installer.Binding
{
    public class AutoIdContext
    {
        public IEnumerable<IInstallable> Key { get; set; }
        public List<int> AssignedIds { get; private set; } = new List<int>();
        private int MinId = 0;

        public AutoIdContext(IEnumerable<IInstallable> Key)
        {
            this.Key = Key;

            foreach(var key in Key)
            {
                AssignedIds.Add(key.SortID);
            }

            AssignedIds.Sort();

            //If all IDs are sequential, then we can simply reduce AssignedIds to a single integer (better performance)
            bool isSequential = true;

            foreach(var id in AssignedIds)
            {
                if(id != MinId)
                {
                    isSequential = false;
                    break;
                }

                MinId++;
            }

            if (isSequential)
            {
                AssignedIds.Clear();
            }
            else
            {
                AssignedIds.RemoveAll(x => x < MinId);
            }

            MinId++;
        }

        public void AddId(int id)
        {
            if (!HasId(id))
            {
                AssignedIds.Add(id);

                if (id == MinId)
                    MinId++;
            }
        }

        public bool HasId(int id)
        {
            if (id < MinId)
                return true;

            return AssignedIds.Contains(id);
        }
        
        public void MergeContext(AutoIdContext context)
        {
            foreach(var keyEntry in context.Key)
            {
                if(keyEntry is IInstallable entry)
                {
                    AddId(entry.SortID);
                }
            }

            foreach(var id in context.AssignedIds)
            {
                AddId(id);
            }
        }
   
        public int GetMaxUsedID()
        {
            int id = Key.Count() > 0 ? Key.Max(x => x.SortID) : 0;
            int assignedMaxId = AssignedIds.Count() > 0 ? AssignedIds.Max() : 0;

            return id > assignedMaxId ? id : assignedMaxId;
        }

    }


}
