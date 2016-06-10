namespace GitMind.GitModel
{
	public class CommitFile
	{
		public string Name { get; }
		public string Status { get; }

		public CommitFile(string name, string status)
		{
			Name = name;
			Status = status;
		}
	}
}