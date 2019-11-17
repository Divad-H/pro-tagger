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
            public IScheduler Dispatcher => DispatcherScheduler.Current;
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
            DataContext = new PTagger(new RepositoryFactory(), new Schedulers());
            InitializeComponent();
        }
    }
}
