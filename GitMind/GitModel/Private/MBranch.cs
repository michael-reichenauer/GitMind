using System.Collections.Generic;
using System.Linq;
using ProtoBuf;


namespace GitMind.GitModel.Private
{
	[ProtoContract]
	public class MBranch
	{
		[ProtoMember(1)]
		public string Id { get; set; }
		[ProtoMember(2)]
		public string Name { get; set; }
		[ProtoMember(3)]
		public string LatestCommitId { get; set; }
		[ProtoMember(4)]
		public string FirstCommitId { get; set; }
		[ProtoMember(5)]
		public string ParentCommitId { get; set; }
		[ProtoMember(6)]
		public string ParentBranchId { get; set; }
		[ProtoMember(7)]
		public bool IsMultiBranch { get; set; }
		[ProtoMember(8)]
		public bool IsActive { get; set; }
		[ProtoMember(9)]
		public bool IsAnonymous { get; set; }
		[ProtoMember(10)]
		public int LocalAheadCount { get; set; }
		[ProtoMember(11)]
		public int RemoteAheadCount { get; set; }
		[ProtoMember(12)]
		public bool IsLocalAndRemote { get; set; }
		[ProtoMember(13)]
		public List<string> ChildBranchNames { get; set; } = new List<string>();

		[ProtoMember(14)]
		public List<string> CommitIds { get; set; } = new List<string>();



		public MRepository Repository { get; set; }

		public IEnumerable<MCommit> Commits =>
			CommitIds.Select(id => Repository.Commits[id]);

		public MCommit FirstCommit => Repository.Commits[FirstCommitId];
		public MCommit LatestCommit => Repository.Commits[LatestCommitId];
		public MCommit ParentCommit => Repository.Commits[ParentCommitId];
		public MBranch ParentBranch => Repository.Branches[ParentBranchId];

		public override string ToString() => $"{Name}";
	}
}