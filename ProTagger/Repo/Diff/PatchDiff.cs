using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProTagger.Repo.Diff
{
    static class PatchDiff
    {
        internal static Variant<Patch, string> CreateDiff(
                IRepositoryWrapper repository,
                Commit? oldCommit,
                Commit newCommit,
                IEnumerable<string> paths)
        {
            try
            {
                return new Variant<Patch, string>(repository.Diff.Compare<Patch>(oldCommit?.Tree ?? newCommit.Parents.FirstOrDefault()?.Tree, newCommit.Tree, paths));
            }
            catch (Exception e)
            {
                return new Variant<Patch, string>(e.Message);
            }
        }
    }
}
