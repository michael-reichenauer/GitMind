using LibGit2Sharp;


namespace GitMind.Git
{
	internal class GitTag
	{
		public GitTag(Tag tag)
		{
			CommitId = tag.Target.Sha;
			TagName = tag.FriendlyName;
		}

		public string CommitId { get; }

		public string TagName { get; }
	}
}