﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitStatusService2 : IGitStatusService2
	{
		private static readonly string StatusArgs = "status -s --porcelain --ahead-behind --untracked-files=all";

		private readonly IGitCmdService gitCmdService;


		public GitStatusService2(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R<Status2>> GetStatusAsync(CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(StatusArgs, ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to get status", result);
			}

			Status2 status = ParseStatus(result.Value);
			Log.Info($"Status: {status} in {result.Value.WorkingDirectory}");
			return status;
		}


		private Status2 ParseStatus(CmdResult2 result)
		{
			IReadOnlyList<GitFile2> files = ParseFiles(result);

			int added = files.Count(file => file.Status.HasFlag(GitFileStatus.Added));
			int deleted = files.Count(file => file.Status.HasFlag(GitFileStatus.Deleted));
			int conflicted = files.Count(file => file.Status.HasFlag(GitFileStatus.Conflict));
			int modified = files.Count - (added + deleted + conflicted);

			return new Status2(modified, added, deleted, conflicted, files);
		}


		private IReadOnlyList<GitFile2> ParseFiles(CmdResult2 result)
		{
			List<GitFile2> files = new List<GitFile2>();

			foreach (string line in result.OutputLines)
			{
				string filePath = line.Substring(2).Trim();

				GitFileStatus status = GitFileStatus.Modified;

				if (line.StartsWith("DD ") ||
						line.StartsWith("AU ") ||
						line.StartsWith("UD ") ||
						line.StartsWith("UA ") ||
						line.StartsWith("DU ") ||
						line.StartsWith("AA ") ||
						line.StartsWith("UU "))
				{
					status = GitFileStatus.Conflict;
				}
				else if (line.StartsWith("AU "))
				{

				}
				else if (line.StartsWith("?? ") || line.StartsWith(" A "))
				{
					status = GitFileStatus.Added;
				}
				else if (line.StartsWith(" D ") || line.StartsWith("D"))
				{
					status = GitFileStatus.Deleted;
				}

				files.Add(new GitFile2(result.WorkingDirectory, filePath, null, status));
			}

			return files;
		}
	}
}