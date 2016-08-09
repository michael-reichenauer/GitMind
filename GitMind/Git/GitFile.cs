namespace GitMind.Git
{
	public class GitFile
	{
		public GitFile(
			string file, 
			string oldFile,
			GitConflict conflict,
			bool isModified,
			bool isAdded, 
			bool isDeleted,
			bool isRenamed,
			bool isConflict)
		{
			File = file;
			OldFile = oldFile;
			Conflict = conflict;

			IsModified = isModified;
			IsAdded = isAdded;
			IsDeleted = isDeleted;
			IsRenamed = isRenamed;
			IsConflict = isConflict;
		}

		public string File { get; }
		public string OldFile { get; }
		public GitConflict Conflict { get;  }
		public bool IsModified { get; }
		public bool IsAdded { get; }
		public bool IsDeleted { get; }
		public bool IsRenamed { get; }
		public bool IsConflict { get; }

		public override string ToString() => File;
	}
}