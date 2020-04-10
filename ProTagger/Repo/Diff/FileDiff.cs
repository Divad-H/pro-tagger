using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProTagger.Repo.Diff
{
    static class FileDiff
    {
        internal static Variant<List<TreeEntryChanges>, string> CreateDiff(
                IRepositoryWrapper repository,
                Commit? oldCommit,
                Commit newCommit,
                CompareOptions compareOptions)
        {
            try
            {
                using var treeChanges = repository.Diff.Compare<TreeChanges>(
                    oldCommit?.Tree ?? newCommit.Parents.FirstOrDefault()?.Tree, newCommit.Tree, compareOptions);
                return new Variant<List<TreeEntryChanges>, string>(
                    treeChanges.Added
                        .Concat(treeChanges.Deleted)
                        .Concat(treeChanges.Modified)
                        .Concat(treeChanges.TypeChanged)
                        .Concat(treeChanges.Renamed)
                        .Concat(treeChanges.Copied)
                        .Concat(treeChanges.Unmodified)
                        .Concat(treeChanges.Conflicted)
                        .ToList());
            }
            catch (Exception e)
            {
                return new Variant<List<TreeEntryChanges>, string>(e.Message);
            }
        }
    }
}
