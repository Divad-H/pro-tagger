using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ProTagger.Wpf
{
    public interface IBatchList
    {
        void Modify(IEnumerable itemsToAdd, IEnumerable itemsToRemove);
        void ModifyNoDuplicates(IEnumerable itemsToAdd, IEnumerable itemsToRemove);
    }
    public interface IBatchList<T>
    {
        void Modify(IEnumerable<T> itemsToAdd, IEnumerable<T> itemsToRemove);
        void ModifyNoDuplicates(IEnumerable<T> itemsToAdd, IEnumerable<T> itemsToRemove);
    }

    public class BatchList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection, IList, INotifyCollectionChanged, IBatchList, IBatchList<T>
    {
        private readonly List<T> _list = new List<T>();

        public BatchList() { }
        public BatchList(List<T> items) => _list = items;
        public BatchList(IList<T> items) => _list = new List<T>(items);

        public T this[int index] { get => _list[index]; set => _list[index] = value; }
        object? IList.this[int index] { get => _list[index]; set => (_list as IList)[index] = value; }

        public int Count => _list.Count;

        public bool IsReadOnly => (_list as ICollection<T>).IsReadOnly;

        public bool IsSynchronized => (_list as ICollection).IsSynchronized;

        public object SyncRoot => (_list as ICollection).SyncRoot;

        public bool IsFixedSize => (_list as IList).IsFixedSize;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public void Add(T item)
        {
            _list.Add(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public int Add(object? value)
        {
            var res = (_list as IList).Add(value);
#pragma warning disable CS8601 // Possible null reference assignment.
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (T)value));
#pragma warning restore CS8601 // Possible null reference assignment.
            return res;
        }

        public void Clear()
        {
            _list.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public bool Contains(object? value)
        {
            return (_list as IList).Contains(value);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            (_list as IList).CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public int IndexOf(object? value)
        {
            return (_list as IList).IndexOf(value);
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void Insert(int index, object? value)
        {
            (_list as IList).Insert(index, value);
#pragma warning disable CS8601 // Possible null reference assignment.
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (T)value, index));
#pragma warning restore CS8601 // Possible null reference assignment.
        }

        public void Modify(IEnumerable itemsToAdd, IEnumerable itemsToRemove)
        {
            Modify(itemsToAdd.Cast<T>(), itemsToRemove.Cast<T>());
        }
        public void ModifyNoDuplicates(IEnumerable itemsToAdd, IEnumerable itemsToRemove)
        {
            ModifyNoDuplicates(itemsToAdd.Cast<T>(), itemsToRemove.Cast<T>());
        }

        public void Modify(IEnumerable<T> itemsToAdd, IEnumerable<T> itemsToRemove)
        {
            itemsToRemove = itemsToRemove.Where(item => _list.Contains(item)).ToList();
            foreach (var item in itemsToRemove)
                _list.Remove(item);
            foreach (var item in itemsToAdd)
                _list.Add(item);
            if (!itemsToRemove.Any() && !itemsToAdd.Any())
                return;
            if (!itemsToRemove.Any())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, itemsToAdd.ToList()));
                return;
            }
            if (!itemsToAdd.Any())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, itemsToRemove.ToList()));
                return;
            }
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Replace, itemsToAdd.ToList(), itemsToRemove.ToList()));
        }

        public void ModifyNoDuplicates(IEnumerable<T> itemsToAdd, IEnumerable<T> itemsToRemove)
        {
            Modify(itemsToAdd.Where(item => !_list.Contains(item)).ToList(), itemsToRemove);
        }

        public bool Remove(T item)
        {
            var res = _list.Remove(item);
            if (res)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            return res;
        }

        public void Remove(object? value)
        {
            (_list as IList).Remove(value);
#pragma warning disable CS8601 // Possible null reference assignment.
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (T)value));
#pragma warning restore CS8601 // Possible null reference assignment.
        }

        public int RemoveAll(Func<T, bool> match)
        {
            var itemsToRemove = _list.Where(match).ToList();
            if (!itemsToRemove.Any())
                return 0;
            foreach (var item in itemsToRemove)
                _list.Remove(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, itemsToRemove));
            return itemsToRemove.Count;
        }

        public void RemoveAt(int index)
        {
            var item = _list[index];
            _list.RemoveAt(index);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    public class ListViewSelectionBinder
    {
        private readonly ListBox _listBox;
        private readonly IList _selectedItems;

        public ListViewSelectionBinder(ListBox listBox, IList selectedItems)
        {
            _listBox = listBox ?? throw new ArgumentNullException(nameof(listBox));
            _selectedItems = selectedItems ?? throw new ArgumentNullException(nameof(selectedItems));
            _listBox.SelectedItems.Clear();
            if (_selectedItems is IBatchList batchList)
                batchList.ModifyNoDuplicates(_selectedItems, Enumerable.Empty<object>());
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
            foreach (var item in e.NewItems ?? new object[0])
                if (!_listBox.SelectedItems.Contains(item))
                    _listBox.SelectedItems.Add(item);
            foreach (var item in e.OldItems ?? new object[0])
                _listBox.SelectedItems.Remove(item);
        }

        private void OnSelectionChangedView(object? sender, SelectionChangedEventArgs e)
        {
            if (_selectedItems is IBatchList batchList)
            {
                batchList.ModifyNoDuplicates(e.AddedItems, e.RemovedItems);
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
        {
            return (ListViewSelectionBinder)obj.GetValue(SelectionBinderProperty);
        }

        private static void SetSelectionBinder(DependencyObject obj, ListViewSelectionBinder items)
        {
            obj.SetValue(SelectionBinderProperty, items);
        }

        private static readonly DependencyProperty SelectionBinderProperty = 
            DependencyProperty.RegisterAttached("SelectionBinder", typeof(ListViewSelectionBinder), typeof(ListViewSelectionBinder));

        public static void SetSelectedItems(DependencyObject elementName, IEnumerable value)
        {
            elementName.SetValue(SelectedItemsProperty, value);
        }

        public static IEnumerable? GetSelectedItems(DependencyObject elementName)
        {
            return (IEnumerable)elementName.GetValue(SelectedItemsProperty);
        }

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
