using GitMind.Common;


namespace GitMind.Git
{
	internal class CommitBranchName
	{
		public CommitBranchName(string id, BranchName name)
		{
			Id = id;
			Name = name;
		}

		public string Id { get; }
		public BranchName Name { get;}

		public override string ToString() => $"{Id} -> {Name}";
	}
}