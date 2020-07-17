using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static ProTagger.Utilities.DiffAnalyzer;

namespace ProTagger.Wpf
{
    public class PatchDiffFormatter : FrameworkElement
    {
        public Hunk Content
        {
            get => (Hunk)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content), typeof(Hunk), typeof(PatchDiffFormatter),
            new FrameworkPropertyMetadata(
                new Hunk(0, 0, 0, 0, "", new List<Variant<WordDiff, UnchangedLine>>()), 
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnContentChanged));

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((PatchDiffFormatter)d).UpdateContent((Hunk?)e.NewValue);

        private CancellationTokenSource? _cancellationTokenSource = null;

        private void UpdateContent(Hunk? newContent)
        {
            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            UpdateContent(newContent, _cancellationTokenSource.Token);
        }

        private async void UpdateContent(Hunk? newContent, CancellationToken ct)
        {
            try
            {
                _backingStore = new DrawingGroup();
                if (newContent is null)
                    return;

                var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

                var backingStore = await Task.Run(() =>
                {
                    var newBackingStore = new DrawingGroup();
                    using (var drawingContext = newBackingStore.Open())
                        Render(drawingContext, (Hunk)newContent, pixelsPerDip, ct);
                    newBackingStore.Freeze();
                    return newBackingStore;
                });
                if (ct.IsCancellationRequested)
                    return;
                _backingStore = backingStore;
                InvalidateVisual();
            }
            catch (Exception)
            { }
        }

        static readonly Brush OldHighlightBrush = new SolidColorBrush(Color.FromArgb(125, 255, 0, 0));
        static readonly Brush OldNormalBrush = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0));
        static readonly Brush NewHighlightBrush = new SolidColorBrush(Color.FromArgb(125, 0, 255, 0));
        static readonly Brush NewNormalBrush = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0));

        private DrawingGroup _backingStore = new DrawingGroup();

        protected override void OnRender(DrawingContext drawingContext)
            => drawingContext.DrawDrawing(_backingStore);

        private static void Render(DrawingContext drawingContext, Hunk content, double pixelsPerDip, CancellationToken ct)
        { 
            var texts = new List<Tuple<FormattedText, Point>>();
            var newNormal = new StreamGeometry { FillRule = FillRule.Nonzero };
            var newHighlight = new StreamGeometry { FillRule = FillRule.Nonzero };
            var oldNormal = new StreamGeometry { FillRule = FillRule.Nonzero };
            var oldHighlight = new StreamGeometry { FillRule = FillRule.Nonzero };
            using (StreamGeometryContext newNormalCtx = newNormal.Open())
            using (StreamGeometryContext newHighlightCtx = newHighlight.Open())
            using (StreamGeometryContext oldNormalCtx = oldNormal.Open())
            using (StreamGeometryContext oldHighlightCtx = oldHighlight.Open())
            {
                int lineIndex = 0;
                foreach (var variant in content.Diff)
                {
                    if (ct.IsCancellationRequested)
                        return;
                    variant.Visit(wordDiff =>
                    {
                        foreach (var oldLine in wordDiff.OldText)
                        {
                            if (ct.IsCancellationRequested)
                                return;
                            var lineNoText = CreateText(oldLine.LineNumber.ToString(), pixelsPerDip);
                            texts.Add(Tuple.Create(lineNoText, new Point(15 - lineNoText.Width / 2, lineIndex * 10)));
                            double x = 60;
                            foreach (var token in oldLine.Text)
                            {
                                var text = CreateText(token.Text, pixelsPerDip);
                                var width = text.WidthIncludingTrailingWhitespace;
                                DrawRect(token.Unchanged ? oldNormalCtx : oldHighlightCtx, x, width, lineIndex * 10);
                                texts.Add(Tuple.Create(text, new Point(x, lineIndex * 10)));
                                x += width;
                            }
                            lineIndex++;
                        }
                        foreach (var newLine in wordDiff.NewText)
                        {
                            if (ct.IsCancellationRequested)
                                return;
                            var lineNoText = CreateText(newLine.LineNumber.ToString(), pixelsPerDip);
                            texts.Add(Tuple.Create(lineNoText, new Point(45 - lineNoText.Width / 2, lineIndex * 10)));
                            double x = 60;
                            foreach (var token in newLine.Text)
                            {
                                var text = CreateText(token.Text, pixelsPerDip);
                                var width = text.WidthIncludingTrailingWhitespace;
                                DrawRect(token.Unchanged ? newNormalCtx : newHighlightCtx, x, width, lineIndex * 10);
                                texts.Add(Tuple.Create(text, new Point(x, lineIndex * 10)));
                                x += width;
                            }
                            lineIndex++;
                        }
                    },
                    unchangedLine =>
                    {
                        if (ct.IsCancellationRequested)
                            return;
                        var lineNoText = CreateText(unchangedLine.OldLineNumber.ToString(), pixelsPerDip);
                        texts.Add(Tuple.Create(lineNoText, new Point(15 - lineNoText.Width / 2, lineIndex * 10)));
                        lineNoText = CreateText(unchangedLine.NewLineNumber.ToString(), pixelsPerDip);
                        texts.Add(Tuple.Create(lineNoText, new Point(45 - lineNoText.Width / 2, lineIndex * 10)));
                        var text = CreateText(unchangedLine.Text, pixelsPerDip);
                        texts.Add(Tuple.Create(text, new Point(60, lineIndex++ * 10)));
                    });
                }
            }

            newNormal.Freeze();
            newHighlight.Freeze();
            oldNormal.Freeze();
            oldHighlight.Freeze();

            drawingContext.DrawGeometry(NewNormalBrush, null, newNormal);
            drawingContext.DrawGeometry(NewHighlightBrush, null, newHighlight);
            drawingContext.DrawGeometry(OldNormalBrush, null, oldNormal);
            drawingContext.DrawGeometry(OldHighlightBrush, null, oldHighlight);
            foreach (var (text, point) in texts)
                drawingContext.DrawText(text, point);
        }

        static void DrawRect(StreamGeometryContext ctx, double left, double width, double top)
        {
            ctx.BeginFigure(new Point(left, top), true, true);
            ctx.LineTo(new Point(left + width, top), false, false);
            ctx.LineTo(new Point(left + width, top + 10), false, false);
            ctx.LineTo(new Point(left, top + 10), false, false);
        }

        static FormattedText CreateText(string text, double pixelsPerDip)
            => new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas"),
                10,
                Brushes.Black,
                pixelsPerDip);

        protected override Size MeasureOverride(Size availableSize)
            => new Size(100,
                Content.Diff
                    .Aggregate(0, (count, variant) => variant.Visit(wordDiff => wordDiff.NewText.Count + wordDiff.OldText.Count, unchangedLine => 1) + count) * 10);

        static PatchDiffFormatter()
        {
            NewHighlightBrush.Freeze();
            NewNormalBrush.Freeze();
            OldHighlightBrush.Freeze();
            OldNormalBrush.Freeze();
        }

    }
}
