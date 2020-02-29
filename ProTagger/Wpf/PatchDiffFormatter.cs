using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using static ProTagger.Utilities.DiffAnalyzer;

namespace ProTagger.Wpf
{
    public class PatchDiffFormatter : FrameworkElement
    {
        public Hunk Content
        {
            get { return (Hunk)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content), typeof(Hunk), typeof(PatchDiffFormatter),
            new FrameworkPropertyMetadata(new Hunk(0, 0, 0, 0, "", new List<Variant<WordDiff, UnchangedLine>>()), FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        Brush OldHighlightBrush = new SolidColorBrush(Color.FromArgb(125, 255, 0, 0));
        Brush OldNormalBrush = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0));
        Brush NewHighlightBrush = new SolidColorBrush(Color.FromArgb(125, 0, 255, 0));
        Brush NewNormalBrush = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0));

        protected override void OnRender(DrawingContext drawingContext)
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
                foreach (var variant in Content.Diff)
                {
                    variant.Visit(wordDiff =>
                    {
                        foreach (var oldLine in wordDiff.OldText)
                        {
                            var lineNoText = CreateText(oldLine.LineNumber.ToString());
                            texts.Add(Tuple.Create(lineNoText, new Point(15 - lineNoText.Width / 2, lineIndex * 10)));
                            double x = 60;
                            foreach (var token in oldLine.Text)
                            {
                                var text = CreateText(token.Text);
                                var width = text.WidthIncludingTrailingWhitespace;
                                DrawRect(token.Unchanged ? oldNormalCtx : oldHighlightCtx, x, width, lineIndex * 10);
                                texts.Add(Tuple.Create(text, new Point(x, lineIndex * 10)));
                                x += width;
                            }
                            lineIndex++;
                        }
                        foreach (var newLine in wordDiff.NewText)
                        {
                            var lineNoText = CreateText(newLine.LineNumber.ToString());
                            texts.Add(Tuple.Create(lineNoText, new Point(45 - lineNoText.Width / 2, lineIndex * 10)));
                            double x = 60;
                            foreach (var token in newLine.Text)
                            {
                                var text = CreateText(token.Text);
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
                        var lineNoText = CreateText(unchangedLine.OldLineNumber.ToString());
                        texts.Add(Tuple.Create(lineNoText, new Point(15 - lineNoText.Width / 2, lineIndex * 10)));
                        lineNoText = CreateText(unchangedLine.NewLineNumber.ToString());
                        texts.Add(Tuple.Create(lineNoText, new Point(45 - lineNoText.Width / 2, lineIndex * 10)));
                        var text = CreateText(unchangedLine.Text);
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

        void DrawRect(StreamGeometryContext ctx, double left, double width, double top)
        {
            ctx.BeginFigure(new Point(left, top), true, true);
            ctx.LineTo(new Point(left + width, top), false, false);
            ctx.LineTo(new Point(left + width, top + 10), false, false);
            ctx.LineTo(new Point(left, top + 10), false, false);
        }

        FormattedText CreateText(string text)
        {
            return new FormattedText(
                        text,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        10,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(100,
                Content.Diff
                    .Aggregate(0, (count, variant) => variant.Visit(wordDiff => wordDiff.NewText.Count + wordDiff.OldText.Count, unchangedLine => 1) + count) * 10);
        }
    }
}
