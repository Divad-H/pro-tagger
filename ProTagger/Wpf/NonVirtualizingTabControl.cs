using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ProTagger.Wpf
{
    [TemplatePart(Name = "PART_ItemsHolder", Type = typeof(Panel))]
    public class NonVirtualizingTabControl : TabControl
    {
        private Panel? _itemsHolderPanel = null;

        public NonVirtualizingTabControl()
        {
            // This is necessary so that we get the initial databound selected item
            ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }

        private void ItemContainerGenerator_StatusChanged(object? sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
                UpdateSelectedItem();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _itemsHolderPanel = GetTemplateChild("PART_ItemsHolder") as Panel;
            UpdateSelectedItem();
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (_itemsHolderPanel == null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    _itemsHolderPanel.Children.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            var cp = FindChildContentPresenter(item);
                            if (cp != null)
                                _itemsHolderPanel.Children.Remove(cp);
                        }
                    }

                    // Don't do anything with new items because we don't want to
                    // create visuals that aren't being shown

                    UpdateSelectedItem();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException("Replace not implemented yet");
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateSelectedItem();
        }

        private void UpdateSelectedItem()
        {
            if (_itemsHolderPanel == null)
                return;

            // Generate a ContentPresenter if necessary
            TabItem? item = GetSelectedTabItem();
            if (item != null)
                CreateChildContentPresenter(item);

            // show the right child
            foreach (ContentPresenter? child in _itemsHolderPanel.Children)
                if (!(child is null))
                    child.Visibility = ((child.Tag as TabItem)?.IsSelected)
                        ?? throw new InvalidOperationException("Tag must be a TabItem") ? Visibility.Visible : Visibility.Collapsed;
        }

        private ContentPresenter? CreateChildContentPresenter(object item)
        {
            if (item == null)
                return null;

            var cp = FindChildContentPresenter(item);
            if (cp != null)
                return cp;

            // the actual child to be added.  cp.Tag is a reference to the TabItem
            cp = new ContentPresenter
            {
                Content = (item as TabItem)?.Content ?? item,
                ContentTemplate = ContentTemplate,
                ContentTemplateSelector = ContentTemplateSelector,
                ContentStringFormat = ContentStringFormat,
                Visibility = Visibility.Collapsed,
                Tag = (item is TabItem) ? item : (ItemContainerGenerator.ContainerFromItem(item))
            };
            _ = _itemsHolderPanel ?? throw new ArgumentNullException(nameof(_itemsHolderPanel));
            _itemsHolderPanel.Children.Add(cp);
            return cp;
        }

        private ContentPresenter? FindChildContentPresenter(object? data)
        {
            data = (data as TabItem)?.Content ?? data;
            if (data == null)
                return null;
            if (_itemsHolderPanel == null)
                return null;
            foreach (ContentPresenter? cp in _itemsHolderPanel.Children)
                if (cp?.Content == data)
                    return cp;
            return null;
        }

        protected TabItem? GetSelectedTabItem()
        {
            object selectedItem = SelectedItem;
            if (selectedItem == null)
                return null;
            return selectedItem as TabItem ?? ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as TabItem;
        }
    }
}
