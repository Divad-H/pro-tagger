using static ReactiveMvvm.DisposableExtensions;
using static ReacitveMvvm.ObservableExtensions;
using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ReactiveMvvm;
using ReacitveMvvm;
using ProTagger.Utilities;
using LibGit2Sharp;

namespace ProTagger
{
    public class PTagger : IDisposable
    {
        public ViewSubject<string> RepositoryPath { get; }
        public ViewSubject<Variant<RepositoryViewModel, string>> Repository { get; }

        public ICommand RefreshCommand { get; }

        public Configuration.CompareOptionsViewModel CompareOptions { get; }

        public PTagger(IRepositoryFactory repositoryFactory, ISchedulers schedulers)
        {
            RepositoryPath = new ViewSubject<string>(@"G:\Projects\pro-tagger")
                .DisposeWith(_disposable);
            Repository = new ViewSubject<Variant<RepositoryViewModel, string>>(
                new Variant<RepositoryViewModel, string>("No repository selected."))
                .DisposeWith(_disposable);

            CompareOptions = new Configuration.CompareOptionsViewModel(schedulers, new CompareOptions() { Similarity = SimilarityOptions.Default });

            var refreshCommand = ReactiveCommand.Create<object, object>(
                    canExecute: RepositoryPath
                        .Select(path => !string.IsNullOrWhiteSpace(path)),
                    execute: (param) =>
                    {
                        return param;
                    },
                    scheduler: schedulers.Dispatcher)
                .DisposeWith(_disposable);

            var repositoryObservable =
                Observable.Create<Variant<RepositoryViewModel, string>>(o =>
                {
                    var serial = new SerialDisposable();
                    return new CompositeDisposable(
                        RepositoryPath
                            .Take(1)
                            .Concat(refreshCommand
                                .ObserveOn(schedulers.Dispatcher)
                                .WithLatestFrom(RepositoryPath, (_, path) => path)
                                .Select(path => path))
                            .Select(path => Observable.FromAsync(ct => RepositoryViewModel.Create(schedulers, ct, repositoryFactory, path, CompareOptions.CompareOptionsObservable)))
                            .Switch()
                            .SkipNull()
                            .Do(x => serial.Disposable = x.Visit(vm => vm, _ => Disposable.Empty))
                            .Subscribe(o),
                        serial);
                });

            repositoryObservable
                .Subscribe(Repository)
                .DisposeWith(_disposable);

            RefreshCommand = refreshCommand;
        }

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public void Dispose()
            => _disposable.Dispose();
    }
}
