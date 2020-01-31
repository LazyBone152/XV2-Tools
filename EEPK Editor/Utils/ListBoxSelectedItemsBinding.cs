using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EEPK_Organiser.Utils
{
    public static class AttachedProperties
    {
        #region AttachedProperties.SelectedItems Attached Property
        public static IList GetSelectedItems(ListBox obj)
        {
            return (IList)obj.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(ListBox obj, IList value)
        {
            obj.SetValue(SelectedItemsProperty, value);
        }

        public static readonly DependencyProperty
            SelectedItemsProperty =
                DependencyProperty.RegisterAttached(
                    "SelectedItems",
                    typeof(IList),
                    typeof(AttachedProperties),
                    new PropertyMetadata(null,
                        SelectedItems_PropertyChanged));

        private static void SelectedItems_PropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var lb = d as ListBox;
            IList coll = e.NewValue as IList;

            //  If you want to go both ways and have changes to 
            //  this collection reflected back into the listbox...
            if (coll is INotifyCollectionChanged)
            {
                (coll as INotifyCollectionChanged)
                    .CollectionChanged += (s, e3) =>
                    {
                    //  Haven't tested this branch -- good luck!
                    if (null != e3.OldItems)
                            foreach (var item in e3.OldItems)
                                lb.SelectedItems.Remove(item);
                        if (null != e3.NewItems)
                            foreach (var item in e3.NewItems)
                                lb.SelectedItems.Add(item);
                    };
            }

            if (null != coll)
            {
                if (coll.Count > 0)
                {
                    //  Minor problem here: This doesn't work for initializing a 
                    //  selection on control creation. 
                    //  When I get here, it's because I've initialized the selected 
                    //  items collection that I'm binding. But at that point, lb.Items 
                    //  isn't populated yet, so adding these items to lb.SelectedItems 
                    //  always fails. 
                    //  Haven't tested this otherwise -- good luck!
                    lb.SelectedItems.Clear();
                    foreach (var item in coll)
                        lb.SelectedItems.Add(item);
                }

                lb.SelectionChanged += (s, e2) =>
                {
                    try
                    {
                        if (null != e2.RemovedItems)
                            foreach (var item in e2.RemovedItems)
                                coll.Remove(item);
                        if (null != e2.AddedItems)
                            foreach (var item in e2.AddedItems)
                                coll.Add(item);
                    }
                    catch
                    {

                    }
                };
            }
            
        }
        #endregion AttachedProperties.SelectedItems Attached Property
    }
}
