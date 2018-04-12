﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel.Private;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCommitService2 : IGitCommitService2
	{
		private static readonly Regex CommitOutputRegEx = new Regex(@"^\[(\S*)\s+(\(.*\)\s+)?(\w+)\]",
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

		private readonly IGitCmdService gitCmdService;
		private readonly IGitDiffService2 gitDiffService2;
		private readonly IGitLogService gitLogService;


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


		public Task<R<string>> GetCommitMessageAsync(string sha, CancellationToken ct) =>
			gitLogService.GetCommitMessageAsync(sha, ct);


		public async Task<R> UndoCommitAsync(string sha, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync($"revert --no-commit {sha}", ct);
			if (result.IsFaulted)
			{
				return Error.From($"Undo {sha} commit failed.", result);
			}

			Log.Info($"Undid commit {sha}");
			return result;
		}


		public async Task<R> UnCommitAsync(CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync("reset HEAD~1", ct);
			if (result.IsFaulted)
			{
				return Error.From("Uncommit commit failed.", result);
			}

			Log.Info("Uncomitted commit");
			return result;
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
	}
}