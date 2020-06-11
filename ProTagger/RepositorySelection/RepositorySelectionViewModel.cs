using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;

namespace ProTagger.RepositorySelection
{
    public class RepositorySelectionViewModel : IDisposable
    {
        public ViewSubject<string> RepositoryPath { get; }

        public ICommand OpenCommand { get; }
        public IObservable<RepositoryDescription> OpenRepository { get; }

        public RepositorySelectionViewModel(ISchedulers schedulers)
        {
            RepositoryPath = new ViewSubject<string>(@"G:\Projects\pro-tagger")
                .DisposeWith(_disposables);

            var repoValid = RepositoryPath
                .Select(path => !string.IsNullOrWhiteSpace(path));

            var openCommand = ReactiveCommand.Create<object, object>(repoValid, p => p, schedulers.Dispatcher)
                .DisposeWith(_disposables);
            OpenCommand = openCommand;

            OpenRepository = openCommand
                .WithLatestFrom(RepositoryPath, (_, path) => path)
                .Select(path => new RepositoryDescription(new DirectoryInfo(path).Name, path));
        }

        private CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
            => _disposables.Dispose();
    }
}
