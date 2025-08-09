using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;

namespace LB_Common.Utils
{
    public static class TreeViewMultipleSelectionAttached
    {
        public static readonly DependencyProperty IsMultipleSelectionProperty =
        DependencyProperty.RegisterAttached(
            "IsMultipleSelection",
            typeof(bool),
            typeof(TreeViewMultipleSelectionAttached),
            new PropertyMetadata(false, OnMultipleSelectionPropertyChanged));

        public static bool GetIsMultipleSelection(TreeView element)
        {
            return (bool)element.GetValue(IsMultipleSelectionProperty);
        }

        public static void SetIsMultipleSelection(TreeView element, bool value)
        {
            element.SetValue(IsMultipleSelectionProperty, value);
        }
        private static void OnMultipleSelectionPropertyChanged(DependencyObject d,
                                         DependencyPropertyChangedEventArgs e)
        {
            TreeView treeView = d as TreeView;

            if (treeView != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                    {
                        treeView.AddHandler(TreeViewItem.MouseLeftButtonDownEvent,
                          new MouseButtonEventHandler(OnTreeViewItemClicked), true);

                        treeView.AddHandler(TreeViewItem.PreviewKeyDownEvent,
                          new KeyEventHandler(OnTreeViewKeyboardInput), true);

                        treeView.AddHandler(TreeViewItem.SelectedEvent,
                          new RoutedEventHandler(OnTreeViewItemSelectecd), true);
                    }
                    else
                    {
                        treeView.RemoveHandler(TreeViewItem.MouseLeftButtonDownEvent,
                             new MouseButtonEventHandler(OnTreeViewItemClicked));

                        treeView.RemoveHandler(TreeViewItem.PreviewKeyDownEvent,
                          new KeyEventHandler(OnTreeViewKeyboardInput));

                        treeView.RemoveHandler(TreeViewItem.SelectedEvent,
                          new RoutedEventHandler(OnTreeViewItemSelectecd));
                    }
                }
            }
        }

        private static void OnTreeViewItemSelectecd(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = FindTreeViewItem(
                                            e.OriginalSource as DependencyObject);
            TreeView treeView = sender as TreeView;

            if (treeViewItem != null && treeView != null)
            {
                treeViewItem.IsSelected = false; //Disable the TreeViews original selection logic
            }
        }

        private static void OnTreeViewKeyboardInput(object sender, KeyboardEventArgs e)
        {
            TreeViewItem treeViewItem = FindTreeViewItem(
                                            e.OriginalSource as DependencyObject);
            TreeView treeView = sender as TreeView;

            if (treeViewItem != null && treeView != null)
            {
                //TODO: Implement entry selecting with keyboard (arrow keys)
                if(Keyboard.IsKeyDown(Key.Down) || Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.Left) || Keyboard.IsKeyDown(Key.Right))
                    e.Handled = true;
            }
        }

        private static void OnTreeViewItemClicked(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = FindTreeViewItem(
                                            e.OriginalSource as DependencyObject);
            TreeView treeView = sender as TreeView;

            if (treeViewItem != null && treeView != null)
            {
                treeViewItem.IsSelected = false; //Disable the TreeViews original selection logic

                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    SelectMultipleItemsRandomly(treeView, treeViewItem);
                }
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    SelectMultipleItemsContinuously(treeView, treeViewItem);
                }
                else
                {
                    SelectSingleItem(treeView, treeViewItem);
                }
            }
        }
        private static TreeViewItem FindTreeViewItem(DependencyObject dependencyObject)
        {
            if (dependencyObject == null)
            {
                return null;
            }

            TreeViewItem treeViewItem = dependencyObject as TreeViewItem;
            if (treeViewItem != null)
            {
                return treeViewItem;
            }

            return FindTreeViewItem(VisualTreeHelper.GetParent(dependencyObject));
        }
        private static void SelectSingleItem(TreeView treeView, TreeViewItem treeViewItem)
        {
            // first deselect all items
            DeSelectAllItems(treeView, null);
            SetIsItemSelected(treeViewItem, true);
            SetStartItem(treeView, treeViewItem);
        }


        private static void DeSelectAllItems(TreeView treeView,  TreeViewItem treeViewItem)
        {
            if (treeView != null)
            {
                for (int i = 0; i < treeView.Items.Count; i++)
                {
                    TreeViewItem item = treeView.ItemContainerGenerator.
                                               ContainerFromIndex(i) as TreeViewItem;
                    if (item != null)
                    {
                        SetIsItemSelected(item, false);
                        DeSelectAllItems(null, item);
                    }
                }
            }
            else
            {
                for (int i = 0; i < treeViewItem.Items.Count; i++)
                {
                    TreeViewItem item = treeViewItem.ItemContainerGenerator.
                                               ContainerFromIndex(i) as TreeViewItem;
                    if (item != null)
                    {
                        SetIsItemSelected(item, false);
                        DeSelectAllItems(null, item);
                    }
                }
            }
        }

        public static readonly DependencyProperty IsItemSelectedProperty =
        DependencyProperty.RegisterAttached(
            "IsItemSelected",
            typeof(bool),
            typeof(TreeViewMultipleSelectionAttached),
            new PropertyMetadata(false, OnIsItemSelectedPropertyChanged));

        public static bool GetIsItemSelected(TreeViewItem element)
        {
            return (bool)element.GetValue(IsItemSelectedProperty);
        }

        public static void SetIsItemSelected(TreeViewItem element, bool value)
        {
            element.SetValue(IsItemSelectedProperty, value);
        }

        private static void OnIsItemSelectedPropertyChanged(DependencyObject d,
                                           DependencyPropertyChangedEventArgs e)
        {
            TreeViewItem treeViewItem = d as TreeViewItem;
            TreeView treeView = FindTreeView(treeViewItem);
            if (treeViewItem != null && treeView != null)
            {
                var selectedItems = GetSelectedItems(treeView);
                if (selectedItems != null)
                {
                    if (GetIsItemSelected(treeViewItem))
                    {
                        if(!selectedItems.Contains(treeViewItem.Header))
                            selectedItems.Add(treeViewItem.Header);
                    }
                    else
                    {
                        selectedItems.Remove(treeViewItem.Header);
                    }
                }
                SetSelectedItems(treeView, selectedItems);
            }
        }

        private static TreeView FindTreeView(DependencyObject dependencyObject)
        {
            if (dependencyObject == null)
            {
                return null;
            }

            TreeView treeView = dependencyObject as TreeView;
            if (treeView != null)
            {
                return treeView;
            }

            return FindTreeView(VisualTreeHelper.GetParent(dependencyObject));
        }

        public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(IList),
            typeof(TreeViewMultipleSelectionAttached),
            new PropertyMetadata());

        public static IList GetSelectedItems(TreeView element)
        {
            return (IList)element.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(TreeView element, IList value)
        {
            element.SetValue(SelectedItemsProperty, value);
        }

        private static readonly DependencyProperty StartItemProperty =
        DependencyProperty.RegisterAttached(
            "StartItem",
            typeof(TreeViewItem),
            typeof(TreeViewMultipleSelectionAttached),
            new PropertyMetadata());

        private static TreeViewItem GetStartItem(TreeView element)
        {
            return (TreeViewItem)element.GetValue(StartItemProperty);
        }

        private static void SetStartItem(TreeView element, TreeViewItem value)
        {
            element.SetValue(StartItemProperty, value);
        }

        private static void SelectMultipleItemsRandomly(TreeView treeView,
                                                    TreeViewItem treeViewItem)
        {
            SetIsItemSelected(treeViewItem, !GetIsItemSelected(treeViewItem));
            if (GetStartItem(treeView) == null)
            {
                if (GetIsItemSelected(treeViewItem))
                {
                    SetStartItem(treeView, treeViewItem);
                }
            }
            else
            {
                if (GetSelectedItems(treeView).Count == 0)
                {
                    SetStartItem(treeView, null);
                }
            }
        }

        private static void SelectMultipleItemsContinuously(TreeView treeView,
                                                     TreeViewItem treeViewItem)
        {
            TreeViewItem startItem = GetStartItem(treeView);
            if (startItem != null)
            {
                if (startItem == treeViewItem)
                {
                    SelectSingleItem(treeView, treeViewItem);
                    return;
                }

                ICollection<TreeViewItem> allItems = new List<TreeViewItem>();
                GetAllItems(treeView, null, allItems);
                DeSelectAllItems(treeView, null);
                bool isBetween = false;
                foreach (var item in allItems)
                {
                    if (item == treeViewItem || item == startItem)
                    {
                        // toggle to true if first element is found and
                        // back to false if last element is found
                        isBetween = !isBetween;

                        // set boundary element
                        SetIsItemSelected(item, true);
                        continue;
                    }

                    if (isBetween)
                    {
                        SetIsItemSelected(item, true);
                    }
                }
            }
        }

        private static void GetAllItems(TreeView treeView, TreeViewItem treeViewItem,
                                    ICollection<TreeViewItem> allItems)
        {
            if (treeView != null)
            {
                for (int i = 0; i < treeView.Items.Count; i++)
                {
                    TreeViewItem item = treeView.ItemContainerGenerator.
                                               ContainerFromIndex(i) as TreeViewItem;
                    if (item != null)
                    {
                        allItems.Add(item);
                        GetAllItems(null, item, allItems);
                    }
                }
            }
            else
            {
                for (int i = 0; i < treeViewItem.Items.Count; i++)
                {
                    TreeViewItem item = treeViewItem.ItemContainerGenerator.
                                               ContainerFromIndex(i) as TreeViewItem;
                    if (item != null)
                    {
                        allItems.Add(item);
                        GetAllItems(null, item, allItems);
                    }
                }
            }
        }


    }

}