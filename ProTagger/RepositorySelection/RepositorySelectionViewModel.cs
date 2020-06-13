using ProTagger.Utilities;
using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;

namespace ProTagger.RepositorySelection
{
    public class RepositorySelectionViewModel : IDisposable
    {
        public static RepositoryDescription RepositoryDescriptionFromPath(IRepositoryFactory repositoryFactory, string path)
            => new RepositoryDescription(repositoryFactory.RepositoryNameFromPath(path) ?? throw new InvalidOperationException($"{path} is not a valid git repository."), path);

        public ViewSubject<string> RepositoryPath { get; }

        public ICommand SelectFromFilesystemCommand { get; }
        public ICommand OpenCommand { get; }
        public IObservable<RepositoryDescription> OpenRepository { get; }

        public RepositorySelectionViewModel(ISchedulers schedulers, IRepositoryFactory repositoryFactory, IFileSystem fileSystemService)
        {
            bool pathCanDiscoverRepository(string path)
            {
                var repositoryDirectory = repositoryFactory.DiscoverRepository(path);
                if (repositoryDirectory != null)
                    return repositoryFactory.IsValidRepository(repositoryDirectory);
                return false;
            }

            RepositoryPath = new ViewSubject<string>(@"G:\Projects\pro-tagger")
                .DisposeWith(_disposables);

            var repoValid = RepositoryPath
                .Select(pathCanDiscoverRepository);

            var openCommand = ReactiveCommand.Create<object, object>(repoValid, p => p, schedulers.Dispatcher)
                .DisposeWith(_disposables);
            OpenCommand = openCommand;

            var selectFromFilesystemCommand = ReactiveCommand.Create<object, object>(Observable.Return(true), p => p, schedulers.Dispatcher)
                .DisposeWith(_disposables);
            SelectFromFilesystemCommand = selectFromFilesystemCommand;

            var repoFileNameObservable = selectFromFilesystemCommand
                .Select(_ => fileSystemService.SelectGitRepositoryDialog("Select a git repository", pathCanDiscoverRepository))
                .SkipNull()
                .Select(repositoryFactory.DiscoverRepository)
                .SkipNull();

            repoFileNameObservable
                .Subscribe(RepositoryPath)
                .DisposeWith(_disposables);

            OpenRepository = openCommand
                .WithLatestFrom(RepositoryPath, (_, path) => path)
                .Select(repositoryFactory.DiscoverRepository)
                .SkipNull()
                .Select(path => RepositoryDescriptionFromPath(repositoryFactory, path));
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
            => _disposables.Dispose();
    }
}
