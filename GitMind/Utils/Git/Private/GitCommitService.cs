using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel.Private;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCommitService : IGitCommit
	{
		public static readonly Regex CommitOutputRegEx = new Regex(@"^\[(\S*)\s+(\(.*\)\s+)?(\w+)\]",
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);


		private readonly IGitCmd gitCmd;
		private readonly IGitDiff gitDiff;
		private readonly IGitLog gitLog;


		public GitCommitService(IGitCmd gitCmd, IGitDiff gitDiff, IGitLog gitLog)
		{
			this.gitCmd = gitCmd;
			this.gitDiff = gitDiff;
			this.gitLog = gitLog;
		}

		public Task<R<GitCommit>> GetCommitAsync(string sha, CancellationToken ct) =>
			gitLog.GetCommitAsync(sha, ct);


		public Task<R<IReadOnlyList<GitFile2>>> GetCommitFilesAsync(string sha, CancellationToken ct) =>
			gitDiff.GetFilesAsync(sha, ct);


		public async Task<R<GitCommit>> CommitAllChangesAsync(string message, CancellationToken ct)
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