namespace GitMind.Git
{
	public class GitTag
	{
		public GitTag(string sha, string name)
		{
			CommitId = sha;
			TagName = name;
		}

		public string CommitId { get; }

		public string TagName { get; }

		public override string ToString() => $"{CommitId} {TagName}";
	}
}