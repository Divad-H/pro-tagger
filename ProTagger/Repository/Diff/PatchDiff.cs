using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProTagger.Repository.Diff
{
    public class PatchDiff
    {
        public List<DiffAnalyzer.Hunk> Hunks { get; }
        public PatchEntryChanges PatchEntryChanges { get; }
        public CancellableChanges CancellableChanges { get; }

        public PatchDiff(List<DiffAnalyzer.Hunk> hunks, PatchEntryChanges patchEntryChanges, CancellableChanges cancellableChanges)
          => (Hunks, PatchEntryChanges, CancellableChanges) = (hunks, patchEntryChanges, cancellableChanges);

        public static Variant<IList<PatchDiff>, CancellableChangesWithError> CreateDiff(LibGit2Sharp.Diff diff,
                IDisposable? delayDisposeRepository,
                Branch? head,
                Variant<Commit, DiffTargets>? oldCommit,
                Variant<Commit, DiffTargets> newCommit,
                IEnumerable<string> paths,
                CompareOptions options,
                CancellableChanges cancellableChanges)
        {
            if (delayDisposeRepository is null)
                return new CancellableChangesWithError(cancellableChanges, "Repository was disposed.");
            try
            {
                if (cancellableChanges.Cancellation.Token.IsCancellationRequested)
                    return new List<PatchDiff>();
                if (newCommit == oldCommit)
                    return new List<PatchDiff>();

                var oldTarget = oldCommit is null
                    ? new Variant<Tree, DiffTargets, bool>(false)
                    : oldCommit.Visit<Variant<Tree, DiffTargets, bool>>(c => c.Tree, t => t);
                using var patch =
                    newCommit.Visit(
                        c => oldTarget.Visit(
                            oldTree => diff.Compare<Patch>(oldTree, c.Tree, paths, options),
                            oldDiffTarget => diff.Compare<Patch>(c.Tree, oldDiffTarget, paths, null, options),
                            _ => diff.Compare<Patch>(c.Parents.FirstOrDefault()?.Tree, c.Tree, paths, options)),
                        dt => oldTarget.Visit(
                            oldTree => diff.Compare<Patch>(oldTree, dt, paths, null, options),
                            oldDiffTarget => dt == DiffTargets.WorkingDirectory
                                ? diff.Compare<Patch>(paths, true)
                                : diff.Compare<Patch>(head?.Tip?.Tree, dt, paths, null, options),
                            _ => diff.Compare<Patch>(paths, true)));
                
                return patch
                    .Select(patchEntry => new PatchDiff(
                        DiffAnalyzer.SplitIntoHunks(patchEntry.Patch, cancellableChanges.Cancellation.Token),
                        patchEntry,
                        cancellableChanges))
                    .ToList();
            }
            catch (Exception e)
            {
                return new CancellableChangesWithError(cancellableChanges, e.Message);
            }
            finally
            {
                delayDisposeRepository.Dispose();
            }
        }
    }
}
