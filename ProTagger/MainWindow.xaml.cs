using ProTagger.Utilities;
using ReacitveMvvm;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Windows;

[assembly: InternalsVisibleTo("ProTaggerTest")]
namespace ProTagger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class Schedulers : ISchedulers
        {
            public Schedulers(DispatcherScheduler dispatcher, DefaultScheduler threadPool)
                => (_dispatcher, _threadPool) = (dispatcher, threadPool);

            private readonly DispatcherScheduler _dispatcher;
            public IScheduler Dispatcher => _dispatcher;

            private readonly DefaultScheduler _threadPool;
            public IScheduler ThreadPool => _threadPool;
        }

        private class RepositoryFactory : IRepositoryFactory
        {
            public IRepositoryWrapper CreateRepository(string path)
                => new RepositoryWrapper(path);
        }

        public MainWindow()
        {
            DataContext = new PTagger(new RepositoryFactory(), new Schedulers(DispatcherScheduler.Current, Scheduler.Default));
            InitializeComponent();
        }
    }
}
