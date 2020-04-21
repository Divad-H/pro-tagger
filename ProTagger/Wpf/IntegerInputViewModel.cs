using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;

namespace ProTagger.Wpf
{
    public class IntegerInputViewModel : IDisposable
    {
        public ViewSubject<string> Text { get; }
        public SubjectBase<bool> Valid { get; }
        public ICommand Increase { get; }
        public ICommand Decrease { get; }

        public IObservable<int> Value { get; }

        public IntegerInputViewModel(ISchedulers schedulers, int value, int min = int.MinValue, int max = int.MaxValue)
        {
            if (value > max || value < min)
                throw new ArgumentException($"{nameof(value)} out of bounds: {value}.", nameof(value));
            Text = new ViewSubject<string>(value.ToString())
                .DisposeWith(_disposable);
            Value = Text
                .Select(text => int.TryParse(text, out int res) ?  (int?)res : null)
                .SkipNull();
            Valid = new ViewSubject<bool>(false)
                .DisposeWith(_disposable);
            Text
                .Select(text => int.TryParse(text, out var _))
                .Subscribe(Valid)
                .DisposeWith(_disposable);

            var increase = ReactiveCommand.Create<object, object>(
                    canExecute: Value
                        .Select(newVal => newVal < max),
                    execute: (param) => param,
                    scheduler: schedulers.Dispatcher)
                .DisposeWith(_disposable);

            Increase = increase;

            increase
                .WithLatestFrom(Value, (_, newVal) => (newVal + 1).ToString())
                .Subscribe(Text)
                .DisposeWith(_disposable);

            var decrease = ReactiveCommand.Create<object, object>(
                    canExecute: Value
                        .Select(newVal => newVal > min),
                    execute: (param) => param,
                    scheduler: schedulers.Dispatcher)
                .DisposeWith(_disposable);

            Decrease = decrease;

            decrease
                .WithLatestFrom(Value, (_, newVal) => (newVal - 1).ToString())
                .Subscribe(Text)
                .DisposeWith(_disposable);
        }

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public void Dispose()
            => _disposable.Dispose();
    }
}
