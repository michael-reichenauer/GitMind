using System.Collections.Generic;


namespace GitMind.Utils.Git
{
	public class GitStatus
	{
		public GitStatus(
			int modified,
			int added,
			int deleted,
			int conflicted,
			bool isMerging,
			string mergeMessage,
			IReadOnlyList<GitFile> files)
		{
			Modified = modified;
			Added = added;
			Deleted = deleted;
			Conflicted = conflicted;
			IsMerging = isMerging;
			MergeMessage = mergeMessage;
			Files = files;
		}


		public static GitStatus Default { get; } = new GitStatus(0, 0, 0, 0, false, null, new GitFile[0]);


		public int Modified { get; }
		public int Added { get; }
		public int Deleted { get; }
		public int Conflicted { get; }
		public bool IsMerging { get; }
		public bool HasConflicts => Conflicted > 0;
		public string MergeMessage { get; }
		public IReadOnlyList<GitFile> Files { get; }
		public int AllChanges => Modified + Added + Deleted + Conflicted;

		public bool OK => AllChanges == 0 && !IsMerging;

		public bool IsSame(GitStatus status) =>
			status.AllChanges == AllChanges &&
			status.Modified == Modified &&
			status.Deleted == Deleted &&
			status.Added == Added &&
			status.Conflicted == Conflicted &&
			status.MergeMessage == MergeMessage &&
			status.IsMerging == IsMerging;


		public override string ToString() =>
			OK ? "Ok" : $"{AllChanges} ({Modified}M, {Added}A, {Deleted}D {Conflicted}C{MergeText})";


		private string MergeText => IsMerging ? " |\\" : "";
	}
}