using System.Windows;
using System.Windows.Media;

namespace ProTagger.Wpf
{
    public enum ClippingType
    {
        All,
        Horizontal,
        Vertical,
        None,
    }

    public class BoundsClipper
    {
        const double InfiniteDistance = 100000;
        public static ClippingType GetClippingEnabled(DependencyObject obj)
        {
            return (ClippingType)obj.GetValue(ClippingEnabledProperty);
        }

        public static void SetClippingEnabled(DependencyObject obj, ClippingType value)
        {
            obj.SetValue(ClippingEnabledProperty, value);
        }

        public static readonly DependencyProperty ClippingEnabledProperty =
            DependencyProperty.RegisterAttached("ClippingEnabled", typeof(ClippingType), typeof(BoundsClipper), new PropertyMetadata(ClippingType.None, ClippingEnabledChanged));

        private static void ClippingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                element.Loaded += (s, evt) => ClipElement(element);
                element.SizeChanged += (s, evt) => ClipElement(element);
            }
        }

        private static void ClipElement(FrameworkElement element)
        {
            var clipping = GetClippingEnabled(element);
            switch(clipping)
            {
                case ClippingType.All:
                    element.Clip = new RectangleGeometry { Rect = new Rect(0, 0, element.ActualWidth, element.ActualHeight) };
                    break;
                case ClippingType.Horizontal:
                    element.Clip = new RectangleGeometry { Rect = new Rect(0, 0, element.ActualWidth, 2 * InfiniteDistance), Transform = new TranslateTransform(0, -InfiniteDistance) };
                    break;
                case ClippingType.Vertical:
                    element.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 2 * InfiniteDistance, element.ActualHeight), Transform = new TranslateTransform(-InfiniteDistance, 0) };
                    break;
                case ClippingType.None:
                    element.Clip = null;
                    break;
            }
        }
    }
}
