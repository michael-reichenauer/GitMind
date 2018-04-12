using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitBranchService2 : IGitBranchService2
	{
		//public static readonly Regex BranchesRegEx = new Regex(
		//	@"^(\*)?\s+(\S+)\s+(\S+)(\s+)?(\[:\s(ahead\s(\d+))?(,\s)?(behind\s(\d+))?(gone)?\])?(\s+)?(.+)?",
		//	RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline);

		public static readonly Regex BranchesRegEx = new Regex(
			@"^(\*)?\s+(\S+)\s+(\S+)(\s+)?(\[(\S+)(:\s)?(ahead\s(\d+))?(,\s)?(behind\s(\d+))?(gone)?\])?(\s+)?(.+)?",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline);

		private readonly IGitCmdService gitCmdService;


		public GitBranchService2(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		//public async Task<R<GitAheadBehind>> GetAheadBehindAsync(
		//	string branchName, CancellationToken ct)
		//{
		//	R<CmdResult2> result = await gitCmdService.RunAsync(
		//		$"rev-list --left-right --count --branches origin/{branchName}...branchName", ct);

		//	if (result.IsFaulted)
		//	{
		//		// Getting ahead/behind failed, but it might be a local branch lets try that
		//		if (IsLocalBranch(result.Error.Message))
		//		{
		//			R<CmdResult2> localResult = await gitCmdService.RunAsync(
		//				$"rev-list --left-right --count --branches {branchName}", ct);

		//			if (localResult.IsOk)
		//			{
		//				GitAheadBehind ahead = ParseAheadBehind(branchName, localResult.Value);
		//				Log.Info($"Local ahead: {ahead} in {result.Value.WorkingDirectory}");
		//				return ahead;
		//			}
		//		}

		//		return Error.From($"Failed to get ahead/behind for {branchName}", result);
		//	}

		//	GitAheadBehind aheadBehind = ParseAheadBehind(branchName, result.Value);
		//	Log.Info($"ahead/behind: {aheadBehind} in {result.Value.WorkingDirectory}");
		//	return aheadBehind;
		//}


		public async Task<R<IReadOnlyList<GitBranch2>>> GetBranchesAsync(CancellationToken ct)
		{
			List<GitBranch2> branches = new List<GitBranch2>();
			R<CmdResult2> result = await gitCmdService.RunAsync("branch -vv --no-color --no-abbrev --all", ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to get branches", result);
			}

			var matches = BranchesRegEx.Matches(result.Value.Output);
			foreach (Match match in matches)
			{
				if (!IsPointerBranch(match))
				{
					GitBranch2 branch = ToBranch(match);
					branches.Add(branch);
				}
			}

			Log.Debug($"Got {branches.Count} branches in {result.Value.WorkingDirectory}");
			return branches;
		}


		public async Task<R> BranchAsync(string name, bool isCheckout, CancellationToken ct)
		{
			string args = isCheckout ? "checkout -b" : "branch";
			R<CmdResult2> result = await gitCmdService.RunAsync($"{args} {name}", ct);

			if (result.IsFaulted)
			{
				return Error.From($"Failed to create branch {name}", result);
			}

			Log.Debug($"Created branch {name}");
			return result;
		}


		public async Task<R> BranchFromCommitAsync(string name, string sha, bool isCheckout, CancellationToken ct)
		{
			string args = isCheckout ? "checkout -b" : "branch";
			R<CmdResult2> result = await gitCmdService.RunAsync($"{args} {name} {sha}", ct);

			if (result.IsFaulted)
			{
				return Error.From($"Failed to create branch {name}", result);
			}

			Log.Debug($"Created branch {name} at {sha}");
			return result;
		}



		private static GitBranch2 ToBranch(Match match)
		{
			bool isCurrent = match.Groups[1].Value == "*";
			string branchName = match.Groups[2].Value;
			CommitSha tipSha = new CommitSha(match.Groups[3].Value);
			string boundBranchName = match.Groups[6].Value;
			int.TryParse(match.Groups[9].Value, out int aheadCount);
			int.TryParse(match.Groups[12].Value, out int behindCount);
			bool isRemoteMissing = match.Groups[13].Value == "gone";
			string message = (match.Groups[15].Value ?? "").TrimEnd('\r');

			GitBranch2 branch = new GitBranch2(
				branchName, tipSha, isCurrent, message, boundBranchName, aheadCount, behindCount, isRemoteMissing);
			return branch;
		}


		private static bool IsPointerBranch(Match match) => match.Groups[3].Value == "->";


		//private static bool IsLocalBranch(string errorMessage) =>
		//	errorMessage.StartsWith("fatal: ambiguous argument");



		//private GitAheadBehind ParseAheadBehind(string branchName, CmdResult2 result)
		//{
		//	string[] parts = result.Output.Trim().Split("\t".ToCharArray());
		//	return new GitAheadBehind(branchName, int.Parse(parts[0]), int.Parse(parts[1]));
		//}
	}
}