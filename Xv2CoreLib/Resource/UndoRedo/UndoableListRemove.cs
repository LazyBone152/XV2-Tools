using System;
using System.Collections.Generic;

namespace Xv2CoreLib.Resource.UndoRedo
{
    /// <summary>
    /// Undo/Redo functionality for removing an object from an IList.
    /// </summary>
    public class UndoableListRemove<T> : IUndoRedo
    {
        public bool doLast { get; set; }
        public string Message { get; set; }

        private IList<T> list;
        private T obj;
        private int idx;

        public UndoableListRemove(IList<T> _list, T _obj, int _idx, string message = "")
        {
            list = _list;
            obj = _obj;
            Message = message;
            idx = _idx;
        }

        public UndoableListRemove(IList<T> _list, T _obj, string message = "")
        {
            list = _list;
            obj = _obj;
            Message = message;
            idx = _list.IndexOf(_obj);
        }

        public void Undo()
        {
            //Add obj at idx
            if (!list.Contains(obj))
            {
                if(idx != -1 && idx < list.Count)
                    list.Insert(idx, obj);
                else
                    list.Add(obj);
            }
        }

        public void Redo()
        {
            list.Remove(obj);
        }
    }
}
