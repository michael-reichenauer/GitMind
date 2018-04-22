namespace GitMind.Git
{
	public class GitTag
	{
		//public GitTag(Tag tag)
		//{
		//	CommitId = tag.Target.Sha;
		//	TagName = tag.FriendlyName;
		//}

		public GitTag(string sha, string name)
		{
			CommitId = sha;
			TagName = name;
		}

		public string CommitId { get; }

		public string TagName { get; }
	}
}