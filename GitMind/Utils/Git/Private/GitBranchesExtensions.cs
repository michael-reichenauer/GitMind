using System.Collections.Generic;
using System.Linq;


namespace GitMind.Utils.Git.Private
{
	public static class GitBranchesExtensions
	{
		public static bool TryGet(this IEnumerable<GitBranch2> branches, string branchName, out GitBranch2 branch)
		{
			branch = branches.FirstOrDefault(b => b.Name == branchName);
			return branch != null;
		}

		public static bool TryGetCurrent(this IEnumerable<GitBranch2> branches, out GitBranch2 branch)
		{ 
			branch = branches.FirstOrDefault(b => b.IsCurrent);
			return branch != null;
		}
	}
}