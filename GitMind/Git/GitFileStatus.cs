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
		ConflictMM = 32,    // Modified by both
		ConflictMD = 64,    // Modified by us, deleted by them
		ConflictDM = 128,   // Deleted by us, modified by them
		ConflictAA = 256    // Added by both
	}
}