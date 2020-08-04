using System;
using System.Collections.Generic;

namespace Xv2CoreLib.Resource.UndoRedo
{
    /// <summary>
    /// Undo/redo functionality for an object state change in a IList.
    /// </summary>
    public class UndoableStateChange<T> : IUndoRedo
    {
        public string Message { get; set; }
        public bool doLast { get; set; }

        private int idx;
        private IList<T> list;
        private T originalState;
        private T changedState;

        public UndoableStateChange(IList<T> _list, int _idx, T _originalState, T _changedState, string message)
        {
            idx = _idx;
            list = _list;
            Message = message;
            originalState = _originalState;
            changedState = _changedState;
        }

        public void Undo()
        {
            if(idx >= 0 && idx <= list.Count - 1)
                list[idx] = originalState;
        }

        public void Redo()
        {
            if (idx >= 0 && idx <= list.Count - 1)
                list[idx] = changedState;
        }
    }
}
