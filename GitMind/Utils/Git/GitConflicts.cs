using System.Collections.Generic;
using System.Linq;


namespace GitMind.Utils.Git
{
	public class GitConflicts
	{
		public static readonly GitConflicts None = new GitConflicts(new GitConflictFile[0]);

		public GitConflicts(IReadOnlyList<GitConflictFile> files)
		{
			Files = files;
		}

		public IReadOnlyList<GitConflictFile> Files { get; }

		public bool HasConflicts => Files.Any();
		public int Count => Files.Count;

		public override string ToString() => !HasConflicts ? "Ok" : $"{Files.Count} Conflicts";
	}
}