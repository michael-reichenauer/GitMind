using System.IO;


namespace GitMind.Utils.Git
{
	public class GitFile
	{
		public GitFile(
			string workFolderPath,
			string filePath,
			string oldFilePath,
			GitFileStatus status)
		{
			FilePath = filePath;
			OldFilePath = oldFilePath;
			WorkFolderPath = workFolderPath;
			Status = status;
		}

		public string FilePath { get; }
		public string OldFilePath { get; }
		public string FullFilePath => Path.Combine(WorkFolderPath, FilePath);
		public string WorkFolderPath { get; }
		public GitFileStatus Status { get; }

		public override string ToString() => $"{FilePath} ({Status})";
	}
}