using ProtoBuf;


namespace GitMind.GitModel.Private
{
	[ProtoContract]
	public class MSubBranch
	{
		[ProtoMember(1)]
		public string SubBranchId { get; set; }
		[ProtoMember(2)]
		public string BranchId { get; set; }
		[ProtoMember(3)]
		public string Name { get; set; }
		[ProtoMember(4)]
		public string LatestCommitId { get; set; }
		[ProtoMember(5)]
		public string FirstCommitId { get; set; }
		[ProtoMember(6)]
		public string ParentCommitId { get; set; }
		[ProtoMember(7)]
		public bool IsMultiBranch { get; set; }
		[ProtoMember(8)]
		public bool IsActive { get; set; }
		[ProtoMember(9)]
		public bool IsAnonymous { get; set; }
		[ProtoMember(10)]
		public bool IsRemote { get; set; }

		public MRepository Repository { get; set; }

		public MCommit LatestCommit => Repository.Commits[LatestCommitId];
		public MCommit ParentCommit => Repository.Commits[ParentCommitId];
		public bool IsLocal => !IsRemote;

		public override string ToString() => $"{Name} ({IsRemote})";
	}
}