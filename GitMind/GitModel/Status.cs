namespace GitMind.GitModel
{
	internal class Status
	{
		public Status(int conflictCount)
		{
			ConflictCount = conflictCount;
		}


		public int ConflictCount { get; }
	}
}