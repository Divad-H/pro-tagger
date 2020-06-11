using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProTagger.Wpf
{
    public class HideEqualSizesConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return DependencyProperty.UnsetValue;
            if (!(values[0] is double first))
                return DependencyProperty.UnsetValue;
            if (!(values[1] is double second))
                return DependencyProperty.UnsetValue;
            return Math.Abs(first - second) < 1e-5 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
