namespace GitMind.CommitsHistory
{
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