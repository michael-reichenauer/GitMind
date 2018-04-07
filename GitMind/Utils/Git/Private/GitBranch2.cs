using GitMind.Common;


namespace GitMind.Utils.Git.Private
{
	public class GitBranch2
	{
		public string BranchName { get; }
		public CommitSha TipSha { get; }
		public bool IsCurrent { get; }
		public string Message { get; }
		public string BoundBranchName { get; }
		public int AheadCount { get; }
		public int BehindCount { get; }
		public bool IsRemoteMissing { get; }
		public bool IsRemote { get; }
		public bool IsLocal => !IsRemote;

		public bool IsAhead => AheadCount > 0;
		public bool IsBehind => BehindCount > 0;

		public bool IsPushable =>
			!string.IsNullOrEmpty(BoundBranchName) &&
			(AheadCount > 0 || IsRemoteMissing) &&
			(BehindCount == 0);

		public bool IsFetchable =>
			!string.IsNullOrEmpty(BoundBranchName) &&
			BehindCount > 0 &&
			AheadCount == 0;

		public GitBranch2(string branchName,
			CommitSha tipSha,
			bool isCurrent,
			string message,
			string boundBranchName,
			int aheadCount,
			int behindCount,
			bool isRemoteMissing)
		{
			IsRemote = branchName.StartsWith("remotes/");
			BranchName = !IsRemote ? branchName : branchName.Substring(8);
			TipSha = tipSha;
			IsCurrent = isCurrent;
			Message = message;
			BoundBranchName = boundBranchName;
			AheadCount = aheadCount;
			BehindCount = behindCount;
			IsRemoteMissing = isRemoteMissing;
		}


		public override string ToString() =>
			$"{BranchName} {TipSha.ShortSha} ({CurrentText}{AheadCount}A {BehindCount}B) ->{BoundBranchName}";


		private string CurrentText => IsCurrent ? "* " : "";
	}
}