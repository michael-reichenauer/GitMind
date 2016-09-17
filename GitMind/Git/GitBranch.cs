



namespace GitMind.Git
{
	internal class GitBranch
	{
		private readonly LibGit2Sharp.Branch branch;
		private static readonly string DetachedBranchName = "(no branch)";


		public GitBranch(LibGit2Sharp.Branch branch)
		{
			this.branch = branch;
			Name = branch.FriendlyName != DetachedBranchName
				? branch.FriendlyName
				: $"(detached_{branch.Tip.Sha.Substring(0, 6)})";
		}

		public string Name { get; }
		public string TipId => branch.Tip.Sha;
		public bool IsDetached => branch.FriendlyName == DetachedBranchName;

		public bool IsRemote => branch.IsRemote;
		public bool IsCurrent => branch.IsCurrentRepositoryHead;

		public GitCommit Tip => new GitCommit(branch.Tip);

		public override string ToString() => Name;
	}
}