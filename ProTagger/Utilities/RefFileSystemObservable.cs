using ReactiveMvvm;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ProTagger.Utilities
{
    class RefFileSystemObservable : IObservable<object?>
    {
        readonly string _gitDirectory;
        readonly IFileSystem _fileSystem;

        public RefFileSystemObservable(string gitDirectory, IFileSystem fileSystem)
            => (_gitDirectory, _fileSystem) = (gitDirectory, fileSystem);

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            var disposable = new CompositeDisposable();
            var refsWatcher = _fileSystem
                .CreateFileSystemWatcher(System.IO.Path.Combine(_gitDirectory, "refs"), "")
                .DisposeWith(disposable);
            var headWatcher = _fileSystem
                .CreateFileSystemWatcher(_gitDirectory, "HEAD")
                .DisposeWith(disposable);
            Observable.FromEventPattern<EventHandler<object?>, object?>(
                    h => refsWatcher.Changed += h,
                    h => refsWatcher.Changed -= h)
                .Merge(Observable.FromEventPattern<EventHandler<object?>, object?>(
                        h => headWatcher.Changed += h,
                        h => headWatcher.Changed -= h))
                .Select(args => args.EventArgs)
                .Subscribe(observer)
                .DisposeWith(disposable);
            return disposable;
        }
    }
}
