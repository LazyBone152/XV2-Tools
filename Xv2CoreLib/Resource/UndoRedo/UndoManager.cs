using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Windows;

#if Xv2CoreLib
using CommunityToolkit.Mvvm.Input;
using Xv2CoreLib.Color;
#endif

namespace Xv2CoreLib.Resource.UndoRedo
{
    public sealed partial class UndoManager : INotifyPropertyChanged
    {
#region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
#endregion

        public const int DefaultMaxCapacity = 1000;
        public const int MaximumMaxCapacity = 10000;
        

        //Singleton
        private static readonly Lazy<UndoManager> instance = new Lazy<UndoManager>(() => new UndoManager());
        public static UndoManager Instance => instance.Value;

        //Event
        public event UndoEventRaisedEventHandler UndoOrRedoCalled;
        /// <summary>
        /// Raised shortly after an Undo or Redo has been requested. The timer for it to be raised resets upon every Undo or Redo, meaning if a large number of Undo or Redo events occur in a short period of time, only one <see cref="RecentUndoOrRedo"/> will be raised for them.
        /// </summary>
        /// <remarks>This event carries no information about the Undo or Redo that occured - it just exists to inform the subscriber that one DID occur. If you need to know about the actual undo event, use <see cref="UndoOrRedoCalled"/>.</remarks>
        public event EventHandler RecentUndoOrRedo;

        //Instance
        private readonly LimitedStack<IUndoRedo> undoStack;
        private readonly LimitedStack<IUndoRedo> redoStack;
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
       private bool IsUndoingInternal = false;
        
        /// <summary>
        /// Indicates whether an undo or redo was recently requested (last 1 second).
        /// </summary>
        public bool HasRecentUndo { get; private set; }
        private DateTime LastUndo = DateTime.MinValue;
        private readonly TimeSpan UndoDuration = new TimeSpan(0, 0, 1); //"HasRecentUndo" will be true for 1 second after an Undo/Redo 
        private readonly Timer IsUndoingTimer = new Timer(100);


        private UndoManager()
        {
            undoStack = new LimitedStack<IUndoRedo>(Capacity);
            redoStack = new LimitedStack<IUndoRedo>(Capacity);
            UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
            IsUndoingTimer.Elapsed += IsUndoingTimer_Elapsed;
        }

        public void AddUndo(IUndoRedo undo)
        {
            if (Capacity == 0) return;

            if (!MergeUndo(undo))
            {
                if (redoStack.Count > 0)
                    redoStack.Clear();

                undoStack.Push(undo);
                RefreshUndoRedoState();
            }

            LastAddition = DateTime.Now;
        }

        public void AddUndo(IUndoRedo undo, UndoGroup undoGroup, string undoArg = null, object undoContext = null)
        {
            AddUndo(new UndoGroupContainer(undo, undoGroup, undoArg, undoContext));
        }

        public void AddCompositeUndo(List<IUndoRedo> undos, string message, UndoGroup undoGroup = UndoGroup.Default, string undoArg = null, object undoContext = null)
        {
            if(undos.Count > 0)
                AddUndo(new CompositeUndo(undos, message, undoGroup, undoArg, undoContext));
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

#if Xv2CoreLib
        [RelayCommand(CanExecute = nameof(CanUndo))]
#endif
        public void Undo()
        {
            if (!CanUndo()) return;

            IUndoRedo action = undoStack.Pop();

            try
            {
                IsUndoingInternal = true;
                HasRecentUndo = true;
                action.Undo();
                redoStack.Push(action);
                RefreshUndoRedoState();
            }
            finally
            {
                IsUndoingInternal = false;
                LastUndo = DateTime.UtcNow;
                IsUndoingTimer.Start();
                UndoOrRedoCalled?.Invoke(this, new UndoEventRaisedEventArgs(GetUndoGroup(action), GetUndoArg(action), GetUndoContext(action)));
            }

        }

#if Xv2CoreLib
        [RelayCommand(CanExecute = nameof(CanRedo))]
#endif
        public void Redo()
        {
            if (!CanRedo()) return;

            IUndoRedo action = redoStack.Pop();
            try
            {
                IsUndoingInternal = true;
                HasRecentUndo = true;
                action.Redo();
                undoStack.Push(action);
                RefreshUndoRedoState();
            }
            finally
            {
                IsUndoingInternal = false;
                LastUndo = DateTime.UtcNow;
                IsUndoingTimer.Start();
                UndoOrRedoCalled?.Invoke(this, new UndoEventRaisedEventArgs(GetUndoGroup(action), GetUndoArg(action), GetUndoContext(action)));
            }

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
            RefreshUndoRedoState();
        }

        private void RefreshUndoRedoState()
        {
            NotifyPropertyChanged(nameof(UndoDescription));
            NotifyPropertyChanged(nameof(RedoDescription));
#if Xv2CoreLib
            Application.Current.Dispatcher.Invoke(() =>
            {
                UndoCommand.NotifyCanExecuteChanged();
                RedoCommand.NotifyCanExecuteChanged();
            });
            //System.Windows.Input.CommandManager.InvalidateRequerySuggested();
#endif
        }

        /// <summary>
        /// Force invoke <see cref="UndoOrRedoCalled"/>.
        /// </summary>
        /// <param name="undoGroup">The UndoGroup attached to the event. This can be used to conditionally respond when the event is raised.</param>
        public void ForceEventCall(UndoGroup undoGroup = UndoGroup.Default, string undoArg = null, object undoContext = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UndoOrRedoCalled?.Invoke(this, new UndoEventRaisedEventArgs(undoGroup, undoArg, undoContext));
            });
        }

        private static UndoGroup GetUndoGroup(IUndoRedo undo)
        {
            var prop = undo.GetType().GetProperty(nameof(CompositeUndo.UndoGroup));

            if (prop != null)
            {
                return (UndoGroup)prop.GetValue(undo);
            }

            return UndoGroup.Default;
        }

        private static string GetUndoArg(IUndoRedo undo)
        {
            var prop = undo.GetType().GetProperty(nameof(CompositeUndo.UndoArg));

            if (prop != null)
            {
                return (string)prop.GetValue(undo);
            }

            return null;
        }

        private static object GetUndoContext(IUndoRedo undo)
        {
            var prop = undo.GetType().GetProperty(nameof(CompositeUndo.UndoContext));

            if (prop != null)
            {
                return prop.GetValue(undo);
            }

            return null;
        }

        private void UndoManager_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            #if Xv2CoreLib
            if (e.UndoGroup == UndoGroup.ColorControl)
            {
                if (e.UndoContext is IPreviewUpdate preview)
                {
                    preview.UpdatePreviewBrush();
                }
                else if (e.UndoContext is IEnumerable<IPreviewUpdate> previewCol)
                {
                    foreach (var previewEntry in previewCol)
                    {
                        previewEntry?.UpdatePreviewBrush();
                    }
                }
            }
            #endif
        }

        private void IsUndoingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (HasRecentUndo)
            {
                if(DateTime.UtcNow - LastUndo >= UndoDuration && !IsUndoingInternal)
                {
                    HasRecentUndo = false;
                    Application.Current.Dispatcher.Invoke(() => RecentUndoOrRedo?.Invoke(this, EventArgs.Empty));
                }
            }
            else
            {
                IsUndoingTimer.Stop();
            }
        }
    }

    public enum UndoGroup
    {
        Default,
        Animation,
        Camera,
        Effect,
        Action,
        BCS,
        EMD,
        ColorControl
    }

    public delegate void UndoEventRaisedEventHandler(object source, UndoEventRaisedEventArgs e);

    public class UndoEventRaisedEventArgs : EventArgs
    {
        public UndoGroup UndoGroup { get; private set; }
        public string UndoArg { get; private set; }
        public object UndoContext { get; private set; }

        public UndoEventRaisedEventArgs(UndoGroup undoGroup, string undoArg, object undoContext)
        {
            UndoGroup = undoGroup;
            UndoArg = undoArg;
            UndoContext = undoContext;
        }
    }
}
