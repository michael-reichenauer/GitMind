using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitLog : IGitLog
	{
		private static readonly string GitLogArgs = "log --all --pretty=\"%H|%ai|%ci|%an|%P|%s\"";
		private static readonly string[] NoParents = new string[0];
		private static readonly char[] IdSplitter = " ".ToCharArray();
		private static readonly char[] LogRowSplitter = "|".ToCharArray();

		private readonly IGitCmd gitCmd;


		public GitLog(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<IReadOnlyList<LogCommit>> GetAsync(CancellationToken ct)
		{
			List<LogCommit> commits = new List<LogCommit>();

			await GetAsync(commit => commits.Add(commit), ct);

			return commits;
		}


		public async Task GetAsync(Action<LogCommit> commits, CancellationToken ct)
		{
			CmdResult2 result = await gitCmd.RunAsync(GitLogArgs, line => commits(Parse(line)), ct);

			if (result.ExitCode != 0 && !ct.IsCancellationRequested)
			{
				ApplicationException e = new ApplicationException($"Failed to get git log: {result}");
				Log.Exception(e, "Failed to get log");
				throw e;
			}
		}


		private static LogCommit Parse(string line)
		{
			try
			{
				string[] parts = line.Split(LogRowSplitter);

				string subject = GetSubject(parts);

				var gitCommit = new LogCommit(
					sha: parts[0],
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


		private static IReadOnlyList<string> GetParentIds(IReadOnlyList<string> lineParts) =>
			string.IsNullOrWhiteSpace(lineParts[4]) ? NoParents : lineParts[4].Split(IdSplitter);
	}
}