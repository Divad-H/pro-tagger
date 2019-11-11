using procom_tagger.Repo.GitLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace procom_tagger
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

        public static async Task<RepositoryViewModel?> Create(CancellationToken ct, string path)
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            if (ct.IsCancellationRequested)
                return null;
            return new RepositoryViewModel(path);
        }

        public RepositoryViewModel(string path)
        {
            var selectedBranches = Observable
                .Return(new List<string>() { "master" });
            SelectedBranches = selectedBranches;

            _graph = new LogGraph(path, selectedBranches);
        }

        public IObservable<IEnumerable<string>> SelectedBranches { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
