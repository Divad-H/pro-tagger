using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace ReacitveMvvm
{
    public class ViewObservable<T> : IObservable<T>
    {
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(value, _value))
                {
                    _value = value;
                    ValueChanged?.Invoke(value);
                }
            }
        }

        private delegate void ValueChangedHandler(T value);
        private event ValueChangedHandler? ValueChanged;

        public ViewObservable(T value)
            => _value = value;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ValueChanged += observer.OnNext;
            observer.OnNext(_value);
            return Disposable.Create(() => ValueChanged -= observer.OnNext);
        }
    }
}
