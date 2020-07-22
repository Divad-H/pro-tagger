using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ProTagger.Utilities
{
    public static class DiffAnalyzer
    {
        public readonly struct Token
        {
            public readonly string Text;
            public readonly bool Unchanged;

            public Token(string text, bool unchanged)
                => (Text, Unchanged) = (text, unchanged);
        }
        public readonly struct ChangedLine
        {
            public readonly int LineNumber;
            public readonly List<Token> Text;

            public ChangedLine(int lineNumber, List<Token> text)
                => (LineNumber, Text) = (lineNumber, text);
        }
        public readonly struct WordDiff
        {
            public readonly List<ChangedLine> NewText;
            public readonly List<ChangedLine> OldText;

            public WordDiff(List<ChangedLine> newText, List<ChangedLine> oldText)
                => (NewText, OldText) = (newText, oldText);
        }

        public readonly struct UnchangedLine
        {
            public readonly int NewLineNumber;
            public readonly int OldLineNumber;
            public readonly string Text;

            public UnchangedLine(int oldLineNumber, int newLineNumber, string text)
                => (NewLineNumber, OldLineNumber, Text) = (newLineNumber, oldLineNumber, text);
        }

        public readonly struct Hunk
        {
            public int NewBeginLine { get; }
            public int NewHunkLength { get; }
            public int OldBeginLine { get; }
            public int OldHunkLength { get; }
            public readonly string AdditionalInfo;

            public readonly List<Variant<WordDiff, UnchangedLine>> Diff;

            public Hunk(int newBeginLine, int newHunkLength, int oldBeginLine, int oldHunkLength, string additionalInfo, List<Variant<WordDiff, UnchangedLine>> diff)
            {
                NewBeginLine = newBeginLine;
                NewHunkLength = newHunkLength;
                OldBeginLine = oldBeginLine;
                OldHunkLength = oldHunkLength;
                AdditionalInfo = additionalInfo;
                Diff = diff;
            }
        }

        static internal List<Hunk> SplitIntoHunks(string rawFilePatch, CancellationToken ct)
        {
            var rawHunks = rawFilePatch.Split("\n@@");

            var result = new List<Hunk>();
            foreach (var rawHunk in rawHunks.Skip(1))
            {
                if (ct.IsCancellationRequested)
                    return new List<Hunk>();
                var lineInfoLength = rawHunk.IndexOf("@@");
                var lineInfo = rawHunk.Substring(0, lineInfoLength);
                var oldNewTokens = lineInfo.Split(new char[] { ' ', '-', '+' }, StringSplitOptions.RemoveEmptyEntries);
                if (oldNewTokens.Length < 2)
                    throw new ArgumentException("Unexpected diff format, cannot parse line numbers.");
                var tokens = oldNewTokens.Select(token => token.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)).ToList();
                var firstLineEndIndex = rawHunk.IndexOf('\n');
                var oldFirstLine = tokens[0].Length > 0 ? int.Parse(tokens[0][0]) : 0;
                var newFirstLine = tokens[1].Length > 0 ? int.Parse(tokens[1][0]) : 0;
                result.Add(new Hunk(newFirstLine, tokens[1].Length > 1 ? int.Parse(tokens[1][1]) : 0, oldFirstLine, tokens[0].Length > 1 ? int.Parse(tokens[0][1]) : 0,
                    rawHunk.Substring(lineInfoLength + 2, firstLineEndIndex - lineInfoLength - 2).Trim(),
                    ParseHunk(rawHunk.Substring(firstLineEndIndex + "\n".Length), newFirstLine, oldFirstLine, ct)));
            }
            return result;
        }

        static List<Variant<WordDiff, UnchangedLine>> ParseHunk(string rawHunkContent, int newLine, int oldLine, CancellationToken ct)
        {
            var lines = rawHunkContent.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new List<Variant<WordDiff, UnchangedLine>>();
            var currentOldLines = new List<string>();
            var currentNewLines = new List<string>();
            foreach (var line in lines)
            {
                if (ct.IsCancellationRequested)
                    return new List<Variant<WordDiff, UnchangedLine>>();
                if (line.Length == 0 || line.ElementAt(0) == ' ')
                {
                    if (currentNewLines.Count > 0 || currentOldLines.Count > 0)
                    {
                        result.Add(new Variant<WordDiff, UnchangedLine>(AnalyzeWordDiff(currentNewLines, newLine, currentOldLines, oldLine)));
                        newLine += currentNewLines.Count;
                        oldLine += currentOldLines.Count;
                        currentNewLines.Clear();
                        currentOldLines.Clear();
                    }
                    result.Add(new Variant<WordDiff, UnchangedLine>(new UnchangedLine(oldLine++, newLine++, line.Any() ? line.Substring(1) : line)));
                }
                else if (line.ElementAt(0) == '-')
                {
                    System.Diagnostics.Debug.Assert(currentNewLines.Count == 0, "Unexpected diff format.");
                    currentOldLines.Add(line.Substring(1));
                }
                else if (line.ElementAt(0) == '+')
                {
                    currentNewLines.Add(line.Substring(1));
                }
            }
            if (currentNewLines.Count > 0 || currentOldLines.Count > 0)
                result.Add(new Variant<WordDiff, UnchangedLine>(AnalyzeWordDiff(currentNewLines, newLine, currentOldLines, oldLine)));
            return result;
        }

        private class IndexedToken
        {
            public string Text;
            public bool Unchanged;
            public int Line;

            public IndexedToken(string text, int line)
                => (Text, Unchanged, Line) = (text, true, line);
        }

        private struct CurrentBest
        {
            public int Index;
            public int Diff;
        }

        static internal WordDiff AnalyzeWordDiff(List<string> newLines, int newLineNumber, List<string> oldLines, int oldLineNumber)
        {
            var newSplitLines = newLines
                .SelectMany((newLine, index) => SplitLine(newLine)
                    .Select(text => new IndexedToken(text, index + newLineNumber)))
                .ToList();
            var oldSplitLines = oldLines
                .SelectMany((oldLine, index) => SplitLine(oldLine)
                    .Select(text => new IndexedToken(text, index + oldLineNumber)))
                .ToList();

            // int oldI = 0; // old diff is checked up to this token
            int currentOldOffset = 0;
            int currentNewOffset = 0;
            var currentBest = new CurrentBest() { Diff = 0, Index = -1 };
            for (int i = 0; i < newSplitLines.Count; ++i)
            {
                if (string.IsNullOrWhiteSpace(newSplitLines[i].Text) && i < newSplitLines.Count - 1)
                    continue;
                else if (!string.IsNullOrWhiteSpace(newSplitLines[i].Text))
                {
                    var oldIndex = oldSplitLines.FindIndex(currentOldOffset, m => m.Text == newSplitLines[i].Text);
                    if (oldIndex == -1)
                    {
                        // The new token was not found in the old text
                        newSplitLines[i].Unchanged = false;
                        if (i < newSplitLines.Count - 1)
                            continue;
                    }
                    else
                    {
                        var diff = oldIndex - currentOldOffset - (i - currentNewOffset);
                        if (diff == 0 || diff == 1)
                        {
                            for (int j = currentOldOffset; j < oldIndex; ++j)
                                oldSplitLines[j].Unchanged = string.IsNullOrWhiteSpace(oldSplitLines[j].Text);
                            for (int j = currentNewOffset; j < i; ++j)
                                newSplitLines[j].Unchanged = string.IsNullOrWhiteSpace(newSplitLines[j].Text);
                            currentBest.Index = -1;
                            currentOldOffset = oldIndex + 1;
                            currentNewOffset = i + 1;
                            continue;
                        }
                        if (currentBest.Index == -1 || diff + i < currentBest.Diff + currentBest.Index)
                        {
                            // Better match was found.
                            currentBest.Index = i;
                            currentBest.Diff = diff;
                        }
                    }
                }
                if (currentBest.Index != -1 && (currentBest.Diff + currentBest.Index <= i + 1 || i == newSplitLines.Count - 1))
                {
                    // Accept this as similar
                    var similarOldIndex = currentOldOffset + currentBest.Index - currentNewOffset + currentBest.Diff;
                    for (int j = currentOldOffset; j < similarOldIndex; ++j)
                        oldSplitLines[j].Unchanged = string.IsNullOrWhiteSpace(oldSplitLines[j].Text);
                    for (int j = currentNewOffset; j < currentBest.Index; ++j)
                        newSplitLines[j].Unchanged = string.IsNullOrWhiteSpace(newSplitLines[j].Text);
                    currentOldOffset = similarOldIndex + 1;
                    currentNewOffset = currentBest.Index + 1;
                    i = currentBest.Index;
                    currentBest.Index = -1;
                    continue;
                }
            }
            for (int i = currentOldOffset; i < oldSplitLines.Count; ++i)
                if (!string.IsNullOrWhiteSpace(oldSplitLines[i].Text))
                    oldSplitLines[i].Unchanged = false;
            if (newSplitLines.Count > 1 && string.IsNullOrWhiteSpace(newSplitLines[0].Text))
                newSplitLines[0].Unchanged = newSplitLines[1].Unchanged;
            for (int i = 1; i < newSplitLines.Count; ++i)
                if (string.IsNullOrWhiteSpace(newSplitLines[i].Text))
                    newSplitLines[i].Unchanged = newSplitLines[i - 1].Unchanged;
            if (oldSplitLines.Count > 1 && string.IsNullOrWhiteSpace(oldSplitLines[0].Text))
                oldSplitLines[0].Unchanged = oldSplitLines[1].Unchanged;
            for (int i = 1; i < oldSplitLines.Count; ++i)
                if (string.IsNullOrWhiteSpace(oldSplitLines[i].Text))
                    oldSplitLines[i].Unchanged = oldSplitLines[i - 1].Unchanged;

            var newText = newSplitLines
                .GroupBy(token => token.Line, (lineNumber, tokens) => new ChangedLine(lineNumber, tokens
                    .Select(indexedToken => new Token(indexedToken.Text, indexedToken.Unchanged))
                    .GroupAdjacent((first, second) => first.Unchanged == second.Unchanged, tokens => tokens
                        .Aggregate((first, second) => new Token(first.Text + second.Text, first.Unchanged)))
                    .ToList()));
            var oldText = oldSplitLines
                .GroupBy(token => token.Line, (lineNumber, tokens) => new ChangedLine(lineNumber, tokens
                    .Select(indexedToken => new Token(indexedToken.Text, indexedToken.Unchanged))
                    .GroupAdjacent((first, second) => first.Unchanged == second.Unchanged, tokens => tokens
                        .Aggregate((first, second) => new Token(first.Text + second.Text, first.Unchanged)))
                    .ToList()));
            return new WordDiff(newText.ToList(), oldText.ToList());
        }

        internal static IEnumerable<string> SplitLine(string line)
        {
            if (!line.Any())
                return line.Yield();
            return Regex.Matches(line, @"(\s+|[^\w\s]+|\w+)", RegexOptions.Compiled | RegexOptions.CultureInvariant)
                .Select(match => match.Value);
        }
    }
}
