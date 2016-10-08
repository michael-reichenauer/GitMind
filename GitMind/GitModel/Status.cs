namespace GitMind.GitModel
{
	internal class Status
	{
		public Status(int statusCount, int conflictCount, string message, bool isMerging)
		{
			StatusCount = statusCount;
			ConflictCount = conflictCount;
			Message = message;
			IsMerging = isMerging;
		}


		public bool isOk => StatusCount == 0 && ConflictCount == 0 && !IsMerging;

		public int StatusCount { get; }
		public int ConflictCount { get; }
		public string Message { get; }
		public bool IsMerging { get; }
	}
}