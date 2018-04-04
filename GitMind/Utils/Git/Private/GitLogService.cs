using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.GitModel.Private;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitLogService : IGitLogService
	{
		private static readonly string LogFormat = "%H|%ai|%ci|%an|%P|%s";
		private static readonly List<CommitId> NoParents = new List<CommitId>();
		private static readonly char[] IdSplitter = " ".ToCharArray();
		private static readonly char[] LogRowSplitter = "|".ToCharArray();

		private readonly IGitCmdService gitCmdService;


		public GitLogService(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R<IReadOnlyList<GitCommit>>> GetLogAsync(CancellationToken ct)
		{
			List<GitCommit> commits = new List<GitCommit>();

			R result = await GetLogAsync(commit => commits.Add(commit), ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to get log", result);
			}

			Log.Info($"Got log for {commits.Count} commits");
			return commits;
		}


		public async Task<R> GetLogAsync(Action<GitCommit> commits, CancellationToken ct)
		{
			int count = 0;

			void OutputLines(string line)
			{
				if (!ct.IsCancellationRequested)
				{
					count++;
					commits(Parse(line));
				}
			}

			R<CmdResult2> result = await gitCmdService.RunAsync($"log --all --pretty=\"{LogFormat}\"", OutputLines, ct);

			if (result.IsFaulted && !ct.IsCancellationRequested)
			{
				return Error.From("Failed to get log", result);
			}

			Log.Info($"Got log for {count} commits");
			return R.Ok;
		}


		public async Task<R<GitCommit>> GetCommitAsync(string sha, CancellationToken ct)
		{
			var result = await gitCmdService.RunAsync($"show --no-patch --pretty=\"{LogFormat}\" {sha}", ct);
			if (result.IsFaulted)
			{
				return Error.From("Failed to get commit log", result);
			}

			GitCommit commit = Parse(result.Value.OutputLines.First());
			Log.Debug($"Got log for commit {commit.Sha.ShortSha}");
			return commit;
		}


		private static GitCommit Parse(string line)
		{
			try
			{
				string[] parts = line.Split(LogRowSplitter);

				string subject = GetSubject(parts);

				var gitCommit = new GitCommit(
					sha: new CommitSha(parts[0]),
					subject: subject,
					message: subject,
					author: parts[3],
					parentIds: GetParentIds(parts),
					authorDate: DateTime.Parse(parts[1]),
					commitDate: DateTime.Parse(parts[2]));
				return gitCommit;
			}
			catch (Exception e)
			{
				Log.Exception(e, $"Failed to parse log line:\n{line}");
				throw;
			}
		}


		private static string GetSubject(IReadOnlyList<string> lineParts)
		{
			if (lineParts.Count > 6)
			{
				// The subject contains one or more "|", so rejoin these parts into original subject
				return string.Join("|", lineParts.Skip(5));
			}

			return lineParts[5];
		}


		private static List<CommitId> GetParentIds(IReadOnlyList<string> lineParts) =>
			string.IsNullOrWhiteSpace(lineParts[4])
				? NoParents : lineParts[4].Split(IdSplitter).Select(sha => new CommitId(sha)).ToList();
	}
}