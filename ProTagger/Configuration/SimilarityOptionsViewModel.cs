using LibGit2Sharp;
using ProTagger.Wpf;
using ReacitveMvvm;
using System;
using System.Reactive.Linq;

namespace ProTagger.Configuration
{
    public class SimilarityOptionsViewModel
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
            RenameDetectionMode = new EnumViewModel<RenameDetectionMode>(similarityOptions.RenameDetectionMode);
            BreakRewriteThresholdInput = new IntegerInputViewModel(schedulers, similarityOptions.BreakRewriteThreshold);
            CopyThresholdInput = new IntegerInputViewModel(schedulers, similarityOptions.CopyThreshold);
            RenameFromRewriteThresholdInput = new IntegerInputViewModel(schedulers, similarityOptions.RenameFromRewriteThreshold);
            RenameLimitInput = new IntegerInputViewModel(schedulers, similarityOptions.RenameLimit);
            RenameThresholdInput = new IntegerInputViewModel(schedulers, similarityOptions.RenameThreshold);
            WhitespaceMode = new EnumViewModel<WhitespaceMode>(similarityOptions.WhitespaceMode);

            SimilarityObservable = Observable
                .CombineLatest(
                    RenameDetectionMode.ValueObservable,
                    BreakRewriteThresholdInput.ValueObservable,
                    CopyThresholdInput.ValueObservable,
                    RenameFromRewriteThresholdInput.ValueObservable,
                    RenameLimitInput.ValueObservable,
                    RenameThresholdInput.ValueObservable,
                    WhitespaceMode.ValueObservable,
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
    }
}
