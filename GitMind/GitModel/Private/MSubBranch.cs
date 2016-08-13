namespace GitMind.GitModel.Private
{
	public class MSubBranch
	{
		public string SubBranchId { get; set; }
		public string BranchId { get; set; }
		public string Name { get; set; }
		public string TipCommitId { get; set; }
		public string FirstCommitId { get; set; }
		public string ParentCommitId { get; set; }
		public bool IsMultiBranch { get; set; }
		public bool IsActive { get; set; }
		public bool IsAnonymous { get; set; }
		public bool IsRemote { get; set; }

		public MRepository Repository { get; set; }

		public MCommit TipCommit => Repository.Commits[TipCommitId];
		public MCommit ParentCommit => Repository.Commits[ParentCommitId];
		public bool IsLocal => !IsRemote;

		public override string ToString() => $"{Name} ({IsRemote})";
	}
}