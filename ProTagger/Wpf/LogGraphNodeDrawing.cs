using ProTagger.Repo.GitLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ProTagger.Wpf
{
    public class LogGraphNodeDrawing : Canvas
    {
        readonly Path _lines = new Path();
        readonly Path _circle = new Path();
        
        public LogGraphNodeDrawing()
        {
            _lines.Stroke = Stroke;
            _lines.StrokeThickness = StrokeThickness;
            _circle.Stroke = Stroke;
            _circle.StrokeThickness = StrokeThickness;
            Children.Add(_lines);
            Children.Add(_circle);
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
          nameof(Stroke), typeof(Brush), typeof(LogGraphNodeDrawing), new PropertyMetadata(new SolidColorBrush(Colors.Black), StrokeChanged));

        private static void StrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LogGraphNodeDrawing logGraphNodeDrawing))
                throw new ArgumentException("Invalid Type", nameof(d));
            if (!(e.NewValue is Brush brush))
                throw new ArgumentException("An object of type Brush was expected.", nameof(e));
            logGraphNodeDrawing._lines.Stroke = brush;
            logGraphNodeDrawing._circle.Stroke = brush;
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
          nameof(StrokeThickness), typeof(double), typeof(LogGraphNodeDrawing), new PropertyMetadata(1.0, StrokeThicknessChanged));

        private static void StrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LogGraphNodeDrawing logGraphNodeDrawing))
                throw new ArgumentException("Invalid Type", nameof(d));
            if (!(e.NewValue is double thickness))
                throw new ArgumentException("An object of type double was expected.", nameof(e));
            logGraphNodeDrawing._lines.StrokeThickness = thickness;
            logGraphNodeDrawing._circle.StrokeThickness = thickness;
        }

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }
        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
          nameof(Fill), typeof(Brush), typeof(LogGraphNodeDrawing), new PropertyMetadata(new SolidColorBrush(Colors.Transparent), FillChanged));

        private static void FillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LogGraphNodeDrawing logGraphNodeDrawing))
                throw new ArgumentException("Invalid Type", nameof(d));
            if (!(e.NewValue is Brush brush))
                throw new ArgumentException("An object of type Brush was expected.", nameof(e));
            if (logGraphNodeDrawing.GraphNode == null || !logGraphNodeDrawing.GraphNode.IsMerge)
              logGraphNodeDrawing._circle.Fill = brush;
        }

        public Brush HighlightFill
        {
            get { return (Brush)GetValue(HighlightFillProperty); }
            set { SetValue(HighlightFillProperty, value); }
        }
        public static readonly DependencyProperty HighlightFillProperty = DependencyProperty.Register(
          nameof(HighlightFill), typeof(Brush), typeof(LogGraphNodeDrawing), new PropertyMetadata(new SolidColorBrush(Colors.Transparent), HighlightFillChanged));

        private static void HighlightFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LogGraphNodeDrawing logGraphNodeDrawing))
                throw new ArgumentException("Invalid Type", nameof(d));
            if (!(e.NewValue is Brush brush))
                throw new ArgumentException("An object of type Brush was expected.", nameof(e));
            if (logGraphNodeDrawing.GraphNode != null && logGraphNodeDrawing.GraphNode.IsMerge)
              logGraphNodeDrawing._circle.Fill = brush;
        }

        public double HorizontalDistance
        {
            get { return (double)GetValue(HorizontalDistanceProperty); }
            set { SetValue(HorizontalDistanceProperty, value); }
        }
        public static readonly DependencyProperty HorizontalDistanceProperty = DependencyProperty.Register(
          nameof(HorizontalDistance), typeof(double), typeof(LogGraphNodeDrawing), new PropertyMetadata(10.0, HorizontalDistanceChanged));

        private static void HorizontalDistanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LogGraphNodeDrawing logGraphNodeDrawing))
                throw new ArgumentException("Invalid Type", nameof(d));
            if (!(e.NewValue is double))
                throw new ArgumentException("An object of type double was expected.", nameof(e));
            logGraphNodeDrawing.GraphChanged();
        }

        public double VerticalDistance
        {
            get { return (double)GetValue(VerticalDistanceProperty); }
            set { SetValue(VerticalDistanceProperty, value); }
        }
        public static readonly DependencyProperty VerticalDistanceProperty = DependencyProperty.Register(
          nameof(VerticalDistance), typeof(double), typeof(LogGraphNodeDrawing), new PropertyMetadata(10.0, VerticalDistanceChanged));

        private static void VerticalDistanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LogGraphNodeDrawing logGraphNodeDrawing))
                throw new ArgumentException("Invalid Type", nameof(d));
            if (!(e.NewValue is double))
                throw new ArgumentException("An object of type double was expected.", nameof(e));
            logGraphNodeDrawing.GraphChanged();
        }

        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }
        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(
          nameof(Radius), typeof(double), typeof(LogGraphNodeDrawing), new PropertyMetadata(3.5, RadiusChanged));

        private static void RadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LogGraphNodeDrawing logGraphNodeDrawing))
                throw new ArgumentException("Invalid Type", nameof(d));
            if (!(e.NewValue is double))
                throw new ArgumentException("An object of type double was expected.", nameof(e));
            logGraphNodeDrawing.GraphChanged();
        }

        public LogGraphNode GraphNode
        {
            get { return (LogGraphNode)GetValue(GraphNodeProperty); }
            set { SetValue(GraphNodeProperty, value); }
        }
        public static readonly DependencyProperty GraphNodeProperty = DependencyProperty.Register(
          nameof(GraphNode), typeof(LogGraphNode), typeof(LogGraphNodeDrawing), new PropertyMetadata(null, GraphNodeChanged));

        private static void GraphNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LogGraphNodeDrawing logGraphNodeDrawing))
                throw new ArgumentException("Invalid Type", nameof(d));
            logGraphNodeDrawing.GraphChanged();
        }

        private void GraphChanged()
        {
            _lines.Data = null;
            _circle.Data = null;
            if (GraphNode == null)
                return;
            _lines.Data = CreateGraph(GraphNode.Directions);
            _circle.Data = CreateCircle(GraphNode.GraphPosition);
            _circle.Fill = GraphNode.IsMerge ? HighlightFill : Fill;
        }

        private IEnumerable<Tuple<int, int>> MapDirections(IEnumerable<int> subDirection, int i)
        {
            foreach (var direction in subDirection)
                yield return Tuple.Create(direction, i);
        }

        private PathGeometry CreateGraph(List<LogGraphNode.DownwardDirections> directions)
        {
            var figures = directions.SelectMany((subDirection, i) => MapDirections(subDirection.Previous, i))
                                    .Select((direction) =>
            {
                PathSegmentCollection pathSegmentCollection = new PathSegmentCollection
                {
                    new LineSegment() { Point = new Point((0.5 + direction.Item1) * HorizontalDistance, -VerticalDistance / 2.5) },
                    new LineSegment() { Point = new Point((0.5 + direction.Item1) * HorizontalDistance, 0) }
                };
                return new PathFigure
                {
                    StartPoint = new Point(0.5 * (1 + direction.Item1 + direction.Item2) * HorizontalDistance, -VerticalDistance / 2 - 0.5),
                    Segments = pathSegmentCollection
                };
            }).Concat(
                directions.SelectMany((subDirection, i) => MapDirections(subDirection.Next, i))
                                    .Select((direction) =>
                                    {
                                        PathSegmentCollection pathSegmentCollection = new PathSegmentCollection
                                        {
                                            new LineSegment() { Point = new Point((0.5 + direction.Item2) * HorizontalDistance, VerticalDistance / 2.5) },
                                            new LineSegment() { Point = new Point((0.5 + direction.Item2) * HorizontalDistance, 0) }
                                        };
                                        return new PathFigure
                                        {
                                            StartPoint = new Point(0.5 * (1 + direction.Item1 + direction.Item2) * HorizontalDistance, VerticalDistance / 2 + 0.5),
                                            Segments = pathSegmentCollection
                                        };
                                    }
                ));
            
            var pathFigureCollection = new PathFigureCollection();
            foreach (var figure in figures)
                pathFigureCollection.Add(figure);
            return new PathGeometry() { Figures = pathFigureCollection };
        }

        private EllipseGeometry CreateCircle(int graphPosition)
        {
            return new EllipseGeometry()
            {
                Center = new Point((0.5 + graphPosition) * HorizontalDistance, ActualHeight / 2),
                RadiusX = Radius,
                RadiusY = Radius,
            };
        }
    }
}
