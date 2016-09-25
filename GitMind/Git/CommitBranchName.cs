namespace GitMind.Git
{
	internal class CommitBranchName
	{
		public CommitBranchName(string commitId, BranchName name)
		{
			CommitId = commitId;
			Name = name;
		}

		public string CommitId { get; }
		public BranchName Name { get;}

		public override string ToString() => $"{CommitId} -> {Name}";
	}
}