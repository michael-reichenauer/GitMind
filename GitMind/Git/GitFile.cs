namespace GitMind.Git
{
	public class GitFile
	{
		public GitFile(string file, bool isModified, bool isAdded, bool isDeleted, bool isRenamed)
		{
			File = file;
			IsModified = isModified;
			IsAdded = isAdded;
			IsDeleted = isDeleted;
			IsRenamed = isRenamed;
		}

		public string File { get; }
		public bool IsModified { get; }
		public bool IsAdded { get; }
		public bool IsDeleted { get; }
		public bool IsRenamed { get; }

		public override string ToString() => File;
	}
}