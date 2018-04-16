namespace GitMind.Git
{
	public class CommitDiff
	{
		public CommitDiff(string leftPath, string rightPath)
		{
			LeftPath = leftPath;
			RightPath = rightPath;
		}

		public string LeftPath { get; }

		public string RightPath { get; }
	}
}