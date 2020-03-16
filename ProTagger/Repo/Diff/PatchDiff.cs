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
        public readonly TreeEntryChanges TreeEntryChanges;

        private PatchDiff(Patch patch, TreeEntryChanges treeEntryChanges)
            => (Patch, TreeEntryChanges) = (patch, treeEntryChanges);

        internal static Variant<PatchDiff, string> CreateDiff(
                IRepositoryWrapper repository,
                Commit? oldCommit,
                Commit newCommit,
                IEnumerable<string> paths,
                TreeEntryChanges treeEntryChanges)
        {
            try
            {
                return new Variant<PatchDiff, string>(
                    new PatchDiff(repository.Diff.Compare<Patch>(oldCommit?.Tree ?? newCommit.Parents.FirstOrDefault()?.Tree, newCommit.Tree, paths), treeEntryChanges));
            }
            catch (Exception e)
            {
                return new Variant<PatchDiff, string>(e.Message);
            }
        }
    }
}
