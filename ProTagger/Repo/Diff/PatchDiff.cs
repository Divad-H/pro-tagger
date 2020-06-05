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

        public static Variant<IList<PatchDiff>, CancellableChangesWithError> CreateDiff(LibGit2Sharp.Diff diff,
                IDisposable? delayDisposeRepository,
                Branch? head,
                Variant<Commit, DiffTargets>? oldCommit,
                Variant<Commit, DiffTargets> newCommit,
                IEnumerable<string> paths,
                CompareOptions options,
                CancellableChanges cancellableChanges)
        {
            if (delayDisposeRepository == null)
                return new Variant<IList<PatchDiff>, CancellableChangesWithError>(
                    new CancellableChangesWithError(cancellableChanges, "Repository was disposed."));
            try
            {
                if (cancellableChanges.Cancellation.Token.IsCancellationRequested)
                    return new Variant<IList<PatchDiff>, CancellableChangesWithError>(new List<PatchDiff>());

                if (oldCommit != null && oldCommit.Is<DiffTargets>())
                    (oldCommit, newCommit) = (newCommit, oldCommit);
                if (oldCommit != null && oldCommit.Is<DiffTargets>())
                    return new Variant<IList<PatchDiff>, CancellableChangesWithError>(new List<PatchDiff>());

                var oldTree = oldCommit?.Get<Commit>().Tree;
                using var patch =
                   newCommit.Visit(
                       c => diff.Compare<Patch>(oldTree ?? c.Parents.FirstOrDefault()?.Tree, c.Tree, paths, options),
                       dt => diff.Compare<Patch>(oldTree ?? head?.Tip.Tree, dt, paths, null, options));
                
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
            finally
            {
                delayDisposeRepository.Dispose();
            }
        }
    }
}
