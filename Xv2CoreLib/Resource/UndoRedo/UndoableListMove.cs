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
        private bool isAsync = false;

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
            isAsync = true;
        }

        public void Undo()
        {
            if (isAsync)
            {
                if (newIdx >= 0 && oldIdx >= 0 && newIdx <= asyncObservableList.Count - 1 && oldIdx <= asyncObservableList.Count - 1)
                    asyncObservableList.Move(newIdx, oldIdx);
            }
            else
            {
                if (newIdx >= 0 && oldIdx >= 0 && newIdx <= observableList.Count - 1 && oldIdx <= observableList.Count - 1)
                    observableList.Move(newIdx, oldIdx);
            }
        }

        public void Redo()
        {
            if (isAsync)
            {
                if (newIdx >= 0 && oldIdx >= 0 && newIdx <= asyncObservableList.Count - 1 && oldIdx <= asyncObservableList.Count - 1)
                    asyncObservableList.Move(oldIdx, newIdx);
            }
            else
            {
                if (newIdx >= 0 && oldIdx >= 0 && newIdx <= observableList.Count - 1 && oldIdx <= observableList.Count - 1)
                    observableList.Move(oldIdx, newIdx);
            }
        }
    }
}
