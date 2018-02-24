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
			CmdResult2 result = await gitCmd.DoAsync("log --all --pretty=\"%H|%ai|%ci|%an|%P|%s\"", ct);

			if (result.ExitCode != 0)
			{
				Log.Warn($"Failed to get git log, {result}");
				return new List<LogCommit>();
			}

			IReadOnlyList<string> logLines = result.Output.Trim().Split("\n".ToCharArray());

			List<LogCommit> commits = new List<LogCommit>(logLines.Count);

			foreach (string line in logLines)
			{
				string[] parts = line.Split(LogRowSplitter);

				string subjec = GetSubject(parts);

				var gitCommit = new LogCommit(
					sha: parts[0],
					subject: subjec,
					message: subjec,
					author: parts[3],
					parentIds: GetParentIds(parts),
					authorDate: DateTime.Parse(parts[1]),
					commitDate: DateTime.Parse(parts[2]));

				commits.Add(gitCommit);
			}

			return commits;
		}

		private static string GetSubject(string[] logRowParts)
		{
			try
			{
				string subject = logRowParts[5];
				if (logRowParts.Length > 6)
				{
					// The subject contains one or more "|", so join these parts into original subject
					logRowParts.Skip(5).ForEach(part => subject += "|" + part);
				}
				return subject;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}

		}


		private static string[] GetParentIds(string[] logRowParts)
		{
			return string.IsNullOrEmpty(logRowParts[4]) ? NoParents : logRowParts[4].Split(IdSplitter);
		}

	}
}