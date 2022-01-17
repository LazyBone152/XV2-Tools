using System;
using System.Collections.Generic;
using System.ComponentModel;
using GalaSoft.MvvmLight.CommandWpf;

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

        public const int DefaultMaxCapacity = 1000;
        public const int MaximumMaxCapacity = 10000;
        

        //Singleton
        private static readonly Lazy<UndoManager> instance = new Lazy<UndoManager>(() => new UndoManager());
        public static UndoManager Instance => instance.Value;

        //Event
        public event EventHandler UndoOrRedoCalled;

        //Instance
        private LimitedStack<IUndoRedo> undoStack;
        private LimitedStack<IUndoRedo> redoStack;
        private int Capacity = DefaultMaxCapacity;
        private DateTime LastAddition = DateTime.MinValue;
        private readonly TimeSpan MergeAdditionTimeSpan = new TimeSpan(0, 0, 1);

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

            if (!MergeUndo(undo))
            {
                if (redoStack.Count > 0)
                    redoStack.Clear();

                undoStack.Push(undo);
                NotifyPropertyChanged(nameof(UndoDescription));
                NotifyPropertyChanged(nameof(RedoDescription));
            }

            LastAddition = DateTime.Now;
        }

        public void AddCompositeUndo(List<IUndoRedo> undos, string message)
        {
            AddUndo(new CompositeUndo(undos, message));
        }

        /// <summary>
        /// Attempt to merge undos of the same time type were commited in the same time span. Only applies to <see cref="UndoableProperty{T}"/>, <see cref="UndoablePropertyGeneric"/>, and <see cref="UndoableField"/> <see cref="IUndoRedo"/> types.
        /// </summary>
        /// <returns>True if the undos were merged, otherwise false.</returns>
        private bool MergeUndo(IUndoRedo undo)
        {
            if (CanUndo() && LastAddition + MergeAdditionTimeSpan >= DateTime.Now)
            {
                IUndoRedo prevUndo = undoStack.First.Value;

                if (undo is IMergableUndo mergeUndo && prevUndo is IMergableUndo prevMergeUndo)
                {
                    //Undos qualify for merging
                    if(mergeUndo._field == prevMergeUndo._field && mergeUndo._instance == prevMergeUndo._instance && mergeUndo.doLast == prevMergeUndo.doLast)
                    {
                        prevMergeUndo._newValue = mergeUndo._newValue;
                        return true;
                    }
                }
                else if(undo is CompositeUndo compositeMerge && prevUndo is CompositeUndo compositePrev)
                {
                    if (compositePrev.CanMerge(compositeMerge))
                    {
                        compositePrev.MergeCompositeUndos(compositeMerge);
                        return true;
                    }
                }
            }

            return false;
        }

        public RelayCommand RedoCommand => new RelayCommand(Redo, CanRedo);

        public RelayCommand UndoCommand => new RelayCommand(Undo, CanUndo);


        public void Undo()
        {
            if (!CanUndo()) return;
            IUndoRedo action = undoStack.Pop();
            action.Undo();
            redoStack.Push(action);
            NotifyPropertyChanged("UndoDescription");
            NotifyPropertyChanged("RedoDescription");
            UndoOrRedoCalled?.Invoke(this, EventArgs.Empty);
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
            NotifyPropertyChanged(nameof(UndoDescription));
            NotifyPropertyChanged(nameof(RedoDescription));
        }

        public void ForceEventCall()
        {
            UndoOrRedoCalled?.Invoke(this, EventArgs.Empty);
        }


    }
    
}
