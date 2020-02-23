using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ProTagger.Wpf
{
    public class ColumnLayoutListView : ListView
    {
        public ColumnDefinitionCollection ColumnDefinitions
        {
            get { return (ColumnDefinitionCollection)GetValue(ColumnDefinitionsProperty); }
            set { SetValue(ColumnDefinitionsProperty, value); }
        }
        public static readonly DependencyProperty ColumnDefinitionsProperty = DependencyProperty.Register(
          nameof(ColumnDefinitions), typeof(ColumnDefinitionCollection), typeof(ColumnLayoutListView), new PropertyMetadata(null));

        public object HeaderContent
        {
            get { return GetValue(HeaderContentProperty); }
            set { SetValue(HeaderContentProperty, value); }
        }
        public static readonly DependencyProperty HeaderContentProperty = DependencyProperty.Register(
          nameof(HeaderContent), typeof(object), typeof(ColumnLayoutListView), new FrameworkPropertyMetadata(null, OnHeaderContentChanged));

        private ColumnLayoutListViewItem? _secondarySelectedItem;

        internal void SecondarySelectItem(ColumnLayoutListViewItem? item)
        {
            if (_secondarySelectedItem == item)
                return;
            if (_secondarySelectedItem != null)
                _secondarySelectedItem.IsSecondarySelected = false;
            _secondarySelectedItem = item;
            if (_secondarySelectedItem != null)
                _secondarySelectedItem.IsSecondarySelected = true;
            SecondarySelection = _secondarySelectedItem?.Content;
        }

        internal void ToggleSecondarySelectItem(ColumnLayoutListViewItem? item)
        {
            if (_secondarySelectedItem != item || _secondarySelectedItem == null)
            {
                SecondarySelectItem(item);
                return;
            }
            _secondarySelectedItem.IsSecondarySelected = false;
            _secondarySelectedItem = null;
            SecondarySelection = null;
        }

        public object? SecondarySelection
        {
            get { return GetValue(SecondarySelectionProperty); }
            set { SetValue(SecondarySelectionProperty, value); }
        }
        public static readonly DependencyProperty SecondarySelectionProperty = DependencyProperty.Register(
          nameof(SecondarySelection), typeof(object), typeof(ColumnLayoutListView), new PropertyMetadata(null));

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if (!(element is ColumnLayoutListViewItem listViewItem))
                return;
            listViewItem.IsSecondarySelected = false;
            if (item == SecondarySelection)
            {
                listViewItem.IsSecondarySelected = true;
                _secondarySelectedItem = listViewItem;
            }
            base.PrepareContainerForItemOverride(element, item);
        }

        private static void OnHeaderContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColumnLayoutListView)d).RegisterGridChanged(e.OldValue, e.NewValue);
        }

        protected override void OnInitialized(EventArgs e)
        {
            RegisterGridChanged(null, HeaderContent);
        }

        private void RegisterGridChanged(object? oldHeaderContent, object newHeaderContent)
        {
            if (oldHeaderContent is DependencyObject oldDepObj)
            {
                var oldGrid = oldDepObj.GetChildOfType<Grid>();
                if (oldGrid != null)
                    oldGrid.LayoutUpdated -= GridUpdatedPublisher.OnGridUpdated;
            }
            if (newHeaderContent is DependencyObject newDepObj)
            {
                var newGrid = newDepObj.GetChildOfType<Grid>();
                if (newGrid != null)
                  newGrid.LayoutUpdated += GridUpdatedPublisher.OnGridUpdated;
            }
        }
        public Func<object, object?, bool>? KeepSelectionRule
        {
            get { return (Func<object, object?, bool>?)GetValue(KeepSelectionRuleProperty); }
            set { SetValue(KeepSelectionRuleProperty, value); }
        }
        public static readonly DependencyProperty KeepSelectionRuleProperty = DependencyProperty.Register(
          nameof(KeepSelectionRule), typeof(Func<object, object?, bool>), typeof(ColumnLayoutListView), new FrameworkPropertyMetadata(null));

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            var oldSelection = SelectedItem;
            var oldSecondarySelection = SecondarySelection;
            base.OnItemsChanged(e);
            if (KeepSelectionRule == null || ItemsSource == null)
                return;
            SelectedItem = ItemsSource.OfType<object>().FirstOrDefault(item => KeepSelectionRule(item, oldSelection)) ?? ItemsSource.OfType<object>().FirstOrDefault();
            SecondarySelection = ItemsSource.OfType<object>().FirstOrDefault(item => KeepSelectionRule(item, oldSecondarySelection)) ?? ItemsSource.OfType<object>().FirstOrDefault();
        }

        public class GridPublisher
        {
            public event EventHandler? GridUpdated;

            public void OnGridUpdated(object? sender, EventArgs e)
            {
                GridUpdated?.Invoke(this, new EventArgs());
            }
        }

        private static readonly DependencyPropertyKey GridUpdatedPublisherPropertyKey
        = DependencyProperty.RegisterReadOnly(nameof(GridUpdatedPublisher), typeof(GridPublisher), typeof(ColumnLayoutListView),
            new FrameworkPropertyMetadata(new GridPublisher(),
                FrameworkPropertyMetadataOptions.None));

        public static readonly DependencyProperty GridUpdatedPublisherProperty
            = GridUpdatedPublisherPropertyKey.DependencyProperty;

        public GridPublisher GridUpdatedPublisher
        {
            get { return (GridPublisher)GetValue(GridUpdatedPublisherProperty); }
            protected set { SetValue(GridUpdatedPublisherPropertyKey, value); }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            var res = new ColumnLayoutListViewItem();
            res.AddedToListView(this);
            return res;
        }
    }
}
