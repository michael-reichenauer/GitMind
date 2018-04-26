using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitMind.Common;
using GitMind.Git;


namespace GitMind.GitModel.Private
{	
	[DataContract]
	public class MBranch
	{
		[DataMember] public string Id { get; set; }
		[DataMember] public BranchName Name { get; set; }
		[DataMember] public CommitId TipCommitId { get; set; }
		[DataMember] public CommitId FirstCommitId { get; set; }
		[DataMember] public CommitId ParentCommitId { get; set; }
		[DataMember] public string ParentBranchId { get; set; }
		[DataMember] public bool IsMultiBranch { get; set; }
		[DataMember] public bool IsActive { get; set; }
		[DataMember] public bool IsCurrent { get; set; }
		[DataMember] public bool IsDetached { get; set; }
		[DataMember] public bool IsLocal { get; set; }
		[DataMember] public bool IsRemote { get; set; }
		[DataMember] public int LocalAheadCount { get; set; }
		[DataMember] public int RemoteAheadCount { get; set; }
		[DataMember] public bool IsLocalAndRemote { get; set; }
		[DataMember] public List<BranchName> ChildBranchNames { get; set; } = new List<BranchName>();
		[DataMember] public List<CommitId> CommitIds { get; set; } = new List<CommitId>();

		[DataMember] public CommitId LocalTipCommitId { get; set; }
		[DataMember] public CommitId RemoteTipCommitId { get; set; }
		[DataMember] public bool IsLocalPart { get; set; }
		[DataMember] public bool IsMainPart { get; set; }
		[DataMember] public string MainBranchId { get; set; }
		[DataMember] public string LocalSubBranchId { get; set; }

		public List<CommitId> TempCommitIds { get; set; } = new List<CommitId>();

		public MRepository Repository { get; set; }

		public IEnumerable<MCommit> Commits => CommitIds.Select(id => Repository.Commits[id]);


		public MCommit FirstCommit => Repository.Commits[FirstCommitId];
		public MCommit TipCommit => Repository.Commits[TipCommitId];
		public MCommit ParentCommit => Repository.Commits[ParentCommitId];
		public MBranch ParentBranch => Repository.Branches[ParentBranchId];
		public bool HasParentBranch => ParentCommitId != null;


		public override string ToString() => IsLocalPart ? $"{Name} (local)" : $"{Name}";
	}
}