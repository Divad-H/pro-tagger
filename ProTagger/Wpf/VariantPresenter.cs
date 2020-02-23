using System;
using System.Windows;
using System.Windows.Controls;

namespace ProTagger.Wpf
{
    internal class VariantTemplateSelector : DataTemplateSelector
    {
        private readonly VariantPresenter _variantPresenter;
        public VariantTemplateSelector(VariantPresenter variantPresenter)
        {
            _variantPresenter = variantPresenter;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (_variantPresenter.VariantContent is Variant variant)
                if (variant.VariantIndex == 0)
                    return _variantPresenter.Variant0Template;
            return _variantPresenter.Variant1Template;
        }
    }

    public class VariantPresenter : UserControl
    {
        private readonly ContentControl _content = new ContentControl();

        public VariantPresenter()
        {
            _content.ContentTemplateSelector = new VariantTemplateSelector(this);
            Content = _content;
        }

        public object VariantContent
        {
            get { return GetValue(VariantContentProperty); }
            set { SetValue(VariantContentProperty, value); }
        }

        public static readonly DependencyProperty VariantContentProperty = DependencyProperty.Register(
          nameof(VariantContent), typeof(object), typeof(VariantPresenter), new FrameworkPropertyMetadata(OnVariantContentChanged));

        private static void OnVariantContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var variantPresenter = d as VariantPresenter ??
                throw new ArgumentException(nameof(d) + " must be of type " + nameof(VariantPresenter));
            variantPresenter._content.Content = (e.NewValue as Variant)?.Value;
        }

        public DataTemplate Variant0Template
        {
            get { return (DataTemplate)GetValue(Variant0TemplateProperty); }
            set { SetValue(Variant0TemplateProperty, value); }
        }

        public static readonly DependencyProperty Variant0TemplateProperty = DependencyProperty.Register(
          nameof(Variant0Template), typeof(DataTemplate), typeof(VariantPresenter));

        public DataTemplate Variant1Template
        {
            get { return (DataTemplate)GetValue(Variant1TemplateProperty); }
            set { SetValue(Variant1TemplateProperty, value); }
        }

        public static readonly DependencyProperty Variant1TemplateProperty = DependencyProperty.Register(
          nameof(Variant1Template), typeof(DataTemplate), typeof(VariantPresenter));
    }
}
