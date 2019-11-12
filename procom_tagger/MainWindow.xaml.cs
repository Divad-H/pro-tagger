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

        public MainWindow()
        {
            DataContext = new ProcomTagger(new Schedulers());
            InitializeComponent();
        }
    }
}
