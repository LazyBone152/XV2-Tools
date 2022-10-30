using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.Resource
{
    /// <summary>
    /// A composite, read-only version of <see cref="AsyncObservableCollection{T}"/>, that automatically updates to reflect changes to the underlying collections. For displaying mixed-type lists in UI elements.
    /// </summary>
    [Serializable]
    public class CompositeReadOnlyAsyncObservableCollection : AsyncObservableCollection<object>
    {
        private List<IEnumerable> lists = new List<IEnumerable>();
        private System.Timers.Timer delayedUpdateTimer;

        public CompositeReadOnlyAsyncObservableCollection(int refreshDelayMs = 500)
        {
            delayedUpdateTimer = new System.Timers.Timer(refreshDelayMs);
            delayedUpdateTimer.Elapsed += Timer_Elapsed;
            delayedUpdateTimer.AutoReset = true;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            delayedUpdateTimer.Stop();
            SyncLists();
        }

        private void FirstCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            /*
            if(e?.Action == NotifyCollectionChangedAction.Remove)
            {
                if(e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                        Remove(item);
                }
            }
            else if(e?.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                        Add(item);
                }
            }
            else
            {
                //All other list changes will recreate the whole list - but on a delayed timer to avoid doing so excessively
                delayedUpdateTimer.Start();
            }
            */
            delayedUpdateTimer.Start();
        }

        public void SyncLists()
        {
            Clear();

            foreach (var list in lists)
            {
                foreach(var entry in list)
                    Add(entry);
            }
        }

        public void AddList<T>(AsyncObservableCollection<T> list, IEnumerable generic)
        {
            if (!lists.Contains(list))
            {
                list.CollectionChanged += FirstCollection_CollectionChanged;
                lists.Add(generic);
            }
        }
    }

    [Serializable]
    public class AsyncObservableCollection<T> : IList<T>
    {

        private object lockObj = new object();
        private bool IsListBeingModified = false;

        //Primary list
        private readonly List<T> list;

        //The observable collection. This is only created when the property requests it (via a binding). This ensures that is will always be created in the UI thread.
        [field: NonSerialized]
        private ROACollection<T> _observableList = null;
        /// <summary>
        /// The underlying ObservableCollection. Bind this in XAML, or access it somewhere in code that will run on the UI thread.
        /// </summary>
        [YAXDontSerialize]
        public ROACollection<T> Binding
        {
            get
            {
                if (_observableList == null)
                {
                    lock (lockObj)
                    {
                        _observableList = new ROACollection<T>(this);
                        _observableList.CollectionChanged += _observableList_CollectionChanged;
                    }
                }
                return _observableList;
            }
        }

        [YAXDontSerialize]
        public int Count => list.Count;
        [YAXDontSerialize]
        public bool IsReadOnly => false;

        [field: NonSerialized]
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public AsyncObservableCollection()
        {
            list = new List<T>();
        }

        public AsyncObservableCollection(IEnumerable<T> _list)
        {
            list = new List<T>(_list);
        }

        public T this[int index]
        {
            get { return list[index]; }
            set
            {
                lock (lockObj)
                {
                    list[index] = value;

                    if (_observableList != null)
                    {
                        _observableList[index] = value;
                        CollectionChanged?.Invoke(this, null);
                    }
                }
            }
        }

        public void Add(T item)
        {
            lock (lockObj)
            {
                list.Add(item);

                if (_observableList != null)
                {
                    IsListBeingModified = true;
                    _observableList.Add(item);
                    IsListBeingModified = false;
                }
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void AddRange(IEnumerable<T> items)
        {
            lock (lockObj)
            {
                list.AddRange(items);

                if (_observableList != null)
                {
                    foreach (var item in items)
                    {
                        IsListBeingModified = true;
                        _observableList.Add(item);
                        IsListBeingModified = false;
                    }
                }
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items));
        }

        public bool Remove(T item)
        {
            return RemoveItem(item);
        }

        public void RemoveRange(int index, int count)
        {
            List<T> removedItems = list.GetRange(index, count);

            lock (lockObj)
            {
                list.RemoveRange(index, count);

                if (_observableList != null)
                {
                    for (int i = index; i < index + count; i++)
                    {
                        IsListBeingModified = true;
                        _observableList.RemoveAt(i);
                        IsListBeingModified = false;
                    }
                }
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));
        }

        public void RemoveAt(int index)
        {
            object item;

            if (list.Count > index)
                item = list[index];
            else
                item = null;

            lock (lockObj)
            {
                list.RemoveAt(index);

                if (_observableList != null)
                {
                    IsListBeingModified = true;
                    _observableList.RemoveAt(index);
                    IsListBeingModified = false;
                }
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        public void Move(int oldIndex, int newIndex)
        {
            Move(oldIndex, newIndex, false);
        }
        private void Move(int oldIndex, int newIndex, bool suppressEvent)
        {
            lock (lockObj)
            {
                T item = this[oldIndex];
                list.RemoveAt(oldIndex);
                list.Insert(newIndex, item);

                if (_observableList != null && !suppressEvent)
                {
                    IsListBeingModified = true;
                    _observableList.Move(oldIndex, newIndex);
                    IsListBeingModified = false;
                }

                if(!suppressEvent)
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
            }
        }


        public void Insert(int index, T item)
        {
            lock (lockObj)
            {
                list.Insert(index, item);

                if (_observableList != null)
                {
                    IsListBeingModified = true;
                    _observableList.Insert(index, item);
                    IsListBeingModified = false;
                }
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void Clear()
        {
            lock (lockObj)
            {
                list.Clear();

                if (_observableList != null)
                {
                    IsListBeingModified = true;
                    _observableList.Clear();
                    IsListBeingModified = false;
                }
            }
        }

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        bool ICollection<T>.Remove(T item)
        {
            return RemoveItem(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        private bool RemoveItem(T item)
        {
            bool wasRemoved;

            lock (lockObj)
            {
                wasRemoved = list.Remove(item);

                if (_observableList != null)
                {
                    IsListBeingModified = true;
                    _observableList.Remove(item);
                    IsListBeingModified = false;
                }
            }

            if (wasRemoved)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));

            return wasRemoved;
        }

        public void Sort(Comparison<T> comparison)
        {
            lock (list)
            {
                list.Sort(comparison);

                if (_observableList != null)
                {
                    lock (_observableList)
                    {
                        for(int i = 0; i < list.Count; i++)
                        {
                            _observableList[i] = list[i];
                        }
                    }
                }
            }
        }

        private void Resync()
        {
            if (_observableList != null)
            {
                lock (lockObj)
                {
                    _observableList.Clear();

                    foreach (var item in this)
                    {
                        IsListBeingModified = true;
                        _observableList.Add(item);
                        IsListBeingModified = false;
                    }
                }
            }
        }

        internal bool HasObservableList()
        {
            return _observableList != null;
        }

        [Obsolete("Not required anymore. Just create a new instance directly.")]
        public static AsyncObservableCollection<T> Create()
        {
            return new AsyncObservableCollection<T>();
        }

        #region UndoableChanges
        //Mirror changes to the observable collection back to the main one
        //Add undoable steps

        private void _observableList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsListBeingModified) return;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if(e.NewStartingIndex != -1)
                    {
                        //Insert
                        if (e.NewItems[0] is T value)
                        {
                            list.Insert(e.NewStartingIndex, value);
                            undos.Add(new UndoableListInsert<T>(this, e.NewStartingIndex, value));
                        }
                    }
                    else
                    {
                        foreach(object item in e.NewItems)
                        {
                            if(item is T value)
                            {
                                list.Add(value);
                                undos.Add(new UndoableListAdd<T>(this, value));
                            }
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach(var item in e.OldItems)
                    {
                        if(item is T value)
                        {
                            undos.Add(new UndoableListRemove<T>(this, value));
                            list.Remove(value);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    undos.Add(new UndoableListMove<T>(this, e.OldStartingIndex, e.NewStartingIndex));
                    Move(e.OldStartingIndex, e.NewStartingIndex, true);
                    break;

            }

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Item Move", allowCompositeMerging: true));
        }

        #endregion
    }

    public class ROACollection<T> : ObservableCollection<T>
    {
        private SynchronizationContext synchronizationContext = SynchronizationContext.Current;

        public ROACollection()
        {
        }

        public ROACollection(IEnumerable<T> list) : base(list)
        {
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (SynchronizationContext.Current == synchronizationContext)
            {
                RaiseCollectionChanged(e);
            }
            else
            {
                synchronizationContext?.Send(RaiseCollectionChanged, e);
            }
        }

        private void RaiseCollectionChanged(object param)
        {
            base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (SynchronizationContext.Current == synchronizationContext)
            {
                RaisePropertyChanged(e);
            }
            else
            {
                synchronizationContext?.Send(RaisePropertyChanged, e);
            }
        }

        private void RaisePropertyChanged(object param)
        {
            base.OnPropertyChanged((PropertyChangedEventArgs)param);
        }

    }
}
