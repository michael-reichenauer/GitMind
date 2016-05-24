namespace GitMind.DataModel.Old
{
	internal class ActiveBranch
	{
		public ActiveBranch(string name, string commitId)
		{
			Name = name;
			CommitId = commitId;
		}


		public string Name { get; }

		public string CommitId { get; }

		public override string ToString() => $"{Name},{CommitId}";
	}
}