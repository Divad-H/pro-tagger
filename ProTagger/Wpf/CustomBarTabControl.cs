using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ProTagger.Wpf
{
    public class CustomBarTabControl : TabControl
    {
        public object? HeaderBarContent
        {
            get => GetValue(HeaderBarContentProperty);
            set => SetValue(HeaderBarContentProperty, value);
        }
        public static readonly DependencyProperty HeaderBarContentProperty = DependencyProperty.Register(
          nameof(HeaderBarContent), typeof(object), typeof(CustomBarTabControl), new PropertyMetadata(null));

        public DataTemplate? HeaderBarContentTemplate
        {
            get => (DataTemplate?)GetValue(HeaderBarContentTemplateProperty);
            set => SetValue(HeaderBarContentTemplateProperty, value);
        }
        public static readonly DependencyProperty HeaderBarContentTemplateProperty = DependencyProperty.Register(
          nameof(HeaderBarContentTemplate), typeof(DataTemplate), typeof(CustomBarTabControl), new PropertyMetadata(null));

        public double HeaderBarMinWidth        {
            get => (double)GetValue(HeaderBarMinWidthProperty);
            set => SetValue(HeaderBarMinWidthProperty, value);
        }
        public static readonly DependencyProperty HeaderBarMinWidthProperty = DependencyProperty.Register(
          nameof(HeaderBarMinWidth), typeof(double), typeof(CustomBarTabControl), new PropertyMetadata(100.0));

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (e.Action == NotifyCollectionChangedAction.Add)
                SelectedIndex = Items.Count - 1;
            else if (SelectedIndex < 0 && !Items.IsEmpty)
                SelectedIndex = 0;
        }
    }
}
