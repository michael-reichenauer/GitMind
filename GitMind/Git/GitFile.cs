namespace GitMind.Git
{
	public class GitFile
	{
		public GitFile(
			string file, 
			string oldFile,
			GitConflict conflict,
			GitFileStatus status)
		{

			File = file;
			OldFile = oldFile;
			Conflict = conflict;
			Status = status;
		}

		public string File { get; }
		public string OldFile { get; }
		public GitConflict Conflict { get;  }
		public GitFileStatus Status { get; }

		public bool IsModified => Status.HasFlag(GitFileStatus.Modified);
		public bool IsAdded => Status.HasFlag(GitFileStatus.Added);
		public bool IsDeleted => Status.HasFlag(GitFileStatus.Deleted);
		public bool IsRenamed => Status.HasFlag(GitFileStatus.Renamed);
		public bool IsConflict => Status.HasFlag(GitFileStatus.Conflict);

		public override string ToString() => File;
	}
}