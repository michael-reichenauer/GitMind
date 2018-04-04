using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCommit : IGitCommit
	{
		public static readonly Regex CommitOutputRegEx = new Regex(@"^\[(\w*)\s+(\(.*\)\s+)?(\w+)\]",
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);


		private readonly IGitCmd gitCmd;
		private readonly IGitDiff gitDiff;


		public GitCommit(IGitCmd gitCmd, IGitDiff gitDiff)
		{
			this.gitCmd = gitCmd;
			this.gitDiff = gitDiff;
		}


		public Task<R<IReadOnlyList<GitFile2>>> GetCommitFilesAsync(string commit, CancellationToken ct) =>
			gitDiff.GetFilesAsync(commit, ct);


		public async Task<R<string>> CommitAllChangesAsync(string message, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmd.RunAsync("add .", ct);
			if (result.IsFaulted)
			{
				return Error.From("Failed to stage using add before commit", result);
			}

			result = await gitCmd.RunAsync($"commit -am \"{message}\"", ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to commit", result);
			}

			if (CommitOutputRegEx.TryMatch(result.Value.Output, out Match match))
			{
				// string branch = match.Groups[1].Value;
				string shortId = match.Groups[3].Value;
				result = await gitCmd.RunAsync($"rev-parse --verify {shortId}", ct);
				string commitId = result.Value.Output.Trim();

				Log.Info($"Commted {commitId} '{message}'");
				return commitId;
			}

			return Error.From("Commit succeded, but failed to parse commit id from output");
		}
	}
}