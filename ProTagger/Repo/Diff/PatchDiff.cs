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
        public readonly CancelableChanges CancelableChanges;

        private PatchDiff(Patch patch, CancelableChanges cancelableChanges)
            => (Patch, CancelableChanges) = (patch, cancelableChanges);

        internal static Variant<PatchDiff, Variant<CancelableChanges, CancelableChangesWithError>> CreateDiff(
                IRepositoryWrapper repository,
                Commit? oldCommit,
                Commit newCommit,
                IEnumerable<string> paths,
                CancelableChanges cancelableChanges)
        {
            try
            {
                if (cancelableChanges.Cancellation.Token.IsCancellationRequested)
                    return new Variant<PatchDiff, Variant<CancelableChanges, CancelableChangesWithError>>(
                        new Variant<CancelableChanges, CancelableChangesWithError>(cancelableChanges));
                return new Variant<PatchDiff, Variant<CancelableChanges, CancelableChangesWithError>>(
                    new PatchDiff(repository.Diff.Compare<Patch>(oldCommit?.Tree ?? newCommit.Parents.FirstOrDefault()?.Tree, newCommit.Tree, paths), cancelableChanges));
            }
            catch (Exception e)
            {
                return new Variant<PatchDiff, Variant<CancelableChanges, CancelableChangesWithError>>(
                    new Variant<CancelableChanges, CancelableChangesWithError>(new CancelableChangesWithError(cancelableChanges, e.Message)));
            }
        }
    }
}
