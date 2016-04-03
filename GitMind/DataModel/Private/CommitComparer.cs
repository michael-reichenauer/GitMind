using System.Collections.Generic;


namespace GitMind.DataModel.Private
{
	internal class CommitComparer : IComparer<Commit>
	{
		public int Compare(Commit x, Commit y)
		{
			return x.CommitDateTime.CompareTo(y.CommitDateTime) * -1;
		}
	}
}