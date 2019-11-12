using procom_tagger.Utilities;
using ReacitveMvvm;
using System.Reactive.Concurrency;
using System.Windows;

namespace procom_tagger
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
            DataContext = new ProcomTagger(new RepositoryFactory(), new Schedulers());
            InitializeComponent();
        }
    }
}
