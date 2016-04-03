using System.Collections.Generic;


namespace GitMind.DataModel.Private
{
	internal class BranchComparer : IComparer<BranchBuilder>
	{
		private readonly IComparer<string> nameComparer;


		public BranchComparer(IComparer<string> nameComparer)
		{
			this.nameComparer = nameComparer;
		}


		public int Compare(BranchBuilder x, BranchBuilder y)
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


		private bool TryCompareMaster(BranchBuilder x, BranchBuilder y, out int result)
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


		private bool TryCompareParent(BranchBuilder x, BranchBuilder y, out int result)
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


		private bool TryCompareMultiBranch(BranchBuilder x, BranchBuilder y, out int result)
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