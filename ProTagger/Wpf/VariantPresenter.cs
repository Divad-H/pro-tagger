using System;
using System.Windows;
using System.Windows.Controls;

namespace ProTagger.Wpf
{
    internal class VariantTemplateSelector : DataTemplateSelector
    {
        private readonly VariantPresenter _variantPresenter;
        public VariantTemplateSelector(VariantPresenter variantPresenter)
            => _variantPresenter = variantPresenter;

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (_variantPresenter.VariantContent is Variant variant)
                return variant.VariantIndex switch
                {
                    0 => _variantPresenter.Variant0Template,
                    1 => _variantPresenter.Variant1Template,
                    2 => _variantPresenter.Variant2Template,
                    3 => _variantPresenter.Variant3Template,
                    _ => _variantPresenter.Variant1Template,
                };
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
            get => GetValue(VariantContentProperty);
            set => SetValue(VariantContentProperty, value);
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
            get => (DataTemplate)GetValue(Variant0TemplateProperty);
            set => SetValue(Variant0TemplateProperty, value);
        }

        public static readonly DependencyProperty Variant0TemplateProperty = DependencyProperty.Register(
          nameof(Variant0Template), typeof(DataTemplate), typeof(VariantPresenter));

        public DataTemplate Variant1Template
        {
            get => (DataTemplate)GetValue(Variant1TemplateProperty);
            set => SetValue(Variant1TemplateProperty, value);
        }

        public static readonly DependencyProperty Variant1TemplateProperty = DependencyProperty.Register(
          nameof(Variant1Template), typeof(DataTemplate), typeof(VariantPresenter));

        public DataTemplate Variant2Template
        {
            get => (DataTemplate)GetValue(Variant2TemplateProperty);
            set => SetValue(Variant2TemplateProperty, value);
        }

        public static readonly DependencyProperty Variant2TemplateProperty = DependencyProperty.Register(
          nameof(Variant2Template), typeof(DataTemplate), typeof(VariantPresenter));

        public DataTemplate Variant3Template
        {
            get => (DataTemplate)GetValue(Variant3TemplateProperty);
            set => SetValue(Variant3TemplateProperty, value);
        }

        public static readonly DependencyProperty Variant3TemplateProperty = DependencyProperty.Register(
          nameof(Variant3Template), typeof(DataTemplate), typeof(VariantPresenter));
    }
}
