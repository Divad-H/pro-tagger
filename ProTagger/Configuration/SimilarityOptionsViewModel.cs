using LibGit2Sharp;
using ProTagger.Wpf;
using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ProTagger.Configuration
{
    public class SimilarityOptionsViewModel : IDisposable
    {
        public EnumViewModel<RenameDetectionMode> RenameDetectionMode { get; }
        public IntegerInputViewModel BreakRewriteThresholdInput { get; }
        public IntegerInputViewModel CopyThresholdInput { get; }
        public IntegerInputViewModel RenameFromRewriteThresholdInput { get; }
        public IntegerInputViewModel RenameLimitInput { get; }
        public IntegerInputViewModel RenameThresholdInput { get; }
        public EnumViewModel<WhitespaceMode> WhitespaceMode { get; }

        public IObservable<SimilarityOptions> SimilarityObservable { get; }

        public SimilarityOptionsViewModel(ISchedulers schedulers, SimilarityOptions similarityOptions)
        {
            RenameDetectionMode = new EnumViewModel<RenameDetectionMode>(similarityOptions.RenameDetectionMode)
                .DisposeWith(_disposable);
            BreakRewriteThresholdInput = new IntegerInputViewModel(schedulers, similarityOptions.BreakRewriteThreshold)
                .DisposeWith(_disposable);
            CopyThresholdInput = new IntegerInputViewModel(schedulers, similarityOptions.CopyThreshold)
                .DisposeWith(_disposable);
            RenameFromRewriteThresholdInput = new IntegerInputViewModel(schedulers, similarityOptions.RenameFromRewriteThreshold)
                .DisposeWith(_disposable);
            RenameLimitInput = new IntegerInputViewModel(schedulers, similarityOptions.RenameLimit)
                .DisposeWith(_disposable);
            RenameThresholdInput = new IntegerInputViewModel(schedulers, similarityOptions.RenameThreshold)
                .DisposeWith(_disposable);
            WhitespaceMode = new EnumViewModel<WhitespaceMode>(similarityOptions.WhitespaceMode)
                .DisposeWith(_disposable);

            SimilarityObservable = Observable
                .CombineLatest(
                    RenameDetectionMode.Value,
                    BreakRewriteThresholdInput.Value,
                    CopyThresholdInput.Value,
                    RenameFromRewriteThresholdInput.Value,
                    RenameLimitInput.Value,
                    RenameThresholdInput.Value,
                    WhitespaceMode.Value,
                    (
                        renameDetectionMode,
                        breakRewriteThreshold,
                        copyThreshold,
                        renameFromRewirteThreshold,
                        renameLimit,
                        renameThreshold,
                        whitespaceMode)
                            => new SimilarityOptions()
                            {
                                RenameDetectionMode = renameDetectionMode,
                                BreakRewriteThreshold = breakRewriteThreshold,
                                CopyThreshold = copyThreshold,
                                RenameFromRewriteThreshold = renameFromRewirteThreshold,
                                RenameLimit = renameLimit,
                                RenameThreshold = renameThreshold,
                                WhitespaceMode = whitespaceMode,
                            });
        }


        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public void Dispose()
            => _disposable.Dispose();
    }
}
