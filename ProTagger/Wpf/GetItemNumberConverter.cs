using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProTagger.Wpf
{
    public class GetItemNumberConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return DependencyProperty.UnsetValue;
            if (!(values[1] is IList list))
                return DependencyProperty.UnsetValue;
            return list.IndexOf(values[0]) + 1;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
