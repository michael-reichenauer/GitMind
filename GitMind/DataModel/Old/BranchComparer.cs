using System.Collections.Generic;


namespace GitMind.DataModel.Old
{
	internal class BranchComparer : IComparer<OldBranchBuilder>
	{
		private readonly IComparer<string> nameComparer;


		public BranchComparer(IComparer<string> nameComparer)
		{
			this.nameComparer = nameComparer;
		}


		public int Compare(OldBranchBuilder x, OldBranchBuilder y)
		{
			int result;
			if (TryCompareMaster(x, y, out result))
			{
				return result;
			}
			else if (TryCompareMultiBranch(x, y, out result))
			{
				return result;
			}
			else if (TryCompareParent(x, y, out result))
			{
				return result;
			}

			return nameComparer.Compare(x.Name, y.Name);
		}


		private bool TryCompareMaster(OldBranchBuilder x, OldBranchBuilder y, out int result)
		{
			if (x.Name == "master")
			{
				result = -1;
				return true;
			}
			else if (y.Name == "master")
			{
				result = 1;
				return true;
			}

			result = 0;
			return false;
		}


		private bool TryCompareParent(OldBranchBuilder x, OldBranchBuilder y, out int result)
		{
			if (x == y.Parent)
			{
				result = -1;
				return true;
			}
			else if (y == x.Parent)
			{
				result = 1;
				return true;
			}

			result = 0;
			return false;
		}


		private bool TryCompareMultiBranch(OldBranchBuilder x, OldBranchBuilder y, out int result)
		{
			if (x.IsMultiBranch && x.MultiBranches.Contains(y))
			{
				result = -1;
				return true;
			}
			else if (y.IsMultiBranch && y.MultiBranches.Contains(x))
			{
				result = 1;
				return true;
			}

			result = 0;
			return false;
		}
	}
}