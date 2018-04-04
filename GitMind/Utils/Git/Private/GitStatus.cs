using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Git;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitStatus : IGitStatus
	{
		private static readonly string StatusArgs = "status -s --porcelain --untracked-files=all";

		private readonly IGitCmd gitCmd;
		private readonly WorkingFolderPath workingFolder;


		public GitStatus(IGitCmd gitCmd, WorkingFolderPath workingFolder)
		{
			this.gitCmd = gitCmd;
			this.workingFolder = workingFolder;
		}


		public async Task<R<Status2>> GetStatusAsync(CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmd.RunAsync(StatusArgs, ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to get status", result);
			}

			Status2 status = ParseStatus(result.Value);
			Log.Info($"Status: {status}");
			return status;
		}


		private Status2 ParseStatus(CmdResult2 result)
		{
			IReadOnlyList<GitFile2> files = ParseFiles(result);

			int added = files.Count(file => file.Status.HasFlag(GitFileStatus.Added));
			int deleted = files.Count(file => file.Status.HasFlag(GitFileStatus.Deleted));
			int modified = files.Count - (added + deleted);

			return new Status2(modified, added, deleted, files);
		}


		private IReadOnlyList<GitFile2> ParseFiles(CmdResult2 result)
		{
			List<GitFile2> files = new List<GitFile2>();

			foreach (string line in result.OutputLines)
			{
				string filePath = line.Substring(2).Trim();

				if (line.StartsWith("?? ") || line.StartsWith(" A "))
				{
					files.Add(new GitFile2(workingFolder, filePath, null, GitFileStatus.Added));
				}
				else if (line.StartsWith(" D ") || line.StartsWith("D"))
				{
					files.Add(new GitFile2(workingFolder, filePath, null, GitFileStatus.Deleted));
				}
				else
				{
					files.Add(new GitFile2(workingFolder, filePath, null, GitFileStatus.Modified));
				}
			}

			return files;
		}
	}
}