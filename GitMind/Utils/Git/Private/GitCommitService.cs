using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.GitModel.Private;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCommitService : IGitCommitService
	{
		private static readonly Regex CommitOutputRegEx = new Regex(@"^\[(\S*)\s+(\(.*\)\s+)?(\w+)\]",
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

		private readonly IGitCmdService gitCmdService;
		private readonly IGitDiffService gitDiffService;
		private readonly IGitLogService gitLogService;
		private readonly WorkingFolder workingFolder;


		public GitCommitService(
			IGitCmdService gitCmdService,
			IGitDiffService gitDiffService,
			IGitLogService gitLogService,
			WorkingFolder workingFolder)
		{
			this.gitCmdService = gitCmdService;
			this.gitDiffService = gitDiffService;
			this.gitLogService = gitLogService;
			this.workingFolder = workingFolder;
		}

		public Task<R<GitCommit>> GetCommitAsync(string sha, CancellationToken ct) =>
			gitLogService.GetCommitAsync(sha, ct);


		public Task<R<IReadOnlyList<GitFile>>> GetCommitFilesAsync(string sha, CancellationToken ct) =>
			gitDiffService.GetFilesAsync(sha, ct);


		public Task<R<string>> GetCommitMessageAsync(string sha, CancellationToken ct) =>
			gitLogService.GetCommitMessageAsync(sha, ct);


		public async Task<R> UndoCommitAsync(string sha, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync($"revert --no-commit {sha}", ct);
			if (result.IsFaulted)
			{
				return R.Error($"Undo {sha} commit failed.", result.Exception);
			}

			Log.Info($"Undid commit {sha}");
			return result;
		}


		public async Task<R> UnCommitAsync(CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync("reset HEAD~1", ct);
			if (result.IsFaulted)
			{
				return R.Error("Uncommit commit failed.", result.Exception);
			}

			Log.Info("Uncommitted commit");
			return result;
		}


		public async Task<R<GitCommit>> CommitAllChangesAsync(string message, CancellationToken ct)
		{
			R<CmdResult2> result;
			if (!IsMergeInProgress())
			{
				result = await gitCmdService.RunAsync("add .", ct);
				if (result.IsFaulted)
				{
					return R.Error("Failed to stage using add before commit", result.Exception);
				}
			}

			result = await gitCmdService.RunAsync($"commit -am \"{message}\"", ct);

			if (result.IsFaulted)
			{
				return R.Error("Failed to commit", result.Exception);
			}

			if (CommitOutputRegEx.TryMatch(result.Value.Output, out Match match))
			{
				// string branch = match.Groups[1].Value;.
				string shortId = match.Groups[3].Value;
				R<GitCommit> commit = await GetCommitAsync(shortId, ct);

				Log.Info($"Committed {commit}");
				return commit;
			}

			return R.Error("Commit succeeded, but failed to parse commit id from output");
		}

		private bool IsMergeInProgress()
		{
			string mergeIpPath = Path.Combine(workingFolder, ".git", "MERGE_HEAD");
			bool isMergeInProgress = File.Exists(mergeIpPath);
			return isMergeInProgress;
		}
	}
}