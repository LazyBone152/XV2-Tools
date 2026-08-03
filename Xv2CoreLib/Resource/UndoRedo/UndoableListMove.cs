using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public class UndoableListMove<T> : IUndoRedo
    {
        public string Message { get; set; }
        public bool doLast { get; set; }

        private int oldIdx;
        private int newIdx;
        private ObservableCollection<T> observableList;
        private AsyncObservableCollection<T> asyncObservableList;
        private IList<T> plainList;

        public UndoableListMove(ObservableCollection<T> _list, int _oldIdx, int _newIdx, string message = null)
        {
            oldIdx = _oldIdx;
            newIdx = _newIdx;
            observableList = _list;
            Message = message;
        }

        public UndoableListMove(AsyncObservableCollection<T> _list, int _oldIdx, int _newIdx, string message = null)
        {
            oldIdx = _oldIdx;
            newIdx = _newIdx;
            asyncObservableList = _list;
            Message = message;
        }

        // For plain lists that have no Move method. Like the other constructors, this does not mutate the list:
        // the caller performs the move, then records this.
        public UndoableListMove(IList<T> _list, int _oldIdx, int _newIdx, string message = null)
        {
            oldIdx = _oldIdx;
            newIdx = _newIdx;
            plainList = _list;
            Message = message;
        }

        public void Undo()
        {
            Move(newIdx, oldIdx);
        }

        public void Redo()
        {
            Move(oldIdx, newIdx);
        }

        private void Move(int from, int to)
        {
            if (asyncObservableList != null)
            {
                if (IsInRange(from, to, asyncObservableList.Count))
                    asyncObservableList.Move(from, to);
            }
            else if (plainList != null)
            {
                if (IsInRange(from, to, plainList.Count))
                {
                    T item = plainList[from];
                    plainList.RemoveAt(from);
                    plainList.Insert(to, item);
                }
            }
            else
            {
                if (IsInRange(from, to, observableList.Count))
                    observableList.Move(from, to);
            }
        }

        private static bool IsInRange(int from, int to, int count)
        {
            return from >= 0 && to >= 0 && from < count && to < count;
        }
    }
}
