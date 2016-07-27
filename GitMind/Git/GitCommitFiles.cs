using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;


namespace GitMind.Git
{
	public class GitCommitFiles
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

		public GitCommitFiles(string commitId, RepositoryStatus status)
		{
			Id = commitId;
			if (status == null)
			{
				Files = new GitFile[0];
			}
			else
			{
				Files = status
					.Added.Select(t => new GitFile(t.FilePath, false, true, false, false))
					.Concat(status.Untracked.Select(t => new GitFile(t.FilePath, false, true, false, false)))
					.Concat(status.Removed.Select(t => new GitFile(t.FilePath, false, false, true, false)))
					.Concat(status.Modified.Select(t => new GitFile(t.FilePath, true, false, false, false)))
					.Concat(status.RenamedInWorkDir.Select(t => new GitFile(t.FilePath, false, false, false, true)))
					.Concat(status.RenamedInIndex.Select(t => new GitFile(t.FilePath, false, false, false, true)))
					.ToList();
			}
		}


		public GitCommitFiles(string commitId, ConflictCollection conflicts)
		{
			Id = commitId;

			Files = conflicts
				.Select(c => new GitFile(c.Ours.Path, true, false, false, false))
				.ToList();
		}


		public string Id { get; set; }
		public IReadOnlyList<GitFile> Files { get; set; }
	}
}