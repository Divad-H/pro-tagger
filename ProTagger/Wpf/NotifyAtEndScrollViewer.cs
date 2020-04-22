using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProTagger.Wpf
{
    class NotifyAtEndScrollViewer : ScrollViewer
    {
        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            if (e.VerticalChange > 0 && e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
                ScrolledBottomCommand?.Execute(null);
            base.OnScrollChanged(e);
        }

        public static readonly DependencyProperty ScrolledBottomCommandProperty = DependencyProperty.Register(
          nameof(ScrolledBottomCommand), typeof(ICommand), typeof(NotifyAtEndScrollViewer),
          new FrameworkPropertyMetadata(null));

        public ICommand? ScrolledBottomCommand
        {
            get => (ICommand?)GetValue(ScrolledBottomCommandProperty);
            set => SetValue(ScrolledBottomCommandProperty, value);
        }
    }
}
