namespace GitMind.Git
{
	internal class GitFile
	{
		public GitFile(string file, bool isModified, bool isAdded, bool isDeleted)
		{
			File = file;
			IsModified = isModified;
			IsAdded = isAdded;
			IsDeleted = isDeleted;
		}

		public string File { get; }
		public bool IsModified { get; }
		public bool IsAdded { get; }
		public bool IsDeleted { get; }

		public override string ToString() => File;
	}
}