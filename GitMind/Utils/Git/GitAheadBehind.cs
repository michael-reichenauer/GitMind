namespace GitMind.Utils.Git
{
	public class GitAheadBehind
	{
		public string Branch { get; }
		public int Ahead { get; }
		public int Behind { get; }

		public GitAheadBehind(string branch, int ahead, int behind)
		{
			Branch = branch;
			Ahead = ahead;
			Behind = behind;
		}

		public override string ToString() => $"{Branch} ({Ahead}A, {Behind}B)";
	}
}