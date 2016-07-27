using System.Linq;
using LibGit2Sharp;


namespace GitMind.Git
{
	public class GitStatus
	{
		public GitStatus(RepositoryStatus status, ConflictCollection conflicts)
		{
			SetStatus(status);
			SetConflicts(conflicts);
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



		private void SetStatus(RepositoryStatus status)
		{
			Modified = status.Modified.Count();
			Added = status.Added.Count() + status.Untracked.Count();
			Deleted = status.Missing.Count() + status.Removed.Count();
			Other = status.Staged.Count();

			CommitFiles = new GitCommitFiles(GitCommit.UncommittedId, status);
		}

		private void SetConflicts(ConflictCollection conflicts)
		{
			ConflictFiles = new GitCommitFiles(GitCommit.UncommittedId, conflicts);
		}
	}
}