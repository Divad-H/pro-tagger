using LibGit2Sharp;
using ProTagger.Wpf;
using ReacitveMvvm;
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
        public IntegerInputViewModel ContextLinesInput { get; }
        public IntegerInputViewModel InterhunkLinesInput { get; }
        public EnumViewModel<DiffAlgorithm> DiffAlgorithm { get; }

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

        public SimilarityOptionsViewModel SimilarityOptions { get; }

        public CompareOptionsViewModel(ISchedulers schedulers, CompareOptions compareOptions)
        {
            ContextLinesInput = new IntegerInputViewModel(schedulers, compareOptions.ContextLines, 0);
            InterhunkLinesInput = new IntegerInputViewModel(schedulers, compareOptions.InterhunkLines, 0);
            DiffAlgorithm = new EnumViewModel<DiffAlgorithm>(compareOptions.Algorithm);
            _indentHeuristic = compareOptions.IndentHeuristic;
            SimilarityOptions = new SimilarityOptionsViewModel(schedulers, compareOptions.Similarity);

            CompareOptionsObservable = Observable
                .CombineLatest(ContextLinesInput.ValueObservable,
                    InterhunkLinesInput.ValueObservable,
                    DiffAlgorithm.ValueObservable,
                    this.FromProperty(vm => vm.IndentHeuristic),
                    SimilarityOptions.SimilarityObservable,
                      (contextLines, 
                      interhunkLines, 
                      algorithm, 
                      indentHeuristic,
                      similarityOptions) 
                    => new CompareOptions()
                    {
                        ContextLines = contextLines,
                        InterhunkLines = interhunkLines,
                        Similarity = similarityOptions,
                        IncludeUnmodified = false,
                        Algorithm = algorithm,
                        IndentHeuristic = indentHeuristic,
                    });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
