namespace GitMind.Git
{
	internal class GitBranch
	{
		public GitBranch(
			string name,
			string latestCommitId,
			bool isCurrent,
			string trackingBranchName,
			string latestTrackingCommitId,
			bool isRemote,
			bool isAnonyous)
		{
			Name = name;
			LatestCommitId = latestCommitId;
			IsCurrent = isCurrent;
			TrackingBranchName = trackingBranchName;
			LatestTrackingCommitId = latestTrackingCommitId;
			IsRemote = isRemote;
			IsAnonyous = isAnonyous;
		}


		public string Name { get; }

		public string LatestCommitId { get; }
		public bool IsCurrent { get; set; }
		public string TrackingBranchName { get; }
		public string LatestTrackingCommitId { get; }
		public bool IsRemote { get; }
		public bool IsAnonyous { get; }

		public override string ToString() => Name;
	}
}