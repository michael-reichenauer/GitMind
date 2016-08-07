namespace GitMind.Git
{
	internal class BranchName
	{
		public BranchName(string commitId, string name)
		{
			CommitId = commitId;
			Name = name;
		}

		public string CommitId { get; }
		public string Name { get;}

		public override string ToString() => $"{CommitId} -> {Name}";
	}
}