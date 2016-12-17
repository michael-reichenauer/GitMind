using GitMind.Common;


namespace GitMind.Git
{
	internal class CommitBranchName
	{
		public CommitBranchName(CommitId commitId, BranchName name)
		{
			CommitId = commitId;
			Name = name;
		}

		public CommitId CommitId { get; }
		public BranchName Name { get;}

		public override string ToString() => $"{CommitId} -> {Name}";
	}
}