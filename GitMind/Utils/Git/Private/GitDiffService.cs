using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitDiffService : IGitDiffService
	{
		private readonly IGitCmdService gitCmdService;
		private readonly IWorkingFolder workingFolder;


		public GitDiffService(IGitCmdService gitCmdService, IWorkingFolder workingFolder)
		{
			this.gitCmdService = gitCmdService;
			this.workingFolder = workingFolder;
		}


		public async Task<R<IReadOnlyList<GitFile>>> GetFilesAsync(
			string sha, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(
				$"diff-tree --no-commit-id --name-status -r --find-renames -m --root {sha}", ct);

			if (result.IsFaulted)
			{
				return R.Error($"Failed to get list of commit files for {sha}", result.Exception);
			}

			IReadOnlyList<GitFile> files = ParseCommitFiles(result.Value);
			Log.Info($"Got {files.Count} for {sha}");
			return R.From(files);
		}


		//public async Task<R<string>> GetCommitDiffAsync(string sha, CancellationToken ct)
		//{
		//	R<CmdResult2> result = await gitCmdService.RunAsync(
		//		$"show --patch --root {sha}", ct);

		//	if (result.IsFaulted)
		//	{
		//		return Error.From($"Failed to get commit diff for {sha}", result);
		//	}

		//	Log.Info($"Got path for {sha}");
		//	return R.From(result.Value.Output);
		//}


		public async Task<R<string>> GetCommitDiffAsync(string sha, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"diff --patch --find-renames --unified=6 --root {sha}^..{sha}", ct);

			if (result.IsFaulted)
			{
				if (result.Error.StartsWith("fatal: ambiguous argument"))
				{
					// Failed to get diff for sha, might be root commit, so try again
					CmdResult2 showRootResult = await gitCmdService.RunCmdAsync(
						$"show --patch --root {sha}", ct);
					if (showRootResult.IsOk)
					{
						Log.Info($"Got diff patch for root {sha}");
						return R.From(showRootResult.Output);
					}
				}

				return R.Error($"Failed to get commit diff for {sha}", result.AsException());
			}

			Log.Info($"Got diff patch for {sha}");
			return R.From(result.Output);
		}


		public async Task<R<string>> GetCommitDiffRangeAsync(string sha1, string sha2, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(
				$"diff --patch --unified=6 {sha1} {sha2}", ct);

			if (result.IsFaulted)
			{
				return R.Error($"Failed to get commit diff for {sha1}..{sha2}", result.Exception);
			}

			Log.Info($"Got path for {sha1}..{sha2}");
			return R.From(result.Value.Output);
		}


		public async Task<R<string>> GetFileDiffAsync(string sha, string path, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"diff --patch --root --find-renames --unified=100000  {sha}^..{sha} -- \"{path}\" ", ct);

			if (result.IsFaulted)
			{
				if (result.Error.StartsWith("fatal: ambiguous argument"))
				{
					// Failed to get diff for sha, might be root commit, so try again
					CmdResult2 showRootResult = await gitCmdService.RunCmdAsync(
						$"show --patch --root {sha} -- \"{path}\"", ct);
					if (showRootResult.IsOk)
					{
						Log.Info($"Got file diff patch for root {sha}");
						return R.From(showRootResult.Output);
					}
				}

				return R.Error($"Failed to get file diff for {sha}", result.AsException());
			}

			Log.Info($"Got path for {sha}");
			return R.From(result.Output);
		}


		public async Task<R<string>> GetPreviewMergeDiffAsync(string sha1, string sha2, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(
				$"diff --find-renames --unified=6 --full-index {sha1} {sha2}", ct);

			if (result.IsFaulted)
			{
				return R.Error($"Failed to get merge diff for {sha1} and {sha2}", result.Exception);
			}

			Log.Info($"Got merge diff for {sha1} and {sha2}");
			return R.From(result.Value.Output);

		}


		public async Task<R<string>> GetUncommittedDiffAsync(CancellationToken ct)
		{
			bool isMergeInProgress = IsMergeInProgress();

			if (!isMergeInProgress)
			{
				R<CmdResult2> addResult = await gitCmdService.RunAsync("add .", ct);
				if (addResult.IsFaulted)
				{
					return R.Error("Failed to get uncommitted diff", addResult.Exception);
				}
			}

			R<CmdResult2> diffResult = await gitCmdService.RunAsync(
				$"diff --find-renames --unified=6 --cached", ct);
			if (diffResult.IsFaulted)
			{
				return R.Error("Failed to get uncommitted diff", diffResult.Exception);
			}

			if (!isMergeInProgress)
			{
				// Not resetting if merge is in progress, since that will abort the merge
				R<CmdResult2> resetResult = await gitCmdService.RunAsync("reset", ct);
				if (resetResult.IsFaulted)
				{
					return R.Error("Failed to get uncommitted diff", resetResult.Exception);
				}
			}

			Log.Info("Got uncommitted diff");
			return R.From(diffResult.Value.Output);
		}


		public async Task<R<string>> GetUncommittedFileDiffAsync(string path, CancellationToken ct)
		{
			bool isMergeInProgress = IsMergeInProgress();
			if (!isMergeInProgress)
			{
				R<CmdResult2> addResult = await gitCmdService.RunAsync("add .", ct);
				if (addResult.IsFaulted)
				{
					return R.Error("Failed to get uncommitted diff", addResult.Exception);
				}
			}

			R<CmdResult2> diffResult = await gitCmdService.RunAsync(
				$"diff --find-renames --unified=100000 HEAD -- \"{path}\"", ct);
			if (diffResult.IsFaulted)
			{
				return R.Error("Failed to get uncommitted diff", diffResult.Exception);
			}

			if (!isMergeInProgress)
			{
				R<CmdResult2> resetResult = await gitCmdService.RunAsync("reset", ct);
				if (resetResult.IsFaulted)
				{
					return R.Error("Failed to get uncommitted diff", resetResult.Exception);
				}
			}

			Log.Info("Got uncommitted diff");
			return R.From(diffResult.Value.Output);
		}


		private IReadOnlyList<GitFile> ParseCommitFiles(CmdResult2 result)
		{
			List<GitFile> files = new List<GitFile>();
			string folder = result.WorkingDirectory;

			foreach (string line in result.OutputLines)
			{
				string[] parts = line.Trim().Split("\t".ToCharArray());
				string status = parts[0];
				string filePath = parts[1].Trim();
				string newFilePath = parts.Length > 2 ? parts[2].Trim() : null;

				if (status.StartsWith("A") || status.StartsWith("C"))
				{
					files.Add(new GitFile(folder, filePath, null, GitFileStatus.Added));
				}
				else if (status.StartsWith("D"))
				{
					files.Add(new GitFile(folder, filePath, null, GitFileStatus.Deleted));
				}
				else if (status.StartsWith("R100"))
				{
					files.Add(new GitFile(folder, newFilePath, filePath, GitFileStatus.Renamed));
				}
				else if (status.StartsWith("R"))
				{
					files.Add(new GitFile(folder, newFilePath, filePath, GitFileStatus.Renamed | GitFileStatus.Modified));
				}
				else
				{
					files.Add(new GitFile(folder, filePath, null, GitFileStatus.Modified));
				}
			}

			return files;
		}


		private bool IsMergeInProgress()
		{
			string mergeIpPath = Path.Combine(workingFolder.Path, ".git", "MERGE_HEAD");
			bool isMergeInProgress = File.Exists(mergeIpPath);
			return isMergeInProgress;
		}
	}
}