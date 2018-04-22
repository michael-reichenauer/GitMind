using System.IO;
using GitMind.Git;


namespace GitMind.Utils.Git
{
	public class GitFile2
	{
		public GitFile2(
			string workfolderPath,
			string filePath,
			string oldFilePath,
			GitFileStatus status)
		{
			FilePath = filePath;
			OldFilePath = oldFilePath;
			WorkfolderPath = workfolderPath;
			Status = status;
		}

		public string FilePath { get; }
		public string OldFilePath { get; }
		public string FullFilePath => Path.Combine(WorkfolderPath, FilePath);
		public string WorkfolderPath { get; }
		public GitFileStatus Status { get; }

		public override string ToString() => $"{FilePath} ({Status})";
	}
}