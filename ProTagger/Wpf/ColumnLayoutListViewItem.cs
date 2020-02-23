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
        {
            _listView = listView;
        }

        public bool IsSecondarySelected
        {
            get { return (bool)GetValue(IsSecondarySelectedProperty); }
            set { SetValue(IsSecondarySelectedProperty, value); }
        }
        public static readonly DependencyProperty IsSecondarySelectedProperty = DependencyProperty.Register(
          nameof(IsSecondarySelected), typeof(bool), typeof(ColumnLayoutListViewItem), new FrameworkPropertyMetadata(false));


        private bool IsSecondarySelection(MouseButtonEventArgs args)
        {
            return args.ChangedButton == MouseButton.Right;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (_listView == null)
                throw new ArgumentNullException(nameof(_listView));
            if (IsSecondarySelection(e))
            {
                if (!IsSelected)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        _listView.ToggleSecondarySelectItem(this);
                    else
                        _listView.SecondarySelectItem(this);
                }
                e.Handled = true;
                return;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_listView == null)
                throw new ArgumentNullException(nameof(_listView));
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (!IsSelected)
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                    _listView.SecondarySelectItem(this);
                e.Handled = true;
                return;
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (_listView == null)
                throw new ArgumentNullException(nameof(_listView));
            if (IsSecondarySelection(e))
            {
                if (!IsSelected)
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                    _listView.SecondarySelectItem(this);
                e.Handled = true;
                return;
            }
            base.OnMouseUp(e);
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            if (IsSecondarySelected)
                _listView?.SecondarySelectItem(null);
            base.OnSelected(e);
        }
    }
}
