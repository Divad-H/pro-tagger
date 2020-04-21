using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProTagger.Repo.Diff
{
    static class TreeDiff
    {
        internal static async Task<Variant<List<TreeEntryChanges>, string>> CreateDiff(
                IRefCount repositoryRefCounter,
                LibGit2Sharp.Diff diff,
                Commit? oldCommit,
                Commit newCommit,
                CompareOptions compareOptions)
        {
            using var delayDispose = repositoryRefCounter.AddRef();
            return await Task.Run(() =>
                {
                    try
                    {
                        using var treeChanges = diff.Compare<TreeChanges>(
                            oldCommit?.Tree ?? newCommit.Parents.FirstOrDefault()?.Tree, newCommit.Tree, compareOptions);
                        return new Variant<List<TreeEntryChanges>, string>(treeChanges.ToList());
                    }
                    catch (Exception e)
                    {
                        return new Variant<List<TreeEntryChanges>, string>(e.Message);
                    }
                });
        }
    }
}
