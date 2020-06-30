using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProTagger.Wpf
{
    class ColumnLayoutListViewItem : ListViewItem
    {
        private ColumnLayoutListView? _listView;
        internal void AddedToListView(ColumnLayoutListView listView)
            => _listView = listView;

        public bool IsSecondarySelected
        {
            get => (bool)GetValue(IsSecondarySelectedProperty);
            set => SetValue(IsSecondarySelectedProperty, value);
        }
        public static readonly DependencyProperty IsSecondarySelectedProperty = DependencyProperty.Register(
          nameof(IsSecondarySelected), typeof(bool), typeof(ColumnLayoutListViewItem), new FrameworkPropertyMetadata(false));


        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            _ = _listView ?? throw new ArgumentNullException(nameof(_listView));
            if (e.ChangedButton == MouseButton.Left
                && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
                && !IsSelected
                && _listView.SelectedItem != null)
            {
                _listView.ToggleSecondarySelectItem(this);
                e.Handled = true;
                return;
            }
            base.OnMouseDown(e);
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            if (IsSecondarySelected)
                _listView?.SecondarySelectItem(null);
            base.OnSelected(e);
        }
    }
}
