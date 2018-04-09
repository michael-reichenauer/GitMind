using System.Collections.Generic;


namespace GitMind.Utils.Git
{
	public class GitStatus2
	{
		public GitStatus2(int modified, int added, int deleted, int conflicted, IReadOnlyList<GitFile2> files)
		{
			Modified = modified;
			Added = added;
			Deleted = deleted;
			Conflicted = conflicted;
			Files = files;
		}

		public int Modified { get; }
		public int Added { get; }
		public int Deleted { get; }
		public int Conflicted { get; }
		public IReadOnlyList<GitFile2> Files { get; }
		public int AllChanges => Modified + Added + Deleted + Conflicted;

		public bool OK => AllChanges == 0;

		public override string ToString() =>
			OK ? "Ok" : $"{AllChanges} ({Modified}M, {Added}A, {Deleted}D {Conflicted}C)";
	}
}