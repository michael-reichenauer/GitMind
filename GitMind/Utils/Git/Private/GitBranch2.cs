using System.Collections.Generic;
using System.Linq;
using GitMind.Common;


namespace GitMind.Utils.Git.Private
{
	public static class GitBranchesExtensions
	{
		public static bool TryGet(this IEnumerable<GitBranch2> branches, string branchName, out GitBranch2 branch)
		{
			branch = branches.FirstOrDefault(b => b.Name == branchName);
			return branch != null;
		}
	}


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
		public bool IsRemote { get; }
		public bool IsLocal => !IsRemote;

		public bool IsAhead => AheadCount > 0;
		public bool IsBehind => BehindCount > 0;

		public bool IsPushable =>
			!string.IsNullOrEmpty(RemoteName) &&
			(AheadCount > 0 || IsRemoteMissing) &&
			(BehindCount == 0);

		public bool IsFetchable =>
			!string.IsNullOrEmpty(RemoteName) &&
			BehindCount > 0 &&
			AheadCount == 0;

		public GitBranch2(string branchName,
			CommitSha tipSha,
			bool isCurrent,
			string message,
			string remoteName,
			int aheadCount,
			int behindCount,
			bool isRemoteMissing)
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
		}


		public override string ToString() =>
			$"{Name} {TipSha.ShortSha} ({CurrentText}{AheadCount}A {BehindCount}B) ->{RemoteName}";


		private string CurrentText => IsCurrent ? "* " : "";
	}
}