using LibGit2Sharp;
using ProTagger.Wpf;
using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ProTagger.Configuration
{
    public class CompareOptionsViewModel : IDisposable
    {
        public IObservable<CompareOptions> CompareOptionsObservable { get; }
        public IntegerInputViewModel ContextLinesInput { get; }
        public IntegerInputViewModel InterhunkLinesInput { get; }
        public EnumViewModel<DiffAlgorithm> DiffAlgorithm { get; }

        public SubjectBase<bool> IndentHeuristic { get; }

        public SimilarityOptionsViewModel SimilarityOptions { get; }

        public CompareOptionsViewModel(ISchedulers schedulers, CompareOptions compareOptions)
        {
            ContextLinesInput = new IntegerInputViewModel(schedulers, compareOptions.ContextLines, 0)
                .DisposeWith(_disposable);
            InterhunkLinesInput = new IntegerInputViewModel(schedulers, compareOptions.InterhunkLines, 0)
                .DisposeWith(_disposable);
            DiffAlgorithm = new EnumViewModel<DiffAlgorithm>(compareOptions.Algorithm)
                .DisposeWith(_disposable);
            IndentHeuristic = new ViewSubject<bool>(compareOptions.IndentHeuristic)
                .DisposeWith(_disposable);
            SimilarityOptions = new SimilarityOptionsViewModel(schedulers, compareOptions.Similarity)
                .DisposeWith(_disposable);

            CompareOptionsObservable = Observable
                .CombineLatest(ContextLinesInput.Value,
                    InterhunkLinesInput.Value,
                    DiffAlgorithm.Value,
                    IndentHeuristic,
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

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public void Dispose()
            => _disposable.Dispose();
    }
}
