using System;


namespace GitMind.Git
{
	[Flags]
	public enum GitFileStatus
	{
		Modified = 1,
		Added = 2,
		Deleted = 4,
		Renamed = 8,
		Conflict = 16,
	}
}