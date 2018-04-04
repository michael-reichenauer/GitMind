using System.Collections.Generic;


namespace GitMind.Utils.Git
{
	public class Status2
	{
		public Status2(int modified, int added, int deleted, IReadOnlyList<GitFile2> files)
		{
			Modified = modified;
			Added = added;
			Deleted = deleted;
			Files = files;
		}

		public int Modified { get; }
		public int Added { get; }
		public int Deleted { get; }
		public IReadOnlyList<GitFile2> Files { get; }
		public int AllChanges => Modified + Added + Deleted;

		public bool OK => AllChanges == 0;

		public override string ToString() => OK ? "Ok" : $"{AllChanges} ({Modified}M, {Added}A, {Deleted}D)";
	}
}