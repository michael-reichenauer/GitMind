namespace GitMind.GitModel
{
	internal class Status
	{
		public Status(int statusCount, int conflictCount)
		{
			StatusCount = statusCount;
			ConflictCount = conflictCount;
		}


		public int StatusCount { get; }
		public int ConflictCount { get; }
	}
}