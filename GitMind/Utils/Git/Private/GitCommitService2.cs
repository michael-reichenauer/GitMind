using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel.Private;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCommitService2 : IGitCommitService2
	{
		public static readonly Regex CommitOutputRegEx = new Regex(@"^\[(\S*)\s+(\(.*\)\s+)?(\w+)\]",
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

		public static readonly Regex CleanOutputRegEx = new Regex(@"warning: failed to remove ([^:]+):",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);


		private readonly IGitCmdService gitCmdService;
		private readonly IGitDiffService2 gitDiffService2;
		private readonly IGitLogService gitLogService;
		private static readonly IReadOnlyList<string> EmptyFileList = new string[0].AsReadOnlyList();


		public GitCommitService2(
			IGitCmdService gitCmdService,
			IGitDiffService2 gitDiffService2,
			IGitLogService gitLogService)
		{
			this.gitCmdService = gitCmdService;
			this.gitDiffService2 = gitDiffService2;
			this.gitLogService = gitLogService;
		}

		public Task<R<GitCommit>> GetCommitAsync(string sha, CancellationToken ct) =>
			gitLogService.GetCommitAsync(sha, ct);


		public Task<R<IReadOnlyList<GitFile2>>> GetCommitFilesAsync(string sha, CancellationToken ct) =>
			gitDiffService2.GetFilesAsync(sha, ct);


		public async Task<R<IReadOnlyList<string>>> UndoUncommitedAsync(CancellationToken ct)
		{
			R<IReadOnlyList<string>> result = await CleanFolderAsync("-fd", ct);
			if (result.IsFaulted)
			{
				return Error.From("Failed to undo uncommited changes", result);
			}

			Log.Info("Undid uncommited changes");
			return result;
		}


		public async Task<R<IReadOnlyList<string>>> CleanWorkingFolderAsync(CancellationToken ct)
		{
			R<IReadOnlyList<string>> result = await CleanFolderAsync("-fxd", ct);
			if (result.IsFaulted)
			{
				return Error.From("Failed to clean working folder", result);
			}

			Log.Info("Cleaned working folder");
			return result;
		}


		private async Task<R<IReadOnlyList<string>>> CleanFolderAsync(string cleanArgs, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync("reset --hard", ct);
			if (result.IsFaulted)
			{
				return Error.From("Reset failed.", result);
			}

			CmdResult2 cleanResult = await gitCmdService.RunCmdAsync($"clean {cleanArgs}", ct);
			if (cleanResult.IsFaulted)
			{
				if (IsFailedToRemoveSomeFiles(cleanResult, out IReadOnlyList<string> failedFiles))
				{
					Log.Warn($"Failed to clean {failedFiles.Count} files");
					return R.From(failedFiles);
				}

				return Error.From(cleanResult.ToString());
			}

			return R.From(EmptyFileList);
		}


		public async Task<R<GitCommit>> CommitAllChangesAsync(string message, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync("add .", ct);
			if (result.IsFaulted)
			{
				return Error.From("Failed to stage using add before commit", result);
			}

			result = await gitCmdService.RunAsync($"commit -am \"{message}\"", ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to commit", result);
			}

			if (CommitOutputRegEx.TryMatch(result.Value.Output, out Match match))
			{
				// string branch = match.Groups[1].Value;.
				string shortId = match.Groups[3].Value;
				R<GitCommit> commit = await GetCommitAsync(shortId, ct);

				Log.Info($"Commited {commit}");
				return commit;
			}

			return Error.From("Commit succeded, but failed to parse commit id from output");
		}


		private static bool IsFailedToRemoveSomeFiles(CmdResult2 result, out IReadOnlyList<string> failedFiles)
		{
			// Check if error message contains any "warning: failed to remove <file>:"
			failedFiles = CleanOutputRegEx.Matches(result.Error).OfType<Match>()
				.Select(match => match.Groups[1].Value).ToList();

			return failedFiles.Any();
		}
	}
}