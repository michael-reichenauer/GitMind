using System.Collections.Generic;
using System.Linq;
using GitMind.Git;


namespace GitMind.GitModel.Private
{	
	public class MBranch
	{
		public string Id { get; set; }
		public BranchName Name { get; set; }
		public string TipCommitId { get; set; }
		public string FirstCommitId { get; set; }	
		public string ParentCommitId { get; set; }
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
		public List<string> CommitIds { get; set; } = new List<string>();
	
		public string LocalTipCommitId { get; set; }
		public string RemoteTipCommitId { get; set; }
		public bool IsLocalPart { get; set; }
		public bool IsMainPart { get; set; }
		public string MainBranchId { get; set; }
		public string LocalSubBranchId { get; set; }

		public List<string> TempCommitIds { get; set; } = new List<string>();

		public MRepository Repository { get; set; }

		public IEnumerable<MCommit> Commits => CommitIds.Select(id => Repository.Commits[id]);


		public MCommit FirstCommit => Repository.Commits[FirstCommitId];
		public MCommit TipCommit => Repository.Commits[TipCommitId];
		public MCommit ParentCommit => Repository.Commits[ParentCommitId];
		public MBranch ParentBranch => Repository.Branches[ParentBranchId];



		public override string ToString() => IsLocalPart ? $"{Name} (local)" : $"{Name}";
	}
}