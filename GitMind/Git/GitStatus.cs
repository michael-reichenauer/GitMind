using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;


namespace GitMind.Git
{
	public class GitStatus
	{
		public GitStatus(RepositoryStatus status, ConflictCollection conflicts)
		{
			SetStatus(status, conflicts);
		}


		public int Modified { get; private set; }
		public int Added { get; private set; }
		public int Deleted { get; private set; }
		public int Other { get; private set; }
		public int Count => Added + Deleted + Modified + Other;

		public GitCommitFiles CommitFiles { get; private set; }

		public bool OK => Count == 0;

		public int ConflictCount => ConflictFiles.Files.Count;
		public GitCommitFiles ConflictFiles { get; private set; }



		private void SetStatus(RepositoryStatus status, ConflictCollection conflicts)
		{
			Modified = status.Modified.Count();
			Added = status.Added.Count() + GetUntrackedCount(status, conflicts);
			Deleted = status.Missing.Count() + status.Removed.Count();
			Other = status.Staged.Count();

			CommitFiles = new GitCommitFiles(GitCommit.UncommittedId, status);

			ConflictFiles = new GitCommitFiles(GitCommit.UncommittedId, conflicts);
		}

		
		private static int GetUntrackedCount(RepositoryStatus status, ConflictCollection conflicts)
		{
			// When there are conflicts, tools create temp files like these, lets filter them. 
			IEnumerable<string> conflictFiles = conflicts.Select(c => c.Ours.Path + ".LOCAL.")
				.Concat(conflicts.Select(c => c.Ancestor.Path + ".BASE."))
				.Concat(conflicts.Select(c => c.Theirs.Path + ".REMOTE."))
				.ToList();

			int untrackedCount = 0;

			foreach (StatusEntry statusEntry in status.Untracked)
			{
				if (!conflictFiles.Any(f => statusEntry.FilePath.StartsWith(f)))
				{
					untrackedCount++;
				}
			}

			return untrackedCount;
		}
	}
}