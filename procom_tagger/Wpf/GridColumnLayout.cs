using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace procom_tagger.Wpf
{
    public static class FillColumn
    {
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(FillColumn));

        public static void SetEnabled(DependencyObject dependencyObject, bool enabled)
        {
            dependencyObject.SetValue(EnabledProperty, enabled);
        }

        public static bool IsFillColumn(GridViewColumn column)
        {
            if (column == null)
                return false;
            object value = column.ReadLocalValue(EnabledProperty);
            return value != null && value.GetType() == EnabledProperty.PropertyType;
        }
    }

    public class GridColumnLayout
    {
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(bool),
            typeof(GridColumnLayout),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnGridColumnLayoutEnabledChanged)));

        public static void SetEnabled(DependencyObject dependencyObject, bool enabled)
        {
            dependencyObject.SetValue(EnabledProperty, enabled);
        }

        private static void OnGridColumnLayoutEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView && (bool)e.NewValue)
                new GridColumnLayout(listView);
        }

        public GridColumnLayout(ListView listView)
        {
            ListView = listView ?? throw new ArgumentNullException(nameof(listView));
            ListView.Loaded += new RoutedEventHandler(ListViewLoaded);
            ListView.Unloaded += new RoutedEventHandler(ListViewUnloaded);
        }

        const double _minWidth = 55;

        bool _resizing = false;
        void ResizeColumn(GridViewColumn? fillColumn = null, GridViewColumn? changedColumn = null)
        {
            if (_resizing)
                return;
            _resizing = true;
            try
            {
                if (!(ListView.View is GridView view))
                    return;
                if (view.Columns.Count == 0)
                    return;
                var totalWidth = ListView.ActualWidth - 20;
                var occupiedWidth = 0.0;
                foreach (var column in view.Columns)
                {
                    occupiedWidth += column.ActualWidth;
                    if (fillColumn == null && FillColumn.IsFillColumn(column))
                        fillColumn = column;
                }
                if (fillColumn == null)
                    throw new InvalidOperationException("No column marked as FillColumn");
                double desiredWidth = fillColumn.ActualWidth + totalWidth - occupiedWidth;
                if (Math.Abs(fillColumn.ActualWidth - desiredWidth) > 1)
                {
                    if (desiredWidth >= _minWidth)
                        fillColumn.Width = desiredWidth;
                    else
                    {
                        fillColumn.Width = _minWidth;
                        if (changedColumn != null)
                        {
                            var x = changedColumn.ActualWidth - (desiredWidth - _minWidth);
                            changedColumn.Width = Math.Max(_minWidth, changedColumn.ActualWidth + (desiredWidth - _minWidth));
                        }
                        else
                        {
                            double remainingReduction = -desiredWidth + _minWidth;
                            foreach (var column in _columnHeaders)
                            {
                                if (column.Column == null || column.Column == fillColumn)
                                    continue;
                                double availiableReduction = column.Column.ActualWidth - _minWidth;
                                if (availiableReduction > remainingReduction)
                                {
                                    column.Column.Width = column.Column.ActualWidth - remainingReduction;
                                    break;
                                }
                                else
                                {
                                    remainingReduction -= column.Column.ActualWidth - _minWidth;
                                    column.Column.Width = _minWidth;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                _resizing = false;
            }
        }

        bool _loaded = false;
        private void ListViewLoaded(object sender, RoutedEventArgs e)
        {
            ListView.SizeChanged += OnSizeChanged;
            ResizeColumn();
            RegisterColumnEvents(ListView);
            _loaded = true;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeColumn();
        }

        List<GridViewColumnHeader> _columnHeaders = new List<GridViewColumnHeader>();
        private void RegisterColumnEvents(DependencyObject recursiveObject)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(recursiveObject); i++)
            {
                if (VisualTreeHelper.GetChild(recursiveObject, i) is Visual childVisual)
                {
                    if (childVisual is GridViewColumnHeader columnHeader)
                    {
                        columnHeader.SizeChanged += OnColumnSizeChanged;
                        columnHeader.PreviewMouseDoubleClick += OnPreviewMouseDoubleClick;
                        _columnHeaders.Add(columnHeader);
                    }
                    else
                    {
                        RegisterColumnEvents(childVisual);
                    }
                }
            }
        }

        private void OnPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void OnColumnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width <= _minWidth)
            {
                if (sender is GridViewColumnHeader header)
                {
                    e.Handled = true;
                    if (header.Column != null)
                        header.Column.Width = _minWidth;
                    return;
                }
            }
            if (sender is GridViewColumnHeader changedColumn)
            {
                double smallestBigger = double.PositiveInfinity;
                if (changedColumn.DesiredSize.Width < 1)
                    return;
                GridViewColumnHeader? columnToChange = null;
                foreach (var column in _columnHeaders)
                {
                    if (column.Column == null || column == changedColumn)
                        continue;
                    var relativePoint = column.TranslatePoint(new Point(0, 0), changedColumn);
                    if (relativePoint.X > 0 && relativePoint.X < smallestBigger)
                    {
                        smallestBigger = relativePoint.X;
                        columnToChange = column;
                    }
                }
                ResizeColumn(columnToChange?.Column ?? changedColumn.Column, changedColumn.Column);
            }
        }

        private void ListViewUnloaded(object sender, RoutedEventArgs e)
        {
            if (!_loaded)
                return;
            ListView.SizeChanged -= OnSizeChanged;
            _loaded = false;
        }

        public ListView ListView { get; }
    }
}
