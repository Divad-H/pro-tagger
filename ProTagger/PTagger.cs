using static ReactiveMvvm.DisposableExtensions;
using static ReacitveMvvm.ObservableExtensions;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveMvvm;
using ReacitveMvvm;
using ProTagger.Utilities;
using LibGit2Sharp;
using System.Collections.ObjectModel;
using ProTagger.RepositorySelection;
using ProTagger.Repository;

namespace ProTagger
{
    using TabType = Variant<Variant<RepositoryViewModel, RepositoryError>, RepositorySelectionViewModel>;
    public class PTagger : IDisposable
    {
        public ObservableCollection<TabType> OpenedRepositories { get; }
          = new ObservableCollection<TabType>();

        public ICommand NewTabCommand { get; }
        public ICommand CloseTabCommand { get; }

        public Configuration.CompareOptionsViewModel CompareOptions { get; }

        private static void DisposeTab(TabType  tab)
            => tab.Visit(repoVar => repoVar.Visit(repo => repo.Dispose(), _ => { }), repoSel => repoSel.Dispose());

        public PTagger(IRepositoryFactory repositoryFactory, ISchedulers schedulers, IFileSystem fileSystemService, IObservable<string> pushPathObservable)
        {
            CompareOptions = new Configuration.CompareOptionsViewModel(schedulers, new CompareOptions() { Similarity = SimilarityOptions.Default });

            var newTabCommand = ReactiveCommand
                .Create<object?, object?>(p => p, schedulers.Dispatcher)
                .DisposeWith(_disposable);
            NewTabCommand = newTabCommand;

            var newTabObservable = newTabCommand
                .StartWith((object?)null)
                .ObserveOn(schedulers.Dispatcher)
                .Select(_ => new RepositorySelectionViewModel(schedulers, repositoryFactory, fileSystemService))
                .Publish();

            var openRepositoryFromTab = newTabObservable
                .SelectMany(newTab => newTab.OpenRepository
                    .Select(openRepository => new { newTab, openRepository }));

            var pushedRepositoryDesc = pushPathObservable
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(repositoryFactory.DiscoverRepository)
                .SkipNull()
                .Select(path => RepositorySelectionViewModel.RepositoryDescriptionFromPath(repositoryFactory, path));

            var openRepository = openRepositoryFromTab
                .Select(openRepo => openRepo.openRepository)
                .Merge(pushedRepositoryDesc)
                .SelectMany(desc => Observable
                    .FromAsync(ct => RepositoryViewModel.Create(schedulers, ct, repositoryFactory, desc, CompareOptions.CompareOptionsObservable)))
                .SkipNull()
                .Publish();

            var openTabObservable = newTabObservable
                .Select(newTab => new TabType(newTab))
                .Merge(openRepository
                    .Select(newRepo => new TabType(newRepo))
                    .ObserveOn(schedulers.Dispatcher));

            var closeTabCommand = ReactiveCommand
                .Create<TabType, TabType>(tab => tab, schedulers.Dispatcher)
                .DisposeWith(_disposable);
            CloseTabCommand = closeTabCommand;

            var closeTabObservable = openRepositoryFromTab
                .Select(openRepo => new TabType(openRepo.newTab))
                .Merge(closeTabCommand);

            openTabObservable
                .Subscribe(tab => OpenedRepositories.Add(tab))
                .DisposeWith(_disposable);

            closeTabObservable
                .Subscribe(tab =>
                {
                    OpenedRepositories.Remove(tab);
                    DisposeTab(tab);
                })
                .DisposeWith(_disposable);

            openRepository
                .Connect()
                .DisposeWith(_disposable);

            newTabObservable
                .Connect()
                .DisposeWith(_disposable);
        }

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public void Dispose()
        {
            foreach (var tab in OpenedRepositories)
                DisposeTab(tab);
            _disposable.Dispose();
        }
    }
}
