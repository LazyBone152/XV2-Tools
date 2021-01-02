using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public class UndoableListMove<T> : IUndoRedo
    {
        public string Message { get; set; }
        public bool doLast { get; set; }

        private int oldIdx;
        private int newIdx;
        private ObservableCollection<T> observableList;

        public UndoableListMove(ObservableCollection<T> _list, int _oldIdx, int _newIdx, string message = null)
        {
            oldIdx = _oldIdx;
            newIdx = _newIdx;
            observableList = _list;
            Message = message;
        }


        public void Undo()
        {
            if (newIdx >= 0 && oldIdx >= 0 && newIdx <= observableList.Count - 1 && oldIdx <= observableList.Count - 1)
                observableList.Move(newIdx, oldIdx);
        }

        public void Redo()
        {
            if (newIdx >= 0 && oldIdx >= 0 && newIdx <= observableList.Count - 1 && oldIdx <= observableList.Count - 1)
                observableList.Move(oldIdx, newIdx);
        }
    }
}
