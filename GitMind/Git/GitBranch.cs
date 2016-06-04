namespace GitMind.Git
{
	internal class GitBranch
	{
		public GitBranch(
			string name,
			string latestCommitId,
			bool isCurrent,
			string trackingBranchName,
			bool isRemote)
		{
			Name = name;
			LatestCommitId = latestCommitId;
			IsCurrent = isCurrent;
			TrackingBranchName = trackingBranchName;
			IsRemote = isRemote;
		}


		public string Name { get; }
		public string LatestCommitId { get; }
		public bool IsCurrent { get; }
		public string TrackingBranchName { get; }
		public bool IsRemote { get; }

		public override string ToString() => Name;
	}
}