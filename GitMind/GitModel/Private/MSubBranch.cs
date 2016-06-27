using System.Runtime.Serialization;
using ProtoBuf;


namespace GitMind.GitModel.Private
{
	[DataContract, ProtoContract]
	public class MSubBranch
	{
		[DataMember, ProtoMember(1)]
		public string SubBranchId { get; set; }
		[DataMember, ProtoMember(2)]
		public string BranchId { get; set; }
		[DataMember, ProtoMember(3)]
		public string Name { get; set; }
		[DataMember, ProtoMember(4)]
		public string LatestCommitId { get; set; }
		[DataMember, ProtoMember(5)]
		public string FirstCommitId { get; set; }
		[DataMember, ProtoMember(6)]
		public string ParentCommitId { get; set; }
		[DataMember, ProtoMember(7)]
		public bool IsMultiBranch { get; set; }
		[DataMember, ProtoMember(8)]
		public bool IsActive { get; set; }
		[DataMember, ProtoMember(9)]
		public bool IsAnonymous { get; set; }
		[DataMember, ProtoMember(10)]
		public bool IsRemote { get; set; }


		public MRepository Repository { get; set; }
		public MCommit FirstCommit => Repository.Commits[FirstCommitId];
		public MCommit LatestCommit => Repository.Commits[LatestCommitId];
		public MCommit ParentCommit => Repository.Commits[ParentCommitId];

		public override string ToString() => $"{Name} ({IsRemote})";
	}
}