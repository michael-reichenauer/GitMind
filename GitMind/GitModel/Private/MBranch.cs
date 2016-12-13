using System.Collections.Generic;
using System.Linq;
using GitMind.Common;
using GitMind.Git;


namespace GitMind.GitModel.Private
{	
	public class MBranch
	{
		// Serialized start -------------------

		public string Id { get; set; }
		public BranchName Name { get; set; }
		public CommitId TipCommitId { get; set; }
		public CommitId FirstCommitId { get; set; }	
		public CommitId ParentCommitId { get; set; }
		public string ParentBranchId { get; set; }
		public bool IsMultiBranch { get; set; }
		public bool IsActive { get; set; }
		public bool IsCurrent { get; set; }		
		public bool IsDetached { get; set; }
		public bool IsLocal { get; set; }
		public bool IsRemote { get; set; }
		public int LocalAheadCount { get; set; }	
		public int RemoteAheadCount { get; set; }
		public bool IsLocalAndRemote { get; set; }
		public List<BranchName> ChildBranchNames { get; set; } = new List<BranchName>();
		public List<CommitId> CommitIds { get; set; } = new List<CommitId>();

		public CommitId LocalTipCommitId { get; set; }
		public CommitId RemoteTipCommitId { get; set; }
		public bool IsLocalPart { get; set; }
		public bool IsMainPart { get; set; }
		public string MainBranchId { get; set; }
		public string LocalSubBranchId { get; set; }

		// Serialized Done ---------------------


		public List<CommitId> TempCommitIds { get; set; } = new List<CommitId>();

		public MRepository Repository { get; set; }

		public IEnumerable<MCommit> Commits => CommitIds.Select(id => Repository.Commits[id]);


		public MCommit FirstCommit => Repository.Commits[FirstCommitId];
		public MCommit TipCommit => Repository.Commits[TipCommitId];
		public MCommit ParentCommit => Repository.Commits[ParentCommitId];
		public MBranch ParentBranch => Repository.Branches[ParentBranchId];



		public override string ToString() => IsLocalPart ? $"{Name} (local)" : $"{Name}";
	}
}