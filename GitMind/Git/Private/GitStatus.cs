namespace GitMind.Git.Private
{
	internal class GitStatus
	{
		public GitStatus(int modified, int added, int deleted, int other)
		{
			Modified = modified;
			Added = added;
			Deleted = deleted;
			Other = other;
		}

		public int Modified { get; }
		public int Added { get; }
		public int Deleted { get; }
		public int Other { get; }

		public bool OK => Modified == 0 && Added == 0 && Deleted == 0 && Other == 0;
	}
}