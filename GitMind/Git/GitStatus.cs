


using System.Linq;


namespace GitMind.Git
{
	internal class GitStatus
	{
		public GitStatus(LibGit2Sharp.RepositoryStatus status)
		{
			Modified = status.Modified.Count();
			Added = status.Added.Count() + status.Untracked.Count();
			Deleted = status.Missing.Count() + status.Removed.Count();
			Other = status.Staged.Count();
		}

		public int Modified { get; }
		public int Added { get; }
		public int Deleted { get; }
		public int Other { get; }
		public int Count => Added + Deleted + Modified + Other;


		public bool OK => Count == 0;
	}
}