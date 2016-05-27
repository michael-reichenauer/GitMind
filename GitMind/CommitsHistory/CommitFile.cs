namespace GitMind.CommitsHistory
{
	public class CommitFile
	{
		public string Name { get; set; }
		public string Size { get; set; }


		public CommitFile(string name, string size)
		{
			Name = name;
			Size = size;
		}
	}
}