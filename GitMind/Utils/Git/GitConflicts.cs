using System.Collections.Generic;
using System.Linq;


namespace GitMind.Utils.Git
{
	public class GitConflicts
	{
		public GitConflicts(IReadOnlyList<GitConflictFile> files)
		{
			Files = files;
		}

		public IReadOnlyList<GitConflictFile> Files { get; }

		public bool OK => !Files.Any();
		public int Count => Files.Count;

		public override string ToString() => OK ? "Ok" : $"{Files.Count} Conflicts";
	}
}