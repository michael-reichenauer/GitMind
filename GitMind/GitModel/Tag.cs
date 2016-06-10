namespace GitMind.GitModel
{
	internal class Tag
	{
		public string Name { get; set; }
		public string CommitId { get; set; }


		public Tag(string name, string commitId)
		{
			Name = name;
			CommitId = commitId;
		}
	}
}