using System;
using System.Linq;
using GitMind.Common;
using GitMind.GitModel.Private;


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
			Name = branch.FriendlyName != DetachedBranchName
				? branch.FriendlyName
				: $"({branch.Tip.Sha.Substring(0, 6)})";
		}

		public BranchName Name { get; }
		public string TipId => branch.Tip.Sha;
		public bool IsDetached => branch.FriendlyName == DetachedBranchName;

		public bool IsRemote => branch.IsRemote;
		public bool IsCurrent => 0 ==  Txt.CompareOic(branch.CanonicalName, repository.Head.CanonicalName);
		public bool HasCommits => branch.Commits.Any();

		public GitLibCommit Tip => new GitLibCommit(
			new CommitSha(branch.Tip.Sha),
			branch.Tip.MessageShort,
			branch.Tip.Author.Name,
			branch.Tip.Author.When.LocalDateTime,
			branch.Tip.Committer.When.LocalDateTime,
			branch.Tip.Parents.Select(p =>new CommitSha(p.Sha)).ToList());

		public override string ToString() => Name.ToString();
	}
}