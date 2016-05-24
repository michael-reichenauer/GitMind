namespace GitMind.DataModel.Old
{
	internal class Merge
	{
		public Merge(Commit parentCommit, Commit childCommit, bool isMain, bool isVirtual)
		{
			ParentCommit = parentCommit;
			ChildCommit = childCommit;
			IsMain = isMain;
			IsVirtual = isVirtual;
		}


		public Commit ParentCommit { get; }
		public Commit ChildCommit { get; }
		public bool IsMain { get; }
		public bool IsVirtual { get; }
	}
}