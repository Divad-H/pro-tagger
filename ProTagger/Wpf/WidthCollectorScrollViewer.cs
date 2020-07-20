using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProTagger.Wpf
{
    public class WidthCollectorScrollViewer : ScrollViewer
    {
        public static double GetValue(DependencyObject obj)
            => (double)obj.GetValue(ValueProperty);

        public static void SetValue(DependencyObject obj, double value)
            => obj.SetValue(ValueProperty, value);

        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.RegisterAttached("Value", typeof(double), typeof(WidthCollectorScrollViewer), new PropertyMetadata(0.0, OnValueChanged));

        private static WidthCollectorScrollViewer? FindParentWidthCollectorBorder(DependencyObject d)
        {
            if (d is null)
                return null;
            var parent = VisualTreeHelper.GetParent(d);
            if (parent is WidthCollectorScrollViewer result)
                return result;
            return FindParentWidthCollectorBorder(parent);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement frameworkElement))
                throw new InvalidOperationException($"{nameof(WidthCollectorScrollViewer)}.Value can only be set on {nameof(FrameworkElement)}s");
            var parentBorder = FindParentWidthCollectorBorder(d);
            if (parentBorder is null)
                return;
            parentBorder.ValueChanged(frameworkElement, (double)e.NewValue);
        }

        public double ContentWidth
        {
            get => (double)GetValue(ContentWidthProperty);
            set => SetValue(ContentWidthProperty, value);
        }
        public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register(
            "ContentWidth", typeof(double), typeof(WidthCollectorScrollViewer), new PropertyMetadata(10.0));

        public double CustomHorizontalOffset
        {
            get => (double)GetValue(CustomHorizontalOffsetProperty);
            set => SetValue(CustomHorizontalOffsetProperty, value);
        }
        public static readonly DependencyProperty CustomHorizontalOffsetProperty = DependencyProperty.Register(
            "CustomHorizontalOffset", typeof(double), typeof(WidthCollectorScrollViewer), new PropertyMetadata(0.0));

        private readonly Dictionary<FrameworkElement, double> _elements = new Dictionary<FrameworkElement, double>();
        private void ValueChanged(FrameworkElement element, double value)
        {
            if (!_elements.TryGetValue(element, out double oldValue))
                element.Unloaded += OnElementUnloaded;
            else if (oldValue == value)
                return;
            _elements[element] = value;
            RecomputeMaximum();
        }

        private void OnElementUnloaded(object sender, RoutedEventArgs e)
        {
            _elements.Remove((FrameworkElement)sender);
            RecomputeMaximum();
        }

        private void RecomputeMaximum()
        {
            var oldContentWidth = ContentWidth;
            ContentWidth = _elements.Any() ? _elements.Values.Max() : 0.0;
            if (ContentWidth < oldContentWidth)
                CustomHorizontalOffset = 0.0;
        }
    }
}
