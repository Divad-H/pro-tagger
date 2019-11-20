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
            public Schedulers(DispatcherScheduler dispatcher)
            {
                _dispatcher = dispatcher;
            }

            private readonly DispatcherScheduler _dispatcher;
            public IScheduler Dispatcher => _dispatcher;
        }

        private class RepositoryFactory : IRepositoryFactory
        {
            public IRepositoryWrapper CreateRepository(string path)
            {
                return new RepositoryWrapper(path);
            }
        }

        public MainWindow()
        {
            DataContext = new PTagger(new RepositoryFactory(), new Schedulers(DispatcherScheduler.Current));
            InitializeComponent();
        }
    }
}
