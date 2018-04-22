using GitMind.Common;


namespace GitMind.Utils.Git.Private
{
	public class GitBranch2
	{
		public string Name { get; }
		public CommitSha TipSha { get; }
		public bool IsCurrent { get; }
		public string Message { get; }
		public string RemoteName { get; }
		public int AheadCount { get; }
		public int BehindCount { get; }
		public bool IsRemoteMissing { get; }
		public bool IsDetached { get; }
		public bool IsRemote { get; }
		public bool IsLocal => !IsRemote;

		public bool IsAhead => AheadCount > 0;
		public bool IsBehind => BehindCount > 0;
		public bool IsTracking => !string.IsNullOrEmpty(RemoteName);

		public bool IsPushable =>
			!string.IsNullOrEmpty(RemoteName) &&
			(AheadCount > 0 || IsRemoteMissing) &&
			(BehindCount == 0);

		public bool IsFetchable =>
			!string.IsNullOrEmpty(RemoteName) &&
			BehindCount > 0 &&
			AheadCount == 0;

		public GitBranch2(
			string branchName,
			CommitSha tipSha,
			bool isCurrent,
			string message,
			string remoteName,
			int aheadCount,
			int behindCount,
			bool isRemoteMissing,
			bool isDetached)
		{
			IsRemote = branchName.StartsWith("remotes/");
			Name = !IsRemote ? branchName : branchName.Substring(8);
			TipSha = tipSha;
			IsCurrent = isCurrent;
			Message = message;
			RemoteName = remoteName;
			AheadCount = aheadCount;
			BehindCount = behindCount;
			IsRemoteMissing = isRemoteMissing;
			IsDetached = isDetached;
		}


		public override string ToString() =>
			$"{Name} {TipSha.ShortSha} ({CurrentText}{AheadCount}A {BehindCount}B) ->{RemoteName}";


		private string CurrentText => IsCurrent ? "* " : "";
	}
}