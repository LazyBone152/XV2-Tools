using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
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

        //Primary list
        private List<T> list;

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
                    _observableList.Add(item);
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
                        _observableList.Add(item);
                    }
                }
            }

            CollectionChanged?.Invoke(this, null);
        }

        public bool Remove(T item)
        {
            return RemoveItem(item);
        }

        public void RemoveRange(int index, int count)
        {
            lock (lockObj)
            {
                list.RemoveRange(index, count);

                if (_observableList != null)
                {
                    for (int i = index; i < index + count; i++)
                    {
                        _observableList.RemoveAt(i);
                    }
                }
            }

            CollectionChanged?.Invoke(this, null);
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
                    _observableList.RemoveAt(index);
                }
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        public void Move(int oldIndex, int newIndex)
        {
            lock (lockObj)
            {
                T item = this[oldIndex];
                list.RemoveAt(oldIndex);
                list.Insert(newIndex, item);

                if (_observableList != null)
                {
                    _observableList.Move(oldIndex, newIndex);
                }

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
                    _observableList.Insert(index, item);
                }
            }

            CollectionChanged?.Invoke(this, null);
        }

        public void Clear()
        {
            lock (lockObj)
            {
                list.Clear();

                if (_observableList != null)
                {
                    _observableList.Clear();
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
                    _observableList.Remove(item);
                }
            }

            if (wasRemoved)
                CollectionChanged?.Invoke(this, null);

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
                        _observableList.Add(item);
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
