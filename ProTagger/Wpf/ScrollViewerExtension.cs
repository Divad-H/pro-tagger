using System;
using System.Windows;
using System.Windows.Controls;

namespace ProTagger.Wpf
{
    public static class ScrollViewerExtension
    {
        public static readonly DependencyProperty UseHorizontalScrollingProperty = DependencyProperty.RegisterAttached(
            "UseHorizontalScrolling", typeof(bool), typeof(ScrollViewer), new PropertyMetadata(default(bool), UseHorizontalScrollingChangedCallback));

        private static void UseHorizontalScrollingChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            ScrollViewer? scrollViewer = dependencyObject as ScrollViewer;
            _ = scrollViewer ?? throw new ArgumentException("Element is not a ScrollViewer");

            scrollViewer.PreviewMouseWheel += (sender, args) => 
            {
                if (args.Delta < 0)
                    scrollViewer.LineRight();
                else
                    scrollViewer.LineLeft();
            };
        }

        public static void SetUseHorizontalScrolling(ScrollViewer element, bool value)
            => element.SetValue(UseHorizontalScrollingProperty, value);

        public static bool GetUseHorizontalScrolling(ScrollViewer element)
            => (bool)element.GetValue(UseHorizontalScrollingProperty);
    }
}
