using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ReactiveMvvm
{
    public class ReactiveCommand<TIn, TOut> : ICommand, IObservable<TOut>, IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        private readonly ISubject<int> _executionCountSubject = new BehaviorSubject<int>(0);
        private readonly ISubject<TIn, TOut> _executeSubject;

        private bool _canExecute = true;

        public ReactiveCommand(
            IObservable<bool> canExecuteObservable,
            ISubject<TIn, TOut> executeSubject,
            IScheduler scheduler)
        {
            canExecuteObservable
                .CombineLatest(
                    _executionCountSubject.Scan((a, b) => a + b),
                    (canExec, isExec) => isExec == 0 ? canExec : false)
                .ObserveOn(scheduler)
                .Subscribe(p =>
                {
                    _canExecute = p;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                })
                .DisposeWith(_disposable);

            _executeSubject = Subject.Create(
                executeSubject,
                executeSubject.Publish().Connect(_disposable));

            _executeSubject
                .Subscribe(
                    _ => _executionCountSubject.OnNext(-1),
                    _ => _executionCountSubject.OnNext(-1),
                    () => _executionCountSubject.OnNext(-1))
                .DisposeWith(_disposable);
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object parameter)
            => _canExecute;

        public void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                return;
            _executionCountSubject.OnNext(1);
            _executeSubject.OnNext((TIn)parameter);
        }

        public IDisposable Subscribe(IObserver<TOut> observer)
            => _executeSubject.Subscribe(observer);

        public void Dispose()
            => _disposable.Dispose();
    }

    public static class ReactiveCommand
    {
        public static ReactiveCommand<TIn, TOut> Create<TIn, TOut>(
            IObservable<bool> canExecute,
            Func<TIn, CancellationToken, Task<TOut>> execute,
            IScheduler scheduler)
        {
            var executeSubject = new Subject<TIn>();
            var resultObservable = executeSubject
                .Select(p => Observable.FromAsync(ct => execute(p, ct)))
                .Switch();

            return new ReactiveCommand<TIn, TOut>(
                canExecute,
                Subject.Create(executeSubject, resultObservable),
                scheduler);
        }

        public static ReactiveCommand<TIn, TOut> Create<TIn, TOut>(
            IObservable<bool> canExecute,
            Func<TIn, TOut> execute,
            IScheduler scheduler)
        {
            var executeSubject = new Subject<TIn>();
            var resultObservable = executeSubject
                .Select(p => execute(p));

            return new ReactiveCommand<TIn, TOut>(
                canExecute,
                Subject.Create(executeSubject, resultObservable),
                scheduler);
        }
    }
}
