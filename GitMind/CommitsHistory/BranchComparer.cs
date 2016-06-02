using System.Collections.Generic;
using System.Linq;
using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal class BranchComparer : IComparer<Branch>
	{
		public int Compare(Branch x, Branch y)
		{
			if (y.HasParentBranch && y.ParentBranch == x)
			{
				return -1;
			}
			else if (x.HasParentBranch && x.ParentBranch == y)
			{
				return 1;
			}

			return 0;
		}
	}
}