using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.ExceptionServices;

#if NvvmLight
using GalaSoft.MvvmLight.CommandWpf;
#endif

namespace Xv2CoreLib.Resource.UndoRedo
{
    public sealed class UndoManager : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public const int DefaultMaxCapacity = 500;
        

        //Singleton
        private static readonly Lazy<UndoManager> instance = new Lazy<UndoManager>(() => new UndoManager());
        public static UndoManager Instance => instance.Value;

        //Event
        public event EventHandler UndoOrRedoCalled;

        //Instance
        private LimitedStack<IUndoRedo> undoStack;
        private LimitedStack<IUndoRedo> redoStack;
        private int Capacity = DefaultMaxCapacity;
        
        public string UndoDescription
        {
            get
            {
                if (!CanUndo()) return "Undo";
                return $"Undo | {undoStack.First.Value.Message}";
            }
        }
        public string RedoDescription
        {
            get
            {
                if (!CanRedo()) return "Redo";
                return $"Redo | {redoStack.First.Value.Message}";
            }
        }

        private UndoManager()
        {
            undoStack = new LimitedStack<IUndoRedo>(Capacity);
            redoStack = new LimitedStack<IUndoRedo>(Capacity);
        }

        public void AddUndo(IUndoRedo undo)
        {
            if (Capacity == 0) return;

            if (redoStack.Count > 0)
                redoStack.Clear();
            
            undoStack.Push(undo);
            NotifyPropertyChanged("UndoDescription");
            NotifyPropertyChanged("RedoDescription");
        }

        public void AddCompositeUndo(List<IUndoRedo> undos, string message)
        {
            AddUndo(new CompositeUndo(undos, message));
        }

#if NvvmLight
        public RelayCommand RedoCommand => new RelayCommand(Redo, CanRedo);

        public RelayCommand UndoCommand => new RelayCommand(Undo, CanUndo);
#endif

        public void Undo()
        {
            if (!CanUndo()) return;
            IUndoRedo action = undoStack.Pop();
            action.Undo();
            redoStack.Push(action);
            NotifyPropertyChanged("UndoDescription");
            NotifyPropertyChanged("RedoDescription");
            UndoOrRedoCalled?.Invoke(this, new EventArgs());
        }

        public void Redo()
        {
            if (!CanRedo()) return;
            IUndoRedo action = redoStack.Pop();
            action.Redo();
            undoStack.Push(action);
            NotifyPropertyChanged("UndoDescription");
            NotifyPropertyChanged("RedoDescription");
            UndoOrRedoCalled?.Invoke(this, new EventArgs());
        }

        public bool CanUndo()
        {
            return undoStack.Count > 0;
        }

        public bool CanRedo()
        {
            return redoStack.Count > 0;
        }
        
        public void SetCapacity(int capacity)
        {
            Capacity = capacity;
            undoStack.Resize(Capacity);
            redoStack.Resize(Capacity);
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            NotifyPropertyChanged("UndoDescription");
            NotifyPropertyChanged("RedoDescription");
        }


    }
    
}
