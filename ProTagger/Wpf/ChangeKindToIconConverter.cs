using LibGit2Sharp;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProTagger.Wpf
{
    class ChangeKindToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ChangeKind changeKind))
                return DependencyProperty.UnsetValue;
            return changeKind switch
            {
                ChangeKind.Unmodified => "\uE73E",
                ChangeKind.Added => "\uE948",
                ChangeKind.Deleted => "\uE74D",
                ChangeKind.Modified => "\uEB7E",
                ChangeKind.Renamed => "\uE8AC",
                ChangeKind.Copied => "\uE8C8",
                ChangeKind.Ignored => "\uE7B3",
                ChangeKind.Untracked => "\uF142",
                ChangeKind.TypeChanged => "\uE8AB",
                ChangeKind.Unreadable => "\uE7BA",
                ChangeKind.Conflicted => "\uE945",
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
