using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProTagger.Repo.Diff
{
    class FileDiff
    {
        internal static Variant<IEnumerable<TreeEntryChanges>, string> CreateDiff(
                IRepositoryWrapper repository,
                Commit? oldCommit,
                Commit newCommit)
        {
            try
            {
                var treeChanges = repository.Diff.Compare<TreeChanges>(oldCommit?.Tree ?? newCommit.Parents.FirstOrDefault()?.Tree, newCommit.Tree, new CompareOptions());
                return new Variant<IEnumerable<TreeEntryChanges>, string>(
                    treeChanges.Added
                        .Concat(treeChanges.Deleted)
                        .Concat(treeChanges.Modified)
                        .Concat(treeChanges.TypeChanged)
                        .Concat(treeChanges.Renamed)
                        .Concat(treeChanges.Copied)
                        .Concat(treeChanges.Unmodified)
                        .Concat(treeChanges.Conflicted));
            }
            catch (Exception e)
            {
                return new Variant<IEnumerable<TreeEntryChanges>, string>(e.Message);
            }
        }
    }
}
