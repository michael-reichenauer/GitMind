using System.IO;


namespace GitMind.Utils.Git.Private
{
	public class CommitFile
	{
		public CommitFile(
			string workfolderPath,
			string filePath,
			string oldFilePath,
			FileStatus status)
		{
			FilePath = filePath;
			OldFilePath = oldFilePath;
			WorkfolderPath = workfolderPath;
			Status = status;
		}

		public string FilePath { get; }
		public string OldFilePath { get; }
		public string FillPath => Path.Combine(WorkfolderPath, FilePath);
		public string WorkfolderPath { get; set; }
		public FileStatus Status { get; }

		public bool IsModified => Status.HasFlag(FileStatus.Modified);
		public bool IsAdded => Status.HasFlag(FileStatus.Added);
		public bool IsDeleted => Status.HasFlag(FileStatus.Deleted);
		public bool IsRenamed => Status.HasFlag(FileStatus.Renamed);

		public override string ToString() => $"{FilePath} ({Status})";
	}
}