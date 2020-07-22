using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProTagger.Wpf
{
    class AdditionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return DependencyProperty.UnsetValue;
            if (!(values[0] is int first))
                return DependencyProperty.UnsetValue;
            if (!(values[1] is int second))
                return DependencyProperty.UnsetValue;
            var result = first + second;
            for (int i = 2; i < values.Length; ++i)
            {
                if (!(values[i] is int val))
                    return DependencyProperty.UnsetValue;
                result += val;
            }
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
