using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;


namespace GitMind.DataModel.Old
{
	internal class BranchPriority
	{
		private readonly IComparer<string> branchNameComparer;
		private readonly IComparer<BranchBuilder> branchComparer;


		public BranchPriority()
		{
			branchNameComparer = new BranchNameComparer();
			branchComparer = new BranchComparer(branchNameComparer);
		}


		public IReadOnlyList<string> GetSortedNames(IReadOnlyList<string> branchNames)
		{
			List<string> names = branchNames.ToList();

			Sorter.Sort(names, branchNameComparer);

			return names;
		}


		public IReadOnlyList<BranchBuilder> GetSortedBranches(IReadOnlyList<BranchBuilder> unSortedbranches)
		{
			List<BranchBuilder> branches = unSortedbranches.ToList();

			Sorter.Sort(branches, branchComparer);

			return branches;
		}


		public int Compare(BranchBuilder branch1, BranchBuilder branch2)
		{
			return branchComparer.Compare(branch1, branch2);
		}
	}
}