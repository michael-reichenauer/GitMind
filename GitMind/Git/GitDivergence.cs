using GitMind.Common;


namespace GitMind.Git
{
	internal class GitDivergence
	{
		public GitDivergence(
			CommitId localId,
			CommitId remoteId,
			CommitId commonId,
			int aheadBy,
			int behindBy)
		{
			LocalId = localId;
			RemoteId = remoteId;
			CommonId = commonId;
			AheadBy = aheadBy;
			BehindBy = behindBy;
		}

		public CommitId LocalId { get; }
		public CommitId RemoteId { get; }
		public CommitId CommonId { get; }
		public int AheadBy { get; }
		public int BehindBy { get; }
	}
}