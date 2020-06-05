using ProTagger.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ProTagger.Wpf
{
    public class ListViewSelectionBinder
    {
        private readonly ListBox _listBox;
        private readonly IList _selectedItems;

        public ListViewSelectionBinder(ListBox listBox, IList selectedItems)
        {
            _listBox = listBox ?? throw new ArgumentNullException(nameof(listBox));
            _selectedItems = selectedItems ?? throw new ArgumentNullException(nameof(selectedItems));
            if (_listBox.SelectedItems.Count > 0)
                _listBox.SelectedItems.Clear();
            if (_selectedItems is IBatchList batchList)
                batchList.ModifyNoDuplicates(_selectedItems, Enumerable.Empty<object>(), true);
            else
                foreach (var item in _selectedItems)
                    _listBox.SelectedItems.Add(item);
        }

        public void Subscribe()
        {
            WeakEventManager<ListBox, SelectionChangedEventArgs>.AddHandler(
                _listBox, nameof(_listBox.SelectionChanged), OnSelectionChangedView);
            if (_selectedItems is INotifyCollectionChanged selectedItems)
                WeakEventManager<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>.AddHandler(
                    selectedItems, nameof(selectedItems.CollectionChanged), SelectionChangedViewModel);
        }

        public void Unsubscribe()
        {
            WeakEventManager<ListBox, SelectionChangedEventArgs>.RemoveHandler(
                _listBox, nameof(_listBox.SelectionChanged), OnSelectionChangedView);
            if (_selectedItems is INotifyCollectionChanged selectedItems)
                WeakEventManager<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>.RemoveHandler(
                    selectedItems, nameof(selectedItems.CollectionChanged), SelectionChangedViewModel);
        }

        private void SelectionChangedViewModel(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_listBox is ListBoxKeepSelection listBox)
            {
                var newSelection = _listBox.SelectedItems.Cast<object?>().ToHashSet();
                if (e.NewItems != null)
                    newSelection.UnionWith(e.NewItems.Cast<object>());
                if (e.OldItems != null)
                    newSelection.ExceptWith(e.OldItems.Cast<object>());
                listBox.ChangeSelectedItems(newSelection);
            }
            else
            {
                if (e.NewItems != null)
                {
                    var toAdd = e.NewItems.Cast<object>().ToHashSet();
                    toAdd.ExceptWith(_listBox.SelectedItems.Cast<object>());
                    foreach (var item in toAdd)
                        _listBox.SelectedItems.Add(item);
                }
                if (e.OldItems != null)
                {
                    var toRemove = e.OldItems.Cast<object>().ToHashSet();
                    toRemove.IntersectWith(_listBox.SelectedItems.Cast<object>());
                    foreach (var item in toRemove)
                        _listBox.SelectedItems.Remove(item);
                }
            }
        }

        private void OnSelectionChangedView(object? sender, SelectionChangedEventArgs e)
        {
            if (_selectedItems is IBatchList batchList)
            {
                batchList.ModifyNoDuplicates(e.AddedItems, e.RemovedItems, true);
            }
            else
            {
                foreach (var item in e.AddedItems ?? new object[0])
                    if (!_selectedItems.Contains(item))
                        _selectedItems.Add(item);
                foreach (var item in e.RemovedItems ?? new object[0])
                    _selectedItems.Remove(item);
            }
        }

        private static ListViewSelectionBinder? GetSelectionBinder(DependencyObject obj)
            =>  (ListViewSelectionBinder)obj.GetValue(SelectionBinderProperty);

        private static void SetSelectionBinder(DependencyObject obj, ListViewSelectionBinder items)
            => obj.SetValue(SelectionBinderProperty, items);

        private static readonly DependencyProperty SelectionBinderProperty = 
            DependencyProperty.RegisterAttached("SelectionBinder", typeof(ListViewSelectionBinder), typeof(ListViewSelectionBinder));

        public static void SetSelectedItems(DependencyObject elementName, IEnumerable value)
            => elementName.SetValue(SelectedItemsProperty, value);

        public static IEnumerable? GetSelectedItems(DependencyObject elementName)
            => (IEnumerable)elementName.GetValue(SelectedItemsProperty);

        public static readonly DependencyProperty SelectedItemsProperty = 
            DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(ListViewSelectionBinder),
            new FrameworkPropertyMetadata(null, OnSelectedItemsChanged));


        private static void OnSelectedItemsChanged(DependencyObject o, DependencyPropertyChangedEventArgs value)
        {
            GetSelectionBinder(o)?.Unsubscribe();
            var newSelectionBinder = new ListViewSelectionBinder((ListBox)o, (IList?)value.NewValue ?? new List<object>());
            SetSelectionBinder(o, newSelectionBinder);
            newSelectionBinder.Subscribe();
        }
    }
}
