using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace ProTagger.Utilities
{
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

        public void Modify(IEnumerable itemsToAdd, IEnumerable itemsToRemove, bool supportsRangeOperations)
        {
            Modify(itemsToAdd.Cast<T>(), itemsToRemove.Cast<T>(), supportsRangeOperations);
        }
        public void ModifyNoDuplicates(IEnumerable itemsToAdd, IEnumerable itemsToRemove, bool supportsRangeOperations)
        {
            ModifyNoDuplicates(itemsToAdd.Cast<T>(), itemsToRemove.Cast<T>(), supportsRangeOperations);
        }

        public void Modify(IEnumerable<T> itemsToAdd, IEnumerable<T> itemsToRemove, bool supportsRangeOperations)
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
                CollectionChanged?.Invoke(this, ReplaceIfUnsupported(supportsRangeOperations,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itemsToAdd.ToList())));
                return;
            }
            if (!itemsToAdd.Any())
            {
                CollectionChanged?.Invoke(this, ReplaceIfUnsupported(supportsRangeOperations,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, itemsToRemove.ToList())));
                return;
            }
            CollectionChanged?.Invoke(this, ReplaceIfUnsupported(supportsRangeOperations,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, itemsToAdd.ToList(), itemsToRemove.ToList())));
        }

        public void ModifyNoDuplicates(IEnumerable<T> itemsToAdd, IEnumerable<T> itemsToRemove, bool supportsRangeOperations)
        {
            Modify(itemsToAdd.Where(item => !_list.Contains(item)).ToList(), itemsToRemove, supportsRangeOperations);
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
            return RemoveAll(match, true);
        }

        public int RemoveAll(Func<T, bool> match, bool supportsRangeOperations)
        {
            var itemsToRemove = _list.Where(match).ToList();
            if (!itemsToRemove.Any())
                return 0;
            foreach (var item in itemsToRemove)
                _list.Remove(item);
            CollectionChanged?.Invoke(this, ReplaceIfUnsupported(supportsRangeOperations,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, itemsToRemove)));
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

        private NotifyCollectionChangedEventArgs ReplaceIfUnsupported(bool supportsRangeOperations, NotifyCollectionChangedEventArgs args)
        {
            if (supportsRangeOperations)
                return args;
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add when args.NewItems.Count != 1:
                case NotifyCollectionChangedAction.Remove when args.OldItems.Count != 1:
                case NotifyCollectionChangedAction.Replace when args.NewItems.Count != 1 || args.OldItems.Count != 1:
                case NotifyCollectionChangedAction.Move when args.NewItems.Count != 1:
                    return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                default:
                    return args;
            }
        }
    }
}
