



namespace GitMind.Git
{
	internal class GitBranch
	{
		private readonly LibGit2Sharp.Branch branch;


		public GitBranch(LibGit2Sharp.Branch branch)
		{
			this.branch = branch;
		}


		public string Name => branch.FriendlyName;
		public string TipId => branch.Tip.Sha;

		public bool IsRemote => branch.IsRemote;
		public bool IsCurrent => branch.IsCurrentRepositoryHead;

		public GitCommit Tip => new GitCommit(branch.Tip);

		public override string ToString() => Name;
	}
}