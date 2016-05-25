using System.Collections.Generic;


namespace GitMind.GitModel.Private
{
	internal class MBranch
	{
		public MBranch(MRepository mRepository)
		{
			MRepository = mRepository;
		}


		public MRepository MRepository { get; }

		public string Id { get; set; }
		public string Name { get; set; }

		public string LatestCommitId { get; set; }
		public string FirstCommitId { get; set; }
		public string ParentCommitId { get; set; }

		public string ParentBranchId { get; set; }

		public bool IsMultiBranch { get; set; }
		public bool IsActive { get; set; }
		public bool IsAnonymous { get; set; }


		public List<MSubBranch> SubBranches { get; } = new List<MSubBranch>();
		public List<MCommit> Commits { get; } = new List<MCommit>();

		public List<MBranch> ChildBranches { get; } = new List<MBranch>();

		//public int RemoteAheadCount { get; set; }
		//public int LocalAheadCount { get; set; }
		//public string TrackingName { get; set; }
		//public string LastestLocalCommitId { get; set; }
		//public string LastestTrackingCommitId { get; set; }


		public MCommit FirstCommit => MRepository.Commits[FirstCommitId];
		public MCommit LatestCommit => MRepository.Commits[LatestCommitId];
		public MCommit ParentCommit => MRepository.Commits[ParentCommitId];
		public MBranch ParentBranch => MRepository.Branches[ParentBranchId];

		public override string ToString() => $"{Name}";
	}
}