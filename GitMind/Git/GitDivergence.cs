namespace GitMind.Git
{
	internal class GitDivergence
	{
		public GitDivergence(
			string localId,
			string remoteId,
			string commonId,
			int aheadBy,
			int behindBy)
		{
			LocalId = localId;
			RemoteId = remoteId;
			CommonId = commonId;
			AheadBy = aheadBy;
			BehindBy = behindBy;
		}

		public string LocalId { get; }
		public string RemoteId { get; }
		public string CommonId { get; }
		public int AheadBy { get; }
		public int BehindBy { get; }
	}
}