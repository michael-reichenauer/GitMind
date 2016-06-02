using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;


namespace GitMind.DataModel.Old
{
	internal class BranchPriority
	{
		private readonly IComparer<string> branchNameComparer;
		private readonly IComparer<OldBranchBuilder> branchComparer;


		public BranchPriority()
		{
			branchNameComparer = new BranchNameComparer();
			branchComparer = new OldBranchComparer(branchNameComparer);
		}


		public IReadOnlyList<string> GetSortedNames(IReadOnlyList<string> branchNames)
		{
			List<string> names = branchNames.ToList();

			Sorter.Sort(names, branchNameComparer);

			return names;
		}


		public IReadOnlyList<OldBranchBuilder> GetSortedBranches(IReadOnlyList<OldBranchBuilder> unSortedbranches)
		{
			List<OldBranchBuilder> branches = unSortedbranches.ToList();

			Sorter.Sort(branches, branchComparer);

			return branches;
		}


		public int Compare(OldBranchBuilder branch1, OldBranchBuilder branch2)
		{
			return branchComparer.Compare(branch1, branch2);
		}
	}
}