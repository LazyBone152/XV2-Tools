using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public class UndoableArrayChange<T> : IUndoRedo
    {
        public string Message { get; set; }
        public bool doLast { get; set; }

        private int idx;
        private T[] list;
        private T originalState;
        private T changedState;

        public UndoableArrayChange(T[] _list, int _idx, T _originalState, T _changedState, string message = null)
        {
            idx = _idx;
            list = _list;
            Message = message;
            originalState = _originalState;
            changedState = _changedState;
        }

        public void Undo()
        {
            if (idx >= 0 && idx <= list.Length - 1)
                list[idx] = originalState;
        }

        public void Redo()
        {
            if (idx >= 0 && idx <= list.Length - 1)
                list[idx] = changedState;
        }
    }
}
