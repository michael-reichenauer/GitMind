using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Git;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitDiffService2 : IGitDiffService2
	{
		private readonly IGitCmdService gitCmdService;
		private readonly WorkingFolderPath workingFolder;


		public GitDiffService2(IGitCmdService gitCmdService, WorkingFolderPath workingFolder)
		{
			this.gitCmdService = gitCmdService;
			this.workingFolder = workingFolder;
		}
		public async Task<R<IReadOnlyList<GitFile2>>> GetFilesAsync(
			string sha, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(
				$"diff-tree --no-commit-id --name-status -r --find-renames -m --root {sha}", ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to get list of commit files", result);
			}

			return R.From(ParseCommitFiles(result.Value));
		}

		private IReadOnlyList<GitFile2> ParseCommitFiles(CmdResult2 result)
		{
			List<GitFile2> files = new List<GitFile2>();
			string folder = workingFolder;

			foreach (string line in result.OutputLines)
			{
				string[] parts = line.Trim().Split("\t".ToCharArray());
				string status = parts[0];
				string filePath = parts[1].Trim();
				string newFilePath = parts.Length > 2 ? parts[2].Trim() : null;

				if (status.StartsWith("A") || status.StartsWith("C"))
				{
					files.Add(new GitFile2(folder, filePath, null, GitFileStatus.Added));
				}
				else if (status.StartsWith("D"))
				{
					files.Add(new GitFile2(folder, filePath, null, GitFileStatus.Deleted));
				}
				else if (status.StartsWith("R100"))
				{
					files.Add(new GitFile2(folder, newFilePath, filePath, GitFileStatus.Renamed));
				}
				else if (status.StartsWith("R"))
				{
					files.Add(new GitFile2(folder, newFilePath, filePath, GitFileStatus.Renamed | GitFileStatus.Modified));
				}
				else
				{
					files.Add(new GitFile2(folder, filePath, null, GitFileStatus.Modified));
				}
			}

			return files;
		}
	}
}