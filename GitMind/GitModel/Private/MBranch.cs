using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ProtoBuf;


namespace GitMind.GitModel.Private
{
	[DataContract, ProtoContract]
	public class MBranch
	{
		[DataMember, ProtoMember(1)]
		public string Id { get; set; }
		[DataMember, ProtoMember(2)]
		public string Name { get; set; }
		[DataMember, ProtoMember(3)]
		public string LatestCommitId { get; set; }
		[DataMember, ProtoMember(4)]
		public string FirstCommitId { get; set; }
		[DataMember, ProtoMember(5)]
		public string ParentCommitId { get; set; }
		[DataMember, ProtoMember(6)]
		public string ParentBranchId { get; set; }
		[DataMember, ProtoMember(7)]
		public bool IsMultiBranch { get; set; }
		[DataMember, ProtoMember(8)]
		public bool IsActive { get; set; }
		[DataMember, ProtoMember(9)]
		public bool IsAnonymous { get; set; }
		[DataMember, ProtoMember(10)]
		public int LocalAheadCount { get; set; }
		[DataMember, ProtoMember(11)]
		public int RemoteAheadCount { get; set; }
		[DataMember, ProtoMember(12)]
		public bool IsLocalAndRemote { get; set; }

		[DataMember, ProtoMember(13)]
		public List<string> SubBrancheIds { get; set; } = new List<string>();
		[DataMember, ProtoMember(14)]
		public List<string> CommitIds { get; set; } = new List<string>();
		[DataMember, ProtoMember(15)]
		public List<string> ChildBrancheIds { get; set; } = new List<string>();

		public MRepository Repository { get; set; }

		public IEnumerable<MSubBranch> SubBranches => 
			SubBrancheIds.Select(id => Repository.SubBranches[id]);
		public IEnumerable<MCommit> Commits =>
			CommitIds.Select(id => Repository.Commits[id]);
		public IEnumerable<MBranch> ChildBranches =>
			ChildBrancheIds.Select(id => Repository.Branches[id]);

		public MCommit FirstCommit => Repository.Commits[FirstCommitId];
		public MCommit LatestCommit => Repository.Commits[LatestCommitId];
		public MCommit ParentCommit => Repository.Commits[ParentCommitId];
		public MBranch ParentBranch => Repository.Branches[ParentBranchId];

		public override string ToString() => $"{Name}";
	}
}