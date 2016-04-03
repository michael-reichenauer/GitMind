using System.Collections.Generic;
using System.Linq;


namespace GitMind.DataModel.Private
{
	internal class BranchNameComparer : IComparer<string>
	{
		public int Compare(string x, string y)
		{
			int result;
			if (TryCompareMaster(x, y, out result))
			{
				return result;
			}
			else if (TryCompareWithUsingRules(x, y, out result))
			{
				return result;
			}


			return 0;
		}


		private static bool TryCompareMaster(string x, string y, out int result)
		{
			if (x == "master")
			{
				result = -1;
				return true;
			}
			else if (y == "master")
			{
				result = 1;
				return true;
			}

			result = 0;
			return false;
		}


		private bool TryCompareWithUsingRules(string x, string y, out int result)
		{
			// Splitting x and y on "/" 
			string[] xParts = x.Split("/".ToCharArray());
			string[] yParts = y.Split("/".ToCharArray());

			if (xParts.Length > 2 && yParts.Length > 2)
			{
				string newX = string.Join("/", xParts.Skip(2));
				string newY = string.Join("/", yParts.Skip(2));
				return TryCompareWithUsingSubRules(newX, newY, out result);
			}

			if (xParts.Length > 1 && yParts.Length > 1)
			{
				string newX = string.Join("/", xParts.Skip(1));
				string newY = string.Join("/", yParts.Skip(1));
				return TryCompareWithUsingSubRules(newX, newY, out result);
			}

			result = 0;
			return false;
		}


		private bool TryCompareWithUsingSubRules(string x, string y, out int result)
		{
			// Splitting x and y on either "/" or "_" as if they where paths parts
			string[] xParts = x.Split("/_".ToCharArray());
			string[] yParts = y.Split("/_".ToCharArray());

			for (int i = 0; i < xParts.Length; i++)
			{
				string xp = xParts[i];
				string yp = yParts.Length > i ? yParts[i] : null;  // yp == null if y shorter than x

				if (yp == null)
				{
					// y had fewer parts than x and considered less
					result = 1;
					return true;
				}
				else if (xp != yp)
				{
					// Some part differed, they are on the same level (e.g. sibling branches)
					result = 0;
					return true;
				}
			}

			// If length differ, then x must be shorter since a shorter y would have returned before
			result = xParts.Length == yParts.Length ? 0 : -1;
			return true;
		}
	}
}