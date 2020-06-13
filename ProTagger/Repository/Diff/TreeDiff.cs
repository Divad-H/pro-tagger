using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace ProTagger.Repository.Diff
{
    static class TreeDiff
    {
        internal static Task<Variant<List<TreeEntryChanges>, Unexpected>> CreateDiff(
                IRepositoryWrapper repo,
                CancellationToken ct,
                Branch? head,
                Variant<Commit, DiffTargets>? oldCommit,
                Variant<Commit, DiffTargets> newCommit,
                CompareOptions compareOptions)
        {
            using var delayDispose = repo.AddRef();
            return Task.Run(() =>
                {
                    try
                    {
                        if (ct.IsCancellationRequested)
                            return new Variant<List<TreeEntryChanges>, Unexpected>(new Unexpected("The operation was cancelled."));
                        if (oldCommit != null && oldCommit.Is<DiffTargets>())
                            (oldCommit, newCommit) = (newCommit, oldCommit);
                        if (oldCommit != null && oldCommit.Is<DiffTargets>())
                            return new Variant<List<TreeEntryChanges>, Unexpected>(new List<TreeEntryChanges>());
                        if (newCommit.Is<DiffTargets>() && head is null)
                            return new Variant<List<TreeEntryChanges>, Unexpected>(new Unexpected("Did not get HEAD"));
                        
                        var oldTree = oldCommit?.Get<Commit>().Tree;
                        using var treeChanges =
                           newCommit.Visit(
                               c => repo.Diff.Compare<TreeChanges>(oldTree ?? c.Parents.FirstOrDefault()?.Tree, c.Tree, compareOptions),
                               dt =>
                               {
                                   var changedFiles = repo.RetrieveStatus(new StatusOptions()
                                    { DetectRenamesInIndex = true, DetectRenamesInWorkDir = true, IncludeUntracked = true, Show = StatusShowOption.WorkDirOnly })
                                       .Select(s => s.FilePath);
                                   if (oldTree != null && repo.Head?.Tip.Tree != oldTree)
                                       changedFiles = changedFiles.Concat(repo.Diff.Compare<TreeChanges>(oldTree, head?.Tip.Tree, compareOptions).Select(tc => tc.Path));
                                   var changedFilesList = changedFiles.ToList();
                                   if (!changedFilesList.Any())
                                       return null;
                                   return repo.Diff.Compare<TreeChanges>(oldTree ?? head?.Tip.Tree, dt, changedFiles, null, compareOptions);
                               });
                        return new Variant<List<TreeEntryChanges>, Unexpected>(treeChanges?.ToList() ?? new List<TreeEntryChanges>());
                    }
                    catch (Exception e)
                    {
                        return new Variant<List<TreeEntryChanges>, Unexpected>(new Unexpected(e.Message));
                    }
                });
        }
    }
}
