using System.Collections.Generic;
using System.Linq;
using GitMind.Git.Private;
using LibGit2Sharp;


namespace GitMind.Git
{
	internal class GitCommitFiles
	{
		public GitCommitFiles(string commitId, TreeChanges treeChanges)
		{
			Id = commitId;
			if (treeChanges == null)
			{
				Files = new GitFile[0];
			}
			else
			{
				Files = treeChanges
					.Added.Select(t => new GitFile(t.Path, false, true, false, false))
					.Concat(treeChanges.Deleted.Select(t => new GitFile(t.Path, false, false, true, false)))
					.Concat(treeChanges.Modified.Select(t => new GitFile(t.Path, true, false, false, false)))
					.Concat(treeChanges.Renamed.Select(t => new GitFile(t.Path, false, false, false, true)))
					.ToList();


			}
		}

		public string Id { get; set; }
		public IReadOnlyList<GitFile> Files { get; set; }
	}
}