using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitBranchService2 : IGitBranchService2
	{
		public static readonly Regex branchesRegEx = new Regex(
			@"^(\*)?\s+(\S+)\s+(\S+)(\s+)?(\[\S+\])?(\s+)?(.+)?",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline);



		private readonly IGitCmdService gitCmdService;


		public GitBranchService2(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R<GitAheadBehind>> GetAheadBehindAsync(
			string branchName, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(
				$"rev-list --left-right --count --branches origin/{branchName}...branchName", ct);

			if (result.IsFaulted)
			{
				// Getting ahead/behind failed, but it might be a local branch lets try that
				if (IsLocalBranch(result.Error.Message))
				{
					R<CmdResult2> localResult = await gitCmdService.RunAsync(
						$"rev-list --left-right --count --branches {branchName}", ct);

					if (localResult.IsOk)
					{
						GitAheadBehind ahead = ParseAheadBehind(branchName, localResult.Value);
						Log.Info($"Local ahead: {ahead} in {result.Value.WorkingDirectory}");
						return ahead;
					}
				}

				return Error.From($"Failed to get ahead/behind for {branchName}", result);
			}

			GitAheadBehind aheadBehind = ParseAheadBehind(branchName, result.Value);
			Log.Info($"ahead/behind: {aheadBehind} in {result.Value.WorkingDirectory}");
			return aheadBehind;
		}


		public async Task<R<IReadOnlyList<GitBranch2>>> GetBranchesAsync(CancellationToken ct)
		{
			List<GitBranch2> branches = new List<GitBranch2>();
			R<CmdResult2> result = await gitCmdService.RunAsync("branch -vv --no-color --no-abbrev --all", ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to get branhes", result);
			}

			var matches = branchesRegEx.Matches(result.Value.Output);

			return branches;
		}


		private static bool IsLocalBranch(string errorMessage) =>
			errorMessage.StartsWith("fatal: ambiguous argument");



		private GitAheadBehind ParseAheadBehind(string branchName, CmdResult2 result)
		{
			string[] parts = result.Output.Trim().Split("\t".ToCharArray());
			return new GitAheadBehind(branchName, int.Parse(parts[0]), int.Parse(parts[1]));
		}
	}
}