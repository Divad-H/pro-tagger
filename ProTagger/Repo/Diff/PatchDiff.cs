using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProTagger.Repo.Diff
{
    class PatchDiff
    {
        public readonly Patch Patch;
        public readonly CancellableChanges CancellableChanges;

        private PatchDiff(Patch patch, CancellableChanges cancellableChanges)
            => (Patch, CancellableChanges) = (patch, cancellableChanges);

        internal static Variant<PatchDiff, Variant<CancellableChanges, CancellableChangesWithError>> CreateDiff(
                IRepositoryWrapper repository,
                Commit? oldCommit,
                Commit newCommit,
                IEnumerable<string> paths,
                CompareOptions options,
                CancellableChanges cancellableChanges)
        {
            try
            {
                if (cancellableChanges.Cancellation.Token.IsCancellationRequested)
                    return new Variant<PatchDiff, Variant<CancellableChanges, CancellableChangesWithError>>(
                        new Variant<CancellableChanges, CancellableChangesWithError>(cancellableChanges));
                return new Variant<PatchDiff, Variant<CancellableChanges, CancellableChangesWithError>>(
                    new PatchDiff(repository.Diff.Compare<Patch>(oldCommit?.Tree ?? newCommit.Parents.FirstOrDefault()?.Tree, newCommit.Tree, paths, options), cancellableChanges));
            }
            catch (Exception e)
            {
                return new Variant<PatchDiff, Variant<CancellableChanges, CancellableChangesWithError>>(
                    new Variant<CancellableChanges, CancellableChangesWithError>(new CancellableChangesWithError(cancellableChanges, e.Message)));
            }
        }
    }
}
