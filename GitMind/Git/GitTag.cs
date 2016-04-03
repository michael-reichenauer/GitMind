namespace GitMind.Git
{
	internal class GitTag
	{
		public GitTag(string commitId, string tagName)
		{
			CommitId = commitId;
			TagName = tagName;
		}

		public string CommitId { get; }

		public string TagName { get; }
	}
}