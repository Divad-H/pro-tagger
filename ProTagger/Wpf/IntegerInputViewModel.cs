using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ProTagger.Wpf
{
    public class IntegerInputViewModel : INotifyPropertyChanged, IDisposable
    {
        private string _text = "";
        public string Text
        {
            get => _text;
            set
            {
                if (_text == value)
                    return;
                _text = value;
                NotifyPropertyChanged();
            }
        }

        private bool _valid = true;
        public bool Valid
        {
            get => _valid;
            set
            {
                if (_valid == value)
                    return;
                _valid = value;
                NotifyPropertyChanged();
            }
        }

        public ICommand Increase { get; }
        public ICommand Decrease { get; }

        public IObservable<int> ValueObservable { get; }

        public IntegerInputViewModel(ISchedulers schedulers, int value, int min = int.MinValue, int max = int.MaxValue)
        {
            if (value > max || value < min)
                throw new ArgumentException($"{nameof(value)} out of bounds: {value}.", nameof(value));
            _text = value.ToString();

            var textObservable = this
                .FromProperty(vm => vm.Text);

            ValueObservable = textObservable
                .Select(text => int.TryParse(text, out int res) ?  (int?)res : null)
                .SkipNull();

            textObservable
                .Subscribe(text => Valid = int.TryParse(text, out var _))
                .DisposeWith(_disposable);

            var increase = ReactiveCommand.Create<object, object>(
                    canExecute: ValueObservable
                        .Select(newVal => newVal < max),
                    execute: (param) => param,
                    scheduler: schedulers.Dispatcher)
                .DisposeWith(_disposable);

            Increase = increase;

            increase
                .WithLatestFrom(ValueObservable, (_, newVal) => newVal)
                .Subscribe(newVal => Text = (newVal + 1).ToString())
                .DisposeWith(_disposable);

            var decrease = ReactiveCommand.Create<object, object>(
                    canExecute: ValueObservable
                        .Select(newVal => newVal > min),
                    execute: (param) => param,
                    scheduler: schedulers.Dispatcher)
                .DisposeWith(_disposable);

            Decrease = decrease;

            decrease
                .WithLatestFrom(ValueObservable, (_, newVal) => newVal)
                .Subscribe(newVal => Text = (newVal - 1).ToString())
                .DisposeWith(_disposable);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
