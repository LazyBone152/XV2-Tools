using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public class UndoableListInsert<T> : IUndoRedo
    {
        public string Message { get; set; }
        public bool doLast { get; set; }

        private int idx;
        private IList<T> list;
        private T insertedItem;

        public UndoableListInsert(IList<T> _list, int _idx, T _insertedItem, string message = null)
        {
            idx = _idx;
            list = _list;
            Message = message;
            insertedItem = _insertedItem;
        }

        public void Undo()
        {
            if (idx >= 0 && idx <= list.Count - 1)
                list.RemoveAt(idx);
        }

        public void Redo()
        {
            if (idx >= 0 && idx <= list.Count)
                list.Insert(idx, insertedItem);
        }
    }
}

