using System.Collections.Generic;


namespace GitMind.DataModel.Old
{
	internal class CommitComparer : IComparer<OldCommit>
	{
		public int Compare(OldCommit x, OldCommit y)
		{
			return x.CommitDateTime.CompareTo(y.CommitDateTime) * -1;
		}
	}
}