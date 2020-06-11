using ProTagger.Repository.GitLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProTagger.Wpf
{
    using TGraphPos = UInt16;
    public class GraphRow : FrameworkElement
    {
        public LogGraphNode Content
        {
            get => (LogGraphNode)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
          nameof(Content), typeof(LogGraphNode), typeof(GraphRow), 
          new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        
        public ColumnDefinitionCollection ColumnDefinitions
        {
            get => (ColumnDefinitionCollection)GetValue(ColumnDefinitionsProperty);
            set => SetValue(ColumnDefinitionsProperty, value);
        }
        public static readonly DependencyProperty ColumnDefinitionsProperty = DependencyProperty.Register(
          nameof(ColumnDefinitions), typeof(ColumnDefinitionCollection), typeof(GraphRow),
          new FrameworkPropertyMetadata(null));

        public ColumnLayoutListView.GridPublisher GridUpdatedPublisher
        {
            get => (ColumnLayoutListView.GridPublisher)GetValue(GridUpdatedPublisherProperty);
            set => SetValue(GridUpdatedPublisherProperty, value);
        }

        public static readonly DependencyProperty GridUpdatedPublisherProperty = DependencyProperty.Register(
          nameof(GridUpdatedPublisher), typeof(ColumnLayoutListView.GridPublisher), typeof(GraphRow),
          new FrameworkPropertyMetadata(null, OnGridUpdatedPublisherChanged));

        private static void OnGridUpdatedPublisherChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GraphRow graphRow)
                graphRow.RegisterGridUpdated(
                    e.OldValue as ColumnLayoutListView.GridPublisher,
                    e.NewValue as ColumnLayoutListView.GridPublisher);
        }

        public Brush? Background
        {
            get => (Brush?)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
          nameof(Background), typeof(Brush), typeof(GraphRow),
          new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush? HighlightFill
        {
            get => (Brush?)GetValue(HighlightFillProperty);
            set => SetValue(HighlightFillProperty, value);
        }

        public static readonly DependencyProperty HighlightFillProperty = DependencyProperty.Register(
          nameof(HighlightFill), typeof(Brush), typeof(GraphRow),
          new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush? Fill
        {
            get => (Brush?)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
          nameof(Fill), typeof(Brush), typeof(GraphRow),
          new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
          nameof(Foreground), typeof(Brush), typeof(GraphRow),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush LabelBrush
        {
            get => (Brush)GetValue(LabelBrushProperty);
            set => SetValue(LabelBrushProperty, value);
        }

        public static readonly DependencyProperty LabelBrushProperty = DependencyProperty.Register(
          nameof(LabelBrush), typeof(Brush), typeof(GraphRow),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush LabelForeground
        {
            get => (Brush)GetValue(LabelForegroundProperty);
            set => SetValue(LabelForegroundProperty, value);
        }

        public static readonly DependencyProperty LabelForegroundProperty = DependencyProperty.Register(
          nameof(LabelForeground), typeof(Brush), typeof(GraphRow),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush SecondarySelectionBackground
        {
            get => (Brush)GetValue(SecondarySelectionBackgroundProperty);
            set => SetValue(SecondarySelectionBackgroundProperty, value);
        }

        public static readonly DependencyProperty SecondarySelectionBackgroundProperty = DependencyProperty.Register(
          nameof(SecondarySelectionBackground), typeof(Brush), typeof(GraphRow),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush HeadBrush
        {
            get => (Brush)GetValue(HeadBrushProperty);
            set => SetValue(HeadBrushProperty, value);
        }

        public static readonly DependencyProperty HeadBrushProperty = DependencyProperty.Register(
          nameof(HeadBrush), typeof(Brush), typeof(GraphRow),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        private void RegisterGridUpdated(ColumnLayoutListView.GridPublisher? oldValue, ColumnLayoutListView.GridPublisher? newValue)
        {
            if (oldValue != null)
                WeakEventManager<ColumnLayoutListView.GridPublisher, EventArgs>.RemoveHandler(oldValue, nameof(newValue.GridUpdated), OnGridUpdated);
            if (newValue != null)
                WeakEventManager<ColumnLayoutListView.GridPublisher, EventArgs>.AddHandler(newValue, nameof(newValue.GridUpdated), OnGridUpdated);
        }

        List<double>? _columnWidthBuffer;

        private bool EnsureColumnWidthBuffer()
        {
            if (ColumnDefinitions != null)
            {
                if (_columnWidthBuffer is null)
                    _columnWidthBuffer = ColumnDefinitions.Select(column => column.ActualWidth).ToList();
                else
                    return true;
            }
            return false;
        }

        private void OnGridUpdated(object? sender, EventArgs args)
        {
            if (EnsureColumnWidthBuffer() && _columnWidthBuffer != null)
            {
                bool dirty = false;
                for (int i = 0; i < ColumnDefinitions.Count; ++i)
                {
                    if (Math.Abs(_columnWidthBuffer[i] - ColumnDefinitions[i].ActualWidth) > 0.1)
                    {
                        dirty = true;
                        _columnWidthBuffer[i] = ColumnDefinitions[i].ActualWidth;
                    }
                }
                if (!dirty)
                    return;
                Render();
            }
        }

        readonly DrawingGroup _backingStore = new DrawingGroup();

        protected override void OnRender(DrawingContext drawingContext)
        {
            Render();
            drawingContext.DrawDrawing(_backingStore);
        }

        private void Render()
        {
            using var drawingContext = _backingStore.Open();
            Render(drawingContext);
        }

        private static readonly Typeface _typeface = new Typeface("Consolas");

        private FormattedText CreateText(string text, Brush foreground)
        {
            return new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                _typeface,
                12,
                foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private void Render(DrawingContext drawingContext)
        {
            if (ColumnDefinitions is null)
                return;
            EnsureColumnWidthBuffer();

            if (Background != null)
                drawingContext.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight));

            var geometry = new StreamGeometry { FillRule = FillRule.EvenOdd };
            using (StreamGeometryContext ctx = geometry.Open())
                DrawGraph(ctx);
            geometry.Freeze();

            drawingContext.PushClip(new RectangleGeometry(
                new Rect(0, -10, ColumnDefinitions[0].ActualWidth, ActualHeight + 20)));
            var pen = new Pen(Foreground, 1.5);
            drawingContext.DrawGeometry(Content.IsMerge ? HighlightFill : Content.Commit.Is<LibGit2Sharp.Commit>() ? Fill : null, pen, geometry);
            drawingContext.Pop();

            double left = ColumnDefinitions[0].ActualWidth + ColumnDefinitions[1].ActualWidth;
            double offset = 0;

            drawingContext.PushClip(new RectangleGeometry(
                new Rect(left, 0, ColumnDefinitions[2].ActualWidth, ActualHeight)));
            foreach (var branch in Content.Branches)
                offset += DrawLabel(drawingContext, "⟨⟨ " + branch.ShortName, left + offset, branch.IsHead ? HeadBrush : LabelBrush);
            foreach (var tag in Content.Tags)
                offset += DrawLabel(drawingContext, "🏷️ " + tag.ShortName, left + offset, LabelBrush);

            var formattedText = CreateText(Content.MessageShort, Foreground);
            drawingContext.DrawText(formattedText, new Point(left + offset, 0));
            drawingContext.Pop();

            left += ColumnDefinitions[2].ActualWidth + ColumnDefinitions[3].ActualWidth;
            if (Content.Commit.Is<LibGit2Sharp.Commit>())
                DrawTextColumn(drawingContext,
                    Content.Commit.Get<LibGit2Sharp.Commit>().Author.When.DateTime.ToString(),
                    left,
                    ColumnDefinitions[4].ActualWidth);

            left += ColumnDefinitions[4].ActualWidth + ColumnDefinitions[5].ActualWidth;
            if (Content.Commit.Is<LibGit2Sharp.Commit>())
                DrawTextColumn(drawingContext,
                    Content.Commit.Get<LibGit2Sharp.Commit>().Author.Name + " " + Content.Commit.Get<LibGit2Sharp.Commit>().Author.Email,
                    left,
                    ColumnDefinitions[6].ActualWidth);

            left += ColumnDefinitions[6].ActualWidth + ColumnDefinitions[7].ActualWidth;
            DrawTextColumn(drawingContext,
                Content.ShortSha,
                left,
                ColumnDefinitions[8].ActualWidth);
        }

        private double DrawLabel(DrawingContext ctx, string name, double left, Brush background)
        {
            var formattedText = CreateText(name, LabelForeground);
            var textWidth = formattedText.Width;
            ctx.DrawRectangle(background, null, new Rect(left, 1, textWidth + 10, ActualHeight - 2));
            ctx.DrawText(formattedText, new Point(left + 5, 0));
            return textWidth + 20;
        }

        private void DrawTextColumn(DrawingContext ctx, string text, double left, double width)
        {
            var formattedText = CreateText(text, Foreground);
            ctx.PushClip(new RectangleGeometry(
                new Rect(left, 0, width, ActualHeight)));
            ctx.DrawText(formattedText, new Point(left, 0));
            ctx.Pop();
        }

        private const double HorizontalDistance = 10;
        private const double Radius = 3.5;

        private void DrawGraph(StreamGeometryContext ctx)
        {
            DrawCircle(ctx, (1 + Content.GraphPosition) * HorizontalDistance, ActualHeight / 2, Radius);
            foreach (var direction in Content.Directions.SelectMany((subDirection, i) => MapDirections(subDirection.Previous, (TGraphPos)i)))
            {
                ctx.BeginFigure(
                    new Point((1 + 0.5 * (direction.Item1 + direction.Item2)) * HorizontalDistance, 0), false, false);
                ctx.LineTo(new Point((1 + direction.Item1) * HorizontalDistance, ActualHeight / 10), true, false);
                if (direction.Item1 == Content.GraphPosition)
                    ctx.LineTo(new Point((1 + direction.Item1) * HorizontalDistance, ActualHeight / 2 - Radius), true, false);
                else
                    ctx.LineTo(new Point((1 + direction.Item1) * HorizontalDistance, ActualHeight / 2), true, false);
            }
            foreach (var direction in Content.Directions.SelectMany((subDirection, i) => MapDirections(subDirection.Next, (TGraphPos)i)))
            {
                ctx.BeginFigure(
                    new Point((1 + 0.5 * (direction.Item1 + direction.Item2)) * HorizontalDistance, ActualHeight), false, false);
                ctx.LineTo(new Point((1 + direction.Item2) * HorizontalDistance, ActualHeight / 2.5 + ActualHeight / 2), true, false);
                if (direction.Item2 == Content.GraphPosition)
                    ctx.LineTo(new Point((1 + direction.Item2) * HorizontalDistance, ActualHeight / 2 + Radius), true, false);
                else
                    ctx.LineTo(new Point((1 + direction.Item2) * HorizontalDistance, ActualHeight / 2), true, false);
            }
        }

        private IEnumerable<Tuple<TGraphPos, TGraphPos>> MapDirections(IEnumerable<TGraphPos> subDirection, TGraphPos i)
            => subDirection.Select(direction =>  Tuple.Create(direction, i));

        private static void DrawCircle(StreamGeometryContext ctx, double centerX, double centerY, double radius)
        {
            const double ControlPointRatio = 0.5522847498307933984022516322796;

            var x0 = centerX - radius;
            var x1 = centerX - radius * ControlPointRatio;
            var x2 = centerX;
            var x3 = centerX + radius * ControlPointRatio;
            var x4 = centerX + radius;

            var y0 = centerY - radius;
            var y1 = centerY - radius * ControlPointRatio;
            var y2 = centerY;
            var y3 = centerY + radius * ControlPointRatio;
            var y4 = centerY + radius;

            ctx.BeginFigure(new Point(x2, y0), true, true);
            ctx.BezierTo(new Point(x3, y0), new Point(x4, y1), new Point(x4, y2), true, true);
            ctx.BezierTo(new Point(x4, y3), new Point(x3, y4), new Point(x2, y4), true, true);
            ctx.BezierTo(new Point(x1, y4), new Point(x0, y3), new Point(x0, y2), true, true);
            ctx.BezierTo(new Point(x0, y1), new Point(x1, y0), new Point(x2, y0), true, true);
        }
    }
}
