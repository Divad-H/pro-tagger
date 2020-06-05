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
        internal static Task<Variant<List<TreeEntryChanges>, string>> CreateDiff(
                IRefCount repositoryRefCounter,
                LibGit2Sharp.Diff diff,
                Branch? head,
                Variant<Commit, DiffTargets>? oldCommit,
                Variant<Commit, DiffTargets> newCommit,
                CompareOptions compareOptions)
        {
            // TODO: Diff seems to be slower than status. For comparing the working-tree with HEAD, status should be used.
            // Idea: It might be easier to combine the tree diff between HEAD and the target commmit and the changed files
            // in git status and pass those into diff. The speed needs to be evaluated in this case.
            using var delayDispose = repositoryRefCounter.AddRef();
            return Task.Run(() =>
                {
                    try
                    {
                        if (oldCommit != null && oldCommit.Is<DiffTargets>())
                            (oldCommit, newCommit) = (newCommit, oldCommit);
                        if (oldCommit != null && oldCommit.Is<DiffTargets>())
                            return new Variant<List<TreeEntryChanges>, string>(new List<TreeEntryChanges>());
                        if (newCommit.Is<DiffTargets>() && head == null)
                            return new Variant<List<TreeEntryChanges>, string>("Did not get HEAD");
                        
                        var oldTree = oldCommit?.Get<Commit>().Tree;
                        using var treeChanges =
                           newCommit.Visit(
                               c => diff.Compare<TreeChanges>(oldTree ?? c.Parents.FirstOrDefault()?.Tree, c.Tree, compareOptions),
                               dt => diff.Compare<TreeChanges>(oldTree ?? head?.Tip.Tree, dt, null, null, compareOptions));
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
