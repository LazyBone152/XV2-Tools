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

        public const int DefaultMaxCapacity = 250;
        

        //Singleton
        private static readonly Lazy<UndoManager> instance = new Lazy<UndoManager>(() => new UndoManager());
        public static UndoManager Instance => instance.Value;

        //Event
        public event EventHandler UndoOrRedoCalled;

        //Instance
        private LimitedStack<IUndoRedo> undoStack;
        private LimitedStack<IUndoRedo> redoStack;
        private int Capacity = DefaultMaxCapacity;

        //CompositionUndo
        private bool compositionUndoStart = false;
        private List<IUndoRedo> compositionUndos = new List<IUndoRedo>();

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
            if (compositionUndoStart) throw new InvalidOperationException("UndoManager: AddUndo was called while an undo composition was underway.\n\nTo prevent possible corruption or data going out of sync you CANT add an undo while creating an CompositeUndo.");
            if (Capacity == 0) return;

            if (redoStack.Count > 0)
                redoStack.Clear();
            
            undoStack.Push(undo);
            NotifyPropertyChanged("UndoDescription");
            NotifyPropertyChanged("RedoDescription");
        }

#if NvvmLight
        public RelayCommand RedoCommand => new RelayCommand(Redo, CanRedo);

        public RelayCommand UndoCommand => new RelayCommand(Undo, CanUndo);
#endif

        public void Undo()
        {
            if (compositionUndoStart) throw new InvalidOperationException("UndoManager: Undo was called while an undo composition was underway.\n\nMake sure you have called FinishUndoComposition() before returning control to the GUI!");
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
            if (compositionUndoStart) throw new InvalidOperationException("UndoManager: Redo was called while an undo composition was underway.\n\nMake sure you have called FinishUndoComposition() before returning control to the GUI!");
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

        /// <summary>
        /// Start creating a CompositionUndo. After calling this method, add to the CompositionUndo with the AddToUndoComposition method. When finished, call FinishUndoComposition.
        /// While adding composition undos you will be unable to call AddUndo().
        /// </summary>
        public void StartUndoComposition()
        {
            compositionUndoStart = true;
            compositionUndos.Clear();
        }

        /// <summary>
        /// Add an undo state to the CompositeUndo. \n\nNote: Calling this method will have no effect at all unless you have previously called StartUndoComposition(). You must also call FinishUndoComposition when you are done, and at that point the CompositeUndo will be added to the undo stack.
        /// </summary>
        public void AddToUndoComposition(IUndoRedo undo)
        {
            if(compositionUndoStart)
                compositionUndos.Add(undo);
        }

        /// <summary>
        /// Finalize the undo composition and push it to the undo stack.
        /// </summary>
        /// <param name="undoMessage">This is the message that will be displayed as the undo/redo stage.</param>
        public void FinishUndoComposition(string undoMessage)
        {
            if(compositionUndoStart && compositionUndos.Count > 0)
            {
                undoStack.Push(new CompositeUndo(compositionUndos, undoMessage));
            }

            compositionUndoStart = false;
            compositionUndos.Clear();
        }

        /// <summary>
        /// Undo all changes that have been added to the undo composition (AddToUndoComposition()). Use this in the event of an error and you want to undo everything.
        /// </summary>
        public void RollbackUndoComposition()
        {
            if (!compositionUndoStart) return;

            try
            {
                var undos = new CompositeUndo(compositionUndos, "");
                undos.Undo();
            }
            catch (Exception ex)
            {
                throw new Exception($"UndoManager.RollbackUndoComposition: Failed rollback.\n\n{ex.Message}", ex);
            }
            finally
            {
                compositionUndos.Clear();
                compositionUndoStart = false;
            }
        }

    }
    
}
