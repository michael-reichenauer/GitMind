using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCommit : IGitCommit
	{
		private readonly IGitCmd gitCmd;
		private readonly WorkingFolderPath workingFolder;


		public GitCommit(IGitCmd gitCmd, WorkingFolderPath workingFolder)
		{
			this.gitCmd = gitCmd;
			this.workingFolder = workingFolder;
		}

		public async Task<R<IReadOnlyList<CommitFile>>> GetCommitFilesAsync(
			string commit, CancellationToken ct)
		{
			CmdResult2 result = await gitCmd.RunAsync(
				$"diff-tree --no-commit-id --name-status -r --find-renames -m --root {commit}", ct);

			if (result.IsFaulted)
			{
				return Error.From(result.Error);
			}

			return R.From(ParseCommitFiles(result));
		}

		private IReadOnlyList<CommitFile> ParseCommitFiles(CmdResult2 result)
		{

			List<CommitFile> files = new List<CommitFile>();
			string folder = workingFolder;

			foreach (string line in result.OutputLines)
			{
				string[] parts = line.Trim().Split("\t".ToCharArray());
				string status = parts[0];
				string filePath = parts[1].Trim();
				string newFilePath = parts.Length > 2 ? parts[2].Trim() : null;

				if (status.StartsWith("A") || status.StartsWith("C"))
				{
					files.Add(new CommitFile(folder, filePath, null, FileStatus.Added));
				}
				else if (status.StartsWith("D"))
				{
					files.Add(new CommitFile(folder, filePath, null, FileStatus.Deleted));
				}
				else if (status.StartsWith("R"))
				{
					files.Add(new CommitFile(folder, newFilePath, filePath, FileStatus.Renamed | FileStatus.Modified));
				}
				else
				{
					files.Add(new CommitFile(folder, filePath, null, FileStatus.Modified));
				}
			}

			return files;
		}
	}
}