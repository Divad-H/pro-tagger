using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ProTagger.Wpf
{
    public class ColumnLayoutListView : ListView
    {
        //public GridColumnLayout ColumnLayout
        //{
        //    get { return (GridColumnLayout)GetValue(ColumnLayoutProperty); }
        //    protected set { SetValue(ColumnLayoutPropertyKey, value); }
        //}

        //private static readonly DependencyPropertyKey ColumnLayoutPropertyKey = 
        //    DependencyProperty.RegisterReadOnly(nameof(ColumnLayout), typeof(GridColumnLayout), typeof(ColumnLayoutListView),
        //    new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None));

        //public static readonly DependencyProperty ColumnLayoutProperty = ColumnLayoutPropertyKey.DependencyProperty;

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
            if (oldHeaderContent is Grid oldGrid)
                oldGrid.LayoutUpdated -= GridUpdatedPublisher.OnGridUpdated;
            if (!(newHeaderContent is Grid grid))
                return;
            grid.LayoutUpdated += GridUpdatedPublisher.OnGridUpdated;
        }
        public Func<object, object, bool>? KeepSelectionRule
        {
            get { return (Func<object, object, bool>?)GetValue(KeepSelectionRuleProperty); }
            set { SetValue(KeepSelectionRuleProperty, value); }
        }
        public static readonly DependencyProperty KeepSelectionRuleProperty = DependencyProperty.Register(
          nameof(KeepSelectionRule), typeof(Func<object, object, bool>), typeof(ColumnLayoutListView), new FrameworkPropertyMetadata(null));

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            var oldSelection = SelectedItem;
            base.OnItemsChanged(e);
            if (KeepSelectionRule == null || ItemsSource == null)
                return;
            SelectedItem = ItemsSource.OfType<object>().FirstOrDefault(item => KeepSelectionRule(item, oldSelection)) ?? ItemsSource.OfType<object>().FirstOrDefault();
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

    }
}
