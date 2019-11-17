using ProTagger.Repo.GitLog;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProTagger
{
    public class RepositoryViewModel : INotifyPropertyChanged, IDisposable
    {
        private LogGraph _graph;
        public LogGraph Graph
        {
            get { return _graph; }
            set
            {
                if (_graph == value)
                    return;
                _graph = value;
                NotifyPropertyChanged();
            }
        }

        public static async Task<RepositoryViewModel?> Create(CancellationToken ct, IRepositoryFactory repositoryFactory, string path)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            if (ct.IsCancellationRequested)
                return null;
            return new RepositoryViewModel(repositoryFactory, path);
        }

        public RepositoryViewModel(IRepositoryFactory repositoryFactory, string path)
        {
            var selectedBranches = Observable
                .Return(new List<string>() { "master" });
            SelectedBranches = selectedBranches;

            _graph = new LogGraph(repositoryFactory, path, selectedBranches);
        }

        public IObservable<IEnumerable<string>> SelectedBranches { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
        {
            _graph.Dispose();
            _disposables.Dispose();
        }
    }
}
