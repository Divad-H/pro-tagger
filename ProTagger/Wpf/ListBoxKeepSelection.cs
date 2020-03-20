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
        public Func<object, object, bool>? KeepSelectionRule
        {
            get => (Func<object, object, bool>?)GetValue(KeepSelectionRuleProperty);
            set => SetValue(KeepSelectionRuleProperty, value);
        }
        public static readonly DependencyProperty KeepSelectionRuleProperty = DependencyProperty.Register(
          nameof(KeepSelectionRule), typeof(Func<object, object, bool>), typeof(ListBoxKeepSelection), new FrameworkPropertyMetadata(null));

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            var oldSelection = SelectedItems.Cast<object>().ToList();
            base.OnItemsChanged(e);
            if (KeepSelectionRule == null || ItemsSource == null)
                return;
            foreach (var item in ItemsSource.OfType<object>().Where(item => oldSelection.Find(oldItem => KeepSelectionRule(item, oldItem)) != null))
                SelectedItems.Add(item);
        }
    }
}
