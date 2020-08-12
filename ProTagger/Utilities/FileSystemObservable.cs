using ReactiveMvvm;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ProTagger.Utilities
{
    class FileSystemObservable : IObservable<object?>
    {
        readonly string _directory;
        readonly string _filter;
        readonly IFileSystem _fileSystem;

        public FileSystemObservable(string directory, string filter, IFileSystem fileSystem)
            => (_directory, _filter, _fileSystem) = (directory, filter, fileSystem);

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            var disposable = new CompositeDisposable();
            var watcher = _fileSystem
                .CreateFileSystemWatcher(_directory, _filter)
                .DisposeWith(disposable);
            Observable.FromEventPattern<EventHandler<object?>, object?>(
                    h => watcher.Changed += h,
                    h => watcher.Changed -= h)
                .Select(args => args.EventArgs)
                .Subscribe(observer)
                .DisposeWith(disposable);
            return disposable;
        }
    }
}
