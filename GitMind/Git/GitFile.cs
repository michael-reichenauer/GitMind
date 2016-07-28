namespace GitMind.Git
{
	public class GitFile
	{
		public GitFile(
			string file, 
			string oldFile,
			bool isModified,
			bool isAdded, 
			bool isDeleted,
			bool isRenamed)
		{
			File = file;
			OldFile = oldFile;
			IsModified = isModified;
			IsAdded = isAdded;
			IsDeleted = isDeleted;
			IsRenamed = isRenamed;
		}

		public string File { get; }
		public string OldFile { get; }
		public bool IsModified { get; }
		public bool IsAdded { get; }
		public bool IsDeleted { get; }
		public bool IsRenamed { get; }

		public override string ToString() => File;
	}
}