using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;

namespace ReacitveMvvm
{
    public class ViewSubject<T> : SubjectBase<T>, INotifyPropertyChanged
    {
        private readonly object _gate = new object();

        private ImmutableList<IObserver<T>>? _observers = ImmutableList<IObserver<T>>.Empty;
        private bool _stopped = false;
        private T _value;
        private Exception? _exception;

        public ViewSubject(T value)
            => _value = value;

        public override bool HasObservers => _observers?.Count > 0;

        public T Value
        {
            get
            {
                lock (_gate)
                {
                    CheckDisposed();
                    if (_exception != null)
                        throw _exception;
                    return _value;
                }
            }
            set
            {
                OnNext(value);
            }
        }

        public override void OnCompleted()
        {
            var os = default(IObserver<T>[]);
            lock (_gate)
            {
                CheckDisposed();
                if (!_stopped)
                {
                    os = _observers.ToArray();
                    _observers = ImmutableList<IObserver<T>>.Empty;
                    _stopped = true;
                }
            }

            if (os != null)
                foreach (var o in os)
                    o.OnCompleted();
        }

        public override void OnError(Exception error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            var os = default(IObserver<T>[]);
            lock (_gate)
            {
                CheckDisposed();
                if (!_stopped)
                {
                    os = _observers.ToArray();
                    _observers = ImmutableList<IObserver<T>>.Empty;
                    _stopped = true;
                    _exception = error;
                }
            }

            if (os != null)
                foreach (var o in os)
                    o.OnError(error);
        }

        public override void OnNext(T value)
        {
            var os = default(IObserver<T>[]);
            bool changed = false;
            lock (_gate)
            {
                CheckDisposed();
                if (!_stopped)
                {
                    changed = !EqualityComparer<T>.Default.Equals(value, _value);
                    _value = value;
                    os = _observers.ToArray();
                }
            }

            if (changed)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            if (os != null)
                foreach (var o in os)
                    o.OnNext(value);
        }

        public override IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            var ex = default(Exception);

            lock (_gate)
            {
                CheckDisposed();
                if (!_stopped)
                {
                    _observers = _observers?.Add(observer);
                    observer.OnNext(_value);
                    return new Subscription(this, observer);
                }
                ex = _exception;
            }

            if (ex != null)
                observer.OnError(ex);
            else
                observer.OnCompleted();

            return Disposable.Empty;
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(string.Empty);
        }
        private bool _disposed = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public override bool IsDisposed
        {
            get
            {
                lock (_gate)
                    return _disposed;
            }
        }
        public override void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
                _observers = null;
                _exception = null;
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly ViewSubject<T> _subject;
            private IObserver<T>? _observer;

            public Subscription(ViewSubject<T> subject, IObserver<T> observer)
            {
                _subject = subject;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null)
                {
                    lock (_subject._gate)
                    {
                        if (!_subject._disposed && _observer != null)
                        {
                            _subject._observers = _subject._observers?.Remove(_observer);
                            _observer = null;
                        }
                    }
                }
            }
        }
    }
}
