using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ProTagger.Wpf
{
    public class BoolToBrushConverter : IValueConverter
    {
        public SolidColorBrush? TrueBrush { get; set; }
        public SolidColorBrush? FalseBrush { get; set; }

        public object? Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? TrueBrush : FalseBrush;
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
