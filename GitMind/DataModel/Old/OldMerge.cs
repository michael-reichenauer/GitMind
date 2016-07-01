namespace GitMind.DataModel.Old
{
	internal class OldMerge
	{
		public OldMerge(OldCommit parentCommit, OldCommit childCommit, bool isMain, bool isVirtual)
		{
			ParentCommit = parentCommit;
			ChildCommit = childCommit;
			IsMain = isMain;
			IsVirtual = isVirtual;
		}


		public OldCommit ParentCommit { get; }
		public OldCommit ChildCommit { get; }
		public bool IsMain { get; }
		public bool IsVirtual { get; }
	}
}