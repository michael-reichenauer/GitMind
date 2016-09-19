using System;


namespace GitMind.Git
{
	internal class GitBranch
	{
		private static readonly string DetachedBranchName = "(no branch)";
		private readonly LibGit2Sharp.Repository repository;
		private readonly LibGit2Sharp.Branch branch;


		public GitBranch(LibGit2Sharp.Branch branch, LibGit2Sharp.Repository repository)
		{
			this.repository = repository;
			this.branch = branch;
			Name = BranchName.From(branch.FriendlyName != DetachedBranchName
				? branch.FriendlyName
				: $"({branch.Tip.Sha.Substring(0, 6)})");
		}

		public BranchName Name { get; }
		public string TipId => branch.Tip.Sha;
		public bool IsDetached => branch.FriendlyName == DetachedBranchName;

		public bool IsRemote => branch.IsRemote;
		public bool IsCurrent => 0 ==  string.Compare(
			branch.CanonicalName, repository.Head.CanonicalName, StringComparison.OrdinalIgnoreCase) ;

		public GitCommit Tip => new GitCommit(branch.Tip);

		public override string ToString() => Name.ToString();
	}
}