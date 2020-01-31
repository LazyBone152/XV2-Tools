using System.Collections.Generic;

namespace Xv2CoreLib.Resource.UndoRedo
{
    /// <summary>
    /// Undo/Redo functionality for adding an object from an IList.
    /// </summary>
    class UndoableListAdd<T> : IUndoRedo where T : class
    {
        public string Message { get; private set; }

        private IList<T> list;
        private T obj;
        private int idx;

        public UndoableListAdd(IList<T> _list, T _obj, string message)
        {
            list = _list;
            obj = _obj;
            Message = message;
            idx = list.IndexOf(obj);
        }

        public void Undo()
        {
            list.Remove(obj);
        }

        public void Redo()
        {
            //Add obj at idx
            if (!list.Contains(obj))
            {
                if (idx >= 0 && idx <= list.Count)
                    list.Insert(idx, obj);
                else
                    list.Add(obj);
            }
        }
    }
}
