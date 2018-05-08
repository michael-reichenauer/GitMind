using System.Collections.Generic;
using System.Linq;


namespace GitMind.Utils.Git.Private
{
	public static class GitBranchesExtensions
	{
		public static bool TryGet(this IEnumerable<GitBranch> branches, string branchName, out GitBranch branch)
		{
			branch = branches.FirstOrDefault(b => b.Name == branchName);
			return branch != null;
		}

		public static bool TryGetCurrent(this IEnumerable<GitBranch> branches, out GitBranch branch)
		{ 
			branch = branches.FirstOrDefault(b => b.IsCurrent);
			return branch != null;
		}
	}
}