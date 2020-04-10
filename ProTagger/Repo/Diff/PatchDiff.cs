using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProTagger.Repo.Diff
{
    public class PatchDiff
    {
        public List<DiffAnalyzer.Hunk> Hunks { get; }
        public PatchEntryChanges PatchEntryChanges { get; }
        public CancellableChanges CancellableChanges { get; }

        public PatchDiff(List<DiffAnalyzer.Hunk> hunks, PatchEntryChanges patchEntryChanges, CancellableChanges cancellableChanges)
          => (Hunks, PatchEntryChanges, CancellableChanges) = (hunks, patchEntryChanges, cancellableChanges);

        public static Variant<IList<PatchDiff>, CancellableChangesWithError> CreateDiff(IRepositoryWrapper repository,
                    Commit? oldCommit,
                    Commit newCommit,
                    IEnumerable<string> paths,
                    CompareOptions options,
                    CancellableChanges cancellableChanges)
        {
            try
            {
                if (cancellableChanges.Cancellation.Token.IsCancellationRequested)
                    return new Variant<IList<PatchDiff>, CancellableChangesWithError>(new List<PatchDiff>());
                using var patch = repository.Diff.Compare<Patch>(
                    oldCommit?.Tree ?? newCommit.Parents.FirstOrDefault()?.Tree, newCommit.Tree, paths, options);
                return new Variant<IList<PatchDiff>, CancellableChangesWithError>(patch
                        .Select(patchEntry => new PatchDiff(
                            DiffAnalyzer.SplitIntoHunks(patchEntry.Patch, cancellableChanges.Cancellation.Token), 
                            patchEntry, 
                            cancellableChanges))
                        .ToList());
            }
            catch (Exception e)
            {
                return new Variant<IList<PatchDiff>, CancellableChangesWithError>(
                    new CancellableChangesWithError(cancellableChanges, e.Message));
            }
        }
    }
}
