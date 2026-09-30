using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace LB_Common.AttachedProperties
{
    /// <summary>
    /// Provides a bindable SelectedItems attached property for <see cref="ListBox"/>
    /// </summary>
    /// <remarks>Supports <see cref="IList"/> and <seealso cref="INotifyCollectionChanged"/> interfaces.</remarks>
    public static class ListBoxHelper
    {
        private static readonly ConditionalWeakTable<ListBox, ListBoxSelectedItemsState> StateTable = new ConditionalWeakTable<ListBox, ListBoxSelectedItemsState>();

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(ListBoxHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));

        public static void SetSelectedItems(DependencyObject element, IList value) => element.SetValue(SelectedItemsProperty, value);

        public static IList GetSelectedItems(DependencyObject element) => (IList)element.GetValue(SelectedItemsProperty);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox) return;

            ListBoxSelectedItemsState state = StateTable.GetOrCreateValue(listBox);
            state.Init(listBox, e.OldValue, e.NewValue);
        }

        private class ListBoxSelectedItemsState
        {
            private ListBox listBox;
            private bool _attached = false;
            private bool _isUpdating = false;

            public void Init(ListBox _listBox, object oldCollection, object newCollection)
            {
                listBox = _listBox;

                if (oldCollection is INotifyCollectionChanged oldCol)
                {
                    oldCol.CollectionChanged -= OnCollectionChanged;
                }

                if (newCollection is INotifyCollectionChanged newCol)
                {
                    newCol.CollectionChanged += OnCollectionChanged;
                }

                //Subscribe to event handlers on the control. Only do this once on the initial binding
                if (!_attached)
                {
                    listBox.SelectionChanged += OnSelectionChanged;

                    if (listBox.IsLoaded)
                    {
                        SyncToListBox();
                    }
                    else
                    {
                        listBox.Loaded += OnListBoxLoaded;
                    }

                    _attached = true;
                }
            }

            private IList GetSelectedItemsCollection() => GetSelectedItems(listBox);

            public void OnListBoxLoaded(object sender, RoutedEventArgs e)
            {
                listBox.Loaded -= OnListBoxLoaded;
                SyncToListBox();
            }

            public void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                IList selectedItemsCollection = GetSelectedItemsCollection();
                if (_isUpdating || selectedItemsCollection == null || listBox == null) return;

                _isUpdating = true;

                try
                {
                    selectedItemsCollection.Clear();

                    foreach (var item in listBox.SelectedItems)
                    {
                        selectedItemsCollection.Add(item);
                    }
                    
                }
                finally
                {
                    _isUpdating = false;
                }
            }

            public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (_isUpdating || listBox == null) return;

                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    SyncToListBox();
                    return;
                }

                _isUpdating = true;

                try
                {
                    
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            listBox.SelectedItems.Remove(item);
                        }
                    }

                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            listBox.SelectedItems.Add(item);
                        }
                    }
                    
                }
                finally
                {
                    _isUpdating = false;
                }
            }

            private void SyncToListBox()
            {
                IList selectedItemsCollection = GetSelectedItemsCollection();
                if (selectedItemsCollection == null || listBox == null) return;

                _isUpdating = true;

                try
                {
                    listBox.SelectedItems.Clear();

                    foreach (var item in selectedItemsCollection)
                    {
                        listBox.SelectedItems.Add(item);
                    }
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }
    }
}
