using System.IO;
using GitMind.Git;


namespace GitMind.Features.StatusHandling
{
	public class StatusFile
	{
		public StatusFile(
			string workfolderPath,
			string filePath,
			string oldFilePath,
			GitConflict conflict,
			GitFileStatus status)
		{
			FilePath = filePath;
			OldFilePath = oldFilePath;
			WorkfolderPath = workfolderPath;
			Conflict = conflict;
			Status = status;
		}

		public string FilePath { get; }
		public string OldFilePath { get; }
		public string FillPath => Path.Combine(WorkfolderPath, FilePath);
		public string WorkfolderPath { get; set; }
		public GitConflict Conflict { get; }
		public GitFileStatus Status { get; }

		public bool IsModified => Status.HasFlag(GitFileStatus.Modified);
		public bool IsAdded => Status.HasFlag(GitFileStatus.Added);
		public bool IsDeleted => Status.HasFlag(GitFileStatus.Deleted);
		public bool IsRenamed => Status.HasFlag(GitFileStatus.Renamed);
		public bool IsConflict => Status.HasFlag(GitFileStatus.Conflict);

		public override string ToString() => FilePath;
	}
}