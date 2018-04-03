using System.IO;
using GitMind.Git;
using GitMind.Utils.Git.Private;
using LibGit2Sharp;


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
		public string FillPath => Path.Combine(WorkfolderPath, FilePath);
		public string WorkfolderPath { get; set; }
		public GitFileStatus Status { get; }

		//public bool IsModified => Status.HasFlag(FileStatus.Modified);
		//public bool IsAdded => Status.HasFlag(FileStatus.Added);
		//public bool IsDeleted => Status.HasFlag(FileStatus.Deleted);
		//public bool IsRenamed => Status.HasFlag(FileStatus.Renamed);

		public override string ToString() => $"{FilePath} ({Status})";
	}
}