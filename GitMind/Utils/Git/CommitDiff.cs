using System.IO;


namespace GitMind.Utils.Git
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

		public string LeftText => File.Exists(LeftPath) ? File.ReadAllText(LeftPath) : "";
		public string RightText => File.Exists(RightPath) ? File.ReadAllText(RightPath) : "";
	}
}