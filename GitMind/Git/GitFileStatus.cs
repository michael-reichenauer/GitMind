using System;


namespace GitMind.Git
{
	[Flags]
	public enum GitFileStatus
	{
		Modified,
		Added,
		Deleted,
		Renamed,
		Conflict,
	}
}