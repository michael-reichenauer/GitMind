namespace GitMind
{
	public class BranchName
	{
		public string Text { get; set; }


		public BranchName(string text)
		{
			Text = text;
		}
	}

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

	public class CommitDetail
	{
		public string Header { get; set; }
		public string Text { get; set; }


		public CommitDetail(string header, string text)
		{
			Header = header;
			Text = text;
		}
	}
}