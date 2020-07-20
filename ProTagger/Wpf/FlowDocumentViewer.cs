using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace ProTagger.Wpf
{
    class FlowDocumentViewer : FlowDocumentScrollViewer
    {
        public FlowDocumentViewer()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            Loaded += OnLoaded;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {}

        private static readonly DependencyPropertyKey ContentWidthPropertyKey
            = DependencyProperty.RegisterReadOnly(nameof(ContentWidth), typeof(double), typeof(FlowDocumentViewer),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.None));

        public static readonly DependencyProperty ContentWidthProperty
            = ContentWidthPropertyKey.DependencyProperty;

        public double ContentWidth
        {
            get => (double)GetValue(ContentWidthProperty);
            protected set => SetValue(ContentWidthPropertyKey, value);
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            if (!(Document is PatchDiffFormatter document))
                return;
            if (document.LineEndings is null)
                return;
            if (document.ContentTableCell is null)
                return;

            ContentWidth = document.LineEndings
                .Select(lineEnding => lineEnding.ContentStart.GetCharacterRect(LogicalDirection.Forward).Right)
                .Max()
                  + Padding.Left + Padding.Right;
        }
    }
}
