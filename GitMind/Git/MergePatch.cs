namespace GitMind.Git
{
	internal class MergePatch
	{
		public string Patch { get; }

		public string ConflictPatch { get; }

		public MergePatch(string patch, string conflictPatch)
		{
			Patch = patch;
			ConflictPatch = conflictPatch;
		}
	}
}