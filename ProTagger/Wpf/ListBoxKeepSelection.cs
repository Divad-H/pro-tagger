using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ProTagger.Wpf
{
    public class ListBoxKeepSelection : ListBox
    {
        public IEqualityComparer<object>? KeepSelectionRule
        {
            get => (IEqualityComparer<object>?)GetValue(KeepSelectionRuleProperty);
            set => SetValue(KeepSelectionRuleProperty, value);
        }
        public static readonly DependencyProperty KeepSelectionRuleProperty = DependencyProperty.Register(
          nameof(KeepSelectionRule), typeof(IEqualityComparer<object>), typeof(ListBoxKeepSelection), new FrameworkPropertyMetadata(null));

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            var oldSelection = SelectedItems.Cast<object>().ToList();
            base.OnItemsChanged(e);
            if (KeepSelectionRule == null || ItemsSource == null)
                return;
            var newSelection = ItemsSource.OfType<object>().ToHashSet(KeepSelectionRule);
            newSelection.IntersectWith(oldSelection);
            SetSelectedItems(newSelection);
        }

        public void ChangeSelectedItems(IEnumerable<object?> selectedItems)
        {
            SetSelectedItems(selectedItems);
        }
    }
}
