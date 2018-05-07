using System.IO;


namespace GitMind.Utils.Git
{
	public class GitConflictFile
	{
		public GitConflictFile(
			string workfolderPath,
			string filePath,
			string baseId,
			string localId,
			string remoteId,
			GitFileStatus status)
		{
			FilePath = filePath;
			BaseId = baseId;
			LocalId = localId;
			RemoteId = remoteId;
			WorkfolderPath = workfolderPath;
			Status = status;
		}

		public string FilePath { get; }
		public string BaseId { get; }
		public string LocalId { get; }
		public string RemoteId { get; }
		public string FullFilePath => Path.Combine(WorkfolderPath, FilePath);
		public string WorkfolderPath { get; set; }
		public GitFileStatus Status { get; }


		public override string ToString() => $"{FilePath} ({Status})";
	}
}