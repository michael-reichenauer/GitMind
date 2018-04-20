using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitDiffService2 : IGitDiffService2
	{
		private readonly IGitCmdService gitCmdService;


		public GitDiffService2(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R<IReadOnlyList<GitFile2>>> GetFilesAsync(
			string sha, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(
				$"diff-tree --no-commit-id --name-status -r --find-renames -m --root {sha}", ct);

			if (result.IsFaulted)
			{
				return Error.From($"Failed to get list of commit files for {sha}", result);
			}

			IReadOnlyList<GitFile2> files = ParseCommitFiles(result.Value);
			Log.Info($"Got {files.Count} for {sha}");
			return R.From(files);
		}


		public async Task<R<string>> GetCommitDiffAsync(string sha, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(
				$"show --patch --root {sha}", ct);

			if (result.IsFaulted)
			{
				return Error.From($"Failed to get commit diff for {sha}", result);
			}

			Log.Info($"Got path for {sha}");
			return R.From(result.Value.Output);
		}


		public async Task<R<string>> GetCommitDiffRangeAsync(string sha1, string sha2, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(
				$"diff --patch {sha1} {sha2}", ct);

			if (result.IsFaulted)
			{
				return Error.From($"Failed to get commit diff for {sha1}..{sha2}", result);
			}

			Log.Info($"Got path for {sha1}..{sha2}");
			return R.From(result.Value.Output);
		}


		public async Task<R<string>> GetFileDiffAsync(string sha, string path, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(
				$"show --patch --root --find-renames --unified=100000  {sha} -- \"{path}\" ", ct);

			if (result.IsFaulted)
			{
				return Error.From($"Failed to get commit diff for {sha}", result);
			}

			Log.Info($"Got path for {sha}");
			return R.From(result.Value.Output);
		}


		public async Task<R<string>> GetPreviewMergeDiffAsync(string sha1, string sha2, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(
				$"diff --find-renames --full-index {sha1} {sha2}", ct);

			if (result.IsFaulted)
			{
				return Error.From($"Failed to get merge diff for {sha1} and {sha2}", result);
			}

			Log.Info($"Got merge diff for {sha1} and {sha2}");
			return R.From(result.Value.Output);

		}


		public async Task<R<string>> GetUncommittedDiffAsync(CancellationToken ct)
		{
			R<CmdResult2> addResult = await gitCmdService.RunAsync("add .", ct);
			if (addResult.IsFaulted)
			{
				return Error.From("Failed to get uncommitted diff", addResult);
			}

			R<CmdResult2> diffResult = await gitCmdService.RunAsync(
				$"diff --find-renames --cached", ct);
			if (diffResult.IsFaulted)
			{
				return Error.From("Failed to get uncommitted diff", diffResult);
			}

			R<CmdResult2> resetResult = await gitCmdService.RunAsync("reset", ct);
			if (resetResult.IsFaulted)
			{
				return Error.From("Failed to get uncommitted diff", resetResult);
			}

			Log.Info("Got uncommitted diff");
			return R.From(diffResult.Value.Output);
		}


		public async Task<R<string>> GetUncommittedFileDiffAsync(string path, CancellationToken ct)
		{
			R<CmdResult2> addResult = await gitCmdService.RunAsync("add .", ct);
			if (addResult.IsFaulted)
			{
				return Error.From("Failed to get uncommitted diff", addResult);
			}

			R<CmdResult2> diffResult = await gitCmdService.RunAsync(
				$"diff --find-renames --cached --unified=100000 -- \"{path}\"", ct);
			if (diffResult.IsFaulted)
			{
				return Error.From("Failed to get uncommitted diff", diffResult);
			}

			R<CmdResult2> resetResult = await gitCmdService.RunAsync("reset", ct);
			if (resetResult.IsFaulted)
			{
				return Error.From("Failed to get uncommitted diff", resetResult);
			}

			Log.Info("Got uncommitted diff");
			return R.From(diffResult.Value.Output);
		}


		private IReadOnlyList<GitFile2> ParseCommitFiles(CmdResult2 result)
		{
			List<GitFile2> files = new List<GitFile2>();
			string folder = result.WorkingDirectory;

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