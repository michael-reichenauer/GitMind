namespace GitMind.GitModel.Private
{
	internal class MSubBranch
	{
		public MSubBranch(MRepository mRepository)
		{
			MRepository = mRepository;
		}


		public MRepository MRepository { get; }

		public string Id { get; set; }
		public string BranchId { get; set; }

		public string Name { get; set; }

		public string LatestCommitId { get; set; }
		public string FirstCommitId { get; set; }
		public string ParentCommitId { get; set; }

		public bool IsMultiBranch { get; set; }
		public bool IsActive { get; set; }
		public bool IsAnonymous { get; set; }
		public bool IsRemote { get; set; }

		//public int RemoteAheadCount { get; set; }
		//public int LocalAheadCount { get; set; }
		//public string TrackingName { get; set; }
		//public string LastestLocalCommitId { get; set; }
		//public string LastestTrackingCommitId { get; set; }


		public MCommit FirstCommit => MRepository.Commits[FirstCommitId];
		public MCommit LatestCommit => MRepository.Commits[LatestCommitId];
		public MCommit ParentCommit => MRepository.Commits[ParentCommitId];

		public override string ToString() => $"{Name} ({IsRemote})";
	}
}