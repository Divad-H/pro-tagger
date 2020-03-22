using LibGit2Sharp;
using ReactiveMvvm;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace ProTagger.Configuration
{
    public class CompareOptionsViewModel : INotifyPropertyChanged
    {
        public IObservable<CompareOptions> CompareOptionsObservable { get; }

        private int _contextLines;
        public int ContextLines
        {
            get => _contextLines;
            set
            {
                if (value == _contextLines)
                    return;
                _contextLines = value;
                NotifyPropertyChanged();
            }
        }
        private int _interhunkLines;
        public int InterhunkLines
        {
            get => _interhunkLines;
            set
            {
                if (value == _interhunkLines)
                    return;
                _interhunkLines = value;
                NotifyPropertyChanged();
            }
        }

        private DiffAlgorithm _algorithm;
        public DiffAlgorithm Algorithm
        {
            get => _algorithm;
            set
            {
                if (value == _algorithm)
                    return;
                _algorithm = value;
                NotifyPropertyChanged();
            }
        }

        private bool _indentHeuristic;
        public bool IndentHeuristic
        {
            get => _indentHeuristic;
            set
            {
                if (value == _indentHeuristic)
                    return;
                _indentHeuristic = value;
                NotifyPropertyChanged();
            }
        }
        public CompareOptionsViewModel(CompareOptions compareOptions)
        {
            _contextLines = compareOptions.ContextLines;
            _interhunkLines = compareOptions.InterhunkLines;
            _algorithm = compareOptions.Algorithm;
            _indentHeuristic = compareOptions.IndentHeuristic;

            var contextLinesObservable = this.FromProperty(vm => vm.ContextLines);
            var interhunkLinesObservable = this.FromProperty(vm => vm.InterhunkLines);
            var algorithmObservable = this.FromProperty(vm => vm.Algorithm);
            var indentHeuristicObservable = this.FromProperty(vm => vm.IndentHeuristic);

            CompareOptionsObservable = contextLinesObservable
                .WithLatestFrom(interhunkLinesObservable, (contextLines, interhunkLines) => new { contextLines, interhunkLines })
                .WithLatestFrom(algorithmObservable, (data, algorithm) => new { data.contextLines, data.interhunkLines, algorithm })
                .WithLatestFrom(indentHeuristicObservable, (data, indentHeuristic) => new CompareOptions()
                {
                    ContextLines = data.contextLines,
                    InterhunkLines = data.interhunkLines,
                    Similarity = compareOptions.Similarity,
                    IncludeUnmodified = compareOptions.IncludeUnmodified,
                    Algorithm = data.algorithm,
                    IndentHeuristic = indentHeuristic,
                })
                .StartWith(compareOptions);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
