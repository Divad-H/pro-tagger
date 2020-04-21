using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace ProTagger.Wpf
{
    /// <summary>
    /// Simple single line label optimized for performance.
    /// </summary>
    public class SimpleLabel : FrameworkElement
    {
        /// <summary>
        /// DependencyProperty for <see cref="Text" /> property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
                DependencyProperty.Register(
                        nameof(Text),
                        typeof(string),
                        typeof(SimpleLabel),
                        new FrameworkPropertyMetadata(
                                string.Empty,
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                null,
                                new CoerceValueCallback(CoerceText)));

        /// <summary>
        /// The Text property defines the content (text) to be displayed.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static object CoerceText(DependencyObject d, object value)
           => value ?? string.Empty;

        public Typeface Typeface
        {
            get => new Typeface("Consolas");
            set => throw new NotImplementedException();
        }

        public double Fontsize
        {
            get => 12.0;
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// DependencyProperty for <see cref="Foreground" /> property.
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty =
                DependencyProperty.Register(
                        nameof(Foreground),
                        typeof(Brush),
                        typeof(SimpleLabel),
                        new FrameworkPropertyMetadata(
                                new SolidColorBrush(Colors.Black),
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The Foreground property for the Text.
        /// </summary>
        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var formattedText = new FormattedText(
                Text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface,
                Fontsize,
                Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(formattedText, new Point(0, 0));
        }
    }
}
