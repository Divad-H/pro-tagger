using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using static ProTagger.Utilities.DiffAnalyzer;

namespace ProTagger.Wpf
{
    public class PatchDiffFormatter : FlowDocument
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
            => ((PatchDiffFormatter)d).UpdateContent_((Hunk?)e.NewValue);

        private void UpdateContent_(Hunk? content)
        {
            Blocks.Clear();
            if (!content.HasValue)
                return;

            const double lineNumberTableWidth = 35;

            var table = new Table();
            table.Columns.Add(new TableColumn() { Width = new GridLength(lineNumberTableWidth, GridUnitType.Pixel) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(lineNumberTableWidth, GridUnitType.Pixel) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Auto) });
            var tableRowGroup = new TableRowGroup();
            Paragraph paragraph = new Paragraph();
            var removedLineParagraph = new Paragraph();
            var addedLineParagraph = new Paragraph();

            static Run formatLineNumber(int number)
            {
                var str = number.ToString();
                if (str.Length > 5)
                    str = "…" + str.Substring(str.Length - 4, 4);
                return new Run(str + "\n");
            }

            List<Run> lineEndings = new List<Run>();
            var currentIndex = 0;

            void addRun(Run run)
            {
                currentIndex += run.Text.Length;
                paragraph.Inlines.Add(run);
            }
            void endLine()
            {
                var newLine = new Run("\n");
                paragraph.Inlines.Add(newLine);
                lineEndings.Add(newLine);
                var s = newLine.ContentStart.GetTextInRun(LogicalDirection.Forward);
            }

            foreach (var variant in content.Value.Diff)
                variant.Visit(wordDiff =>
                {
                    foreach (var oldLine in wordDiff.OldText)
                    {
                        var lineNumberFormatted = formatLineNumber(oldLine.LineNumber);
                        lineNumberFormatted.Foreground = OldForegroundBrush;
                        removedLineParagraph.Inlines.Add(lineNumberFormatted);
                        addedLineParagraph.Inlines.Add(new Run("\n"));
                        foreach (var token in oldLine.Text)
                            addRun(new Run(token.Text) { Background = token.Unchanged ? OldNormalBrush : OldHighlightBrush });
                        endLine();
                    }
                    foreach (var newLine in wordDiff.NewText)
                    {
                        removedLineParagraph.Inlines.Add(new Run("\n"));
                        var lineNumberFormatted = formatLineNumber(newLine.LineNumber);
                        lineNumberFormatted.Foreground = NewForegroundBrush;
                        addedLineParagraph.Inlines.Add(lineNumberFormatted);
                        foreach (var token in newLine.Text)
                            addRun(new Run(token.Text) { Background = token.Unchanged ? NewNormalBrush : NewHighlightBrush });
                        endLine();
                    }
                },
                unchangedLine =>
                {
                    removedLineParagraph.Inlines.Add(formatLineNumber(unchangedLine.OldLineNumber));
                    addedLineParagraph.Inlines.Add(new Run(unchangedLine.NewLineNumber.ToString() + "\n"));
                    addRun(new Run(unchangedLine.Text));
                    endLine();
                });

            var tableRow = new TableRow();
            tableRow.Cells.Add(new TableCell(removedLineParagraph));
            tableRow.Cells.Add(new TableCell(addedLineParagraph));
            ContentTableCell = new TableCell(paragraph);
            tableRow.Cells.Add(ContentTableCell);
            tableRowGroup.Rows.Add(tableRow);
            table.RowGroups.Add(tableRowGroup);
            Blocks.Add(table);

            LineEndings = lineEndings;
        }

        public List<Run>? LineEndings;
        public TableCell? ContentTableCell;

        static readonly Brush OldHighlightBrush = new SolidColorBrush(Color.FromArgb(125, 255, 0, 0));
        static readonly Brush OldNormalBrush = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0));
        static readonly Brush OldForegroundBrush = new SolidColorBrush(Color.FromArgb(255, 176, 64, 58));
        static readonly Brush NewHighlightBrush = new SolidColorBrush(Color.FromArgb(125, 0, 255, 0));
        static readonly Brush NewNormalBrush = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0));
        static readonly Brush NewForegroundBrush = new SolidColorBrush(Color.FromArgb(255, 45, 134, 45));
        static PatchDiffFormatter()
        {
            NewHighlightBrush.Freeze();
            NewNormalBrush.Freeze();
            NewForegroundBrush.Freeze();
            OldHighlightBrush.Freeze();
            OldNormalBrush.Freeze();
            OldForegroundBrush.Freeze();
        }
    }
}
