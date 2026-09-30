using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace LB_Common.AttachedProperties
{
    /// <summary>
    /// Provides a bindable SelectedItems attached property for <see cref="DataGrid"/>
    /// </summary>
    /// <remarks>Supports <see cref="IList"/> and <seealso cref="INotifyCollectionChanged"/> interfaces.</remarks>
    public static class DataGridHelper
    {
        private static readonly ConditionalWeakTable<DataGrid, DataGridSelectedItemsState> StateTable = new ConditionalWeakTable<DataGrid, DataGridSelectedItemsState>();

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(DataGridHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));

        public static void SetSelectedItems(DependencyObject element, IList value) => element.SetValue(SelectedItemsProperty, value);

        public static IList GetSelectedItems(DependencyObject element) => (IList)element.GetValue(SelectedItemsProperty);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid dataGrid) return;

            DataGridSelectedItemsState state = StateTable.GetOrCreateValue(dataGrid);
            state.Init(dataGrid, e.OldValue, e.NewValue);
        }

        private class DataGridSelectedItemsState
        {
            private DataGrid dataGrid;
            private bool _attached = false;
            private bool _isUpdating = false;

            public void Init(DataGrid _dataGrid, object oldCollection, object newCollection)
            {
                dataGrid = _dataGrid;

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
                    dataGrid.SelectionChanged += OnSelectionChanged;

                    if (dataGrid.IsLoaded)
                    {
                        SyncToDataGrid();
                    }
                    else
                    {
                        dataGrid.Loaded += OnDataGridLoaded;
                    }

                    _attached = true;
                }
            }

            private IList GetSelectedItemsCollection() => GetSelectedItems(dataGrid);

            public void OnDataGridLoaded(object sender, RoutedEventArgs e)
            {
                dataGrid.Loaded -= OnDataGridLoaded;
                SyncToDataGrid();
            }

            public void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                IList selectedItemsCollection = GetSelectedItemsCollection();
                if (_isUpdating || selectedItemsCollection == null || dataGrid == null) return;

                _isUpdating = true;

                try
                {
                    selectedItemsCollection.Clear();

                    foreach (var item in dataGrid.SelectedItems)
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
                if (_isUpdating || dataGrid == null) return;

                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    SyncToDataGrid();
                    return;
                }

                _isUpdating = true;

                try
                {

                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            dataGrid.SelectedItems.Remove(item);
                        }
                    }

                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            dataGrid.SelectedItems.Add(item);
                        }
                    }

                }
                finally
                {
                    _isUpdating = false;
                }

            }

            private void SyncToDataGrid()
            {
                IList selectedItemsCollection = GetSelectedItemsCollection();
                if (selectedItemsCollection == null || dataGrid == null) return;

                _isUpdating = true;

                try
                {
                    dataGrid.SelectedItems.Clear();

                    foreach (var item in selectedItemsCollection)
                    {
                        dataGrid.SelectedItems.Add(item);
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
