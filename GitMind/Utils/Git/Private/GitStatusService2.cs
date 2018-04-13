using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitStatusService2 : IGitStatusService2
	{
		private static readonly string StatusArgs = "status -s --porcelain --ahead-behind --untracked-files=all";

		private static readonly Regex CleanOutputRegEx = new Regex(@"warning: failed to remove ([^:]+):",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

		private static readonly IReadOnlyList<string> EmptyFileList = new string[0].AsReadOnlyList();


		private readonly IGitCmdService gitCmdService;


		public GitStatusService2(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R<GitStatus2>> GetStatusAsync(CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(StatusArgs, ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to get status", result);
			}

			GitStatus2 status = ParseStatus(result.Value);
			Log.Info($"Status: {status} in {result.Value.WorkingDirectory}");
			return status;
		}



		public async Task<R<GitConflicts>> GetConflictsAsync(CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync("ls-files -u", ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to get status", result);
			}

			GitConflicts conflicts = ParseConflicts(result.Value);
			Log.Info($"Conflicts: {conflicts} in {result.Value.WorkingDirectory}");
			return conflicts;
		}


		public async Task<R<string>> GetConflictFile(string fileId, CancellationToken ct)
		{
			//R<CmdResult2> result = await gitCmdService.RunAsync($"show --format=raw {fileId}", ct);
			R<CmdResult2> result = await gitCmdService.RunAsync($"cat-file blob {fileId}", ct);

			if (result.IsFaulted)
			{
				return Error.From($"Failed to get file {fileId}", result);
			}

			string file = result.Value.Output;
			if (file.EndsWith("\r\n"))
			{
				file = file.Substring(0, file.Length - 2);
			}

			Log.Info($"Got file: {fileId} {file.Length} bytes");
			return file;
		}


		public async Task<R<IReadOnlyList<string>>> UndoAllUncommitedAsync(CancellationToken ct)
		{
			R<IReadOnlyList<string>> result = await UndoAndCleanFolderAsync("-fd", ct);
			if (result.IsFaulted)
			{
				return Error.From("Failed to undo uncommited changes", result);
			}

			Log.Info("Undid uncommited changes");
			return result;
		}


		public async Task<R> UndoUncommitedFileAsync(
			string path, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"checkout --force -- \"{path}\"", CancellationToken.None);

			if (result.IsFaulted)
			{
				if (IsFileUnkwon(result, path))
				{
					R deleteResult = DeleteFile(path, result);
					if (deleteResult.IsFaulted)
					{
						return Error.From($"Failed to delete {path}", deleteResult);
					}

					Log.Info($"Undid file {path}");
					return R.Ok;
				}

				return Error.From($"Failed to undo file {path}",
					Error.From(string.Join("\n", result.ErrorLines.Take(10)), new GitException($"{result}")));
			}

			Log.Info($"Undid file {path}");
			return R.Ok;
		}


		public async Task<R<IReadOnlyList<string>>> GetRefsIdsAsync(CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync("show-ref", ct);
			if (result.IsFaulted)
			{
				return Error.From("Failed to get ll refs", result);
			}

			IReadOnlyList<string> refs = result.Value.OutputLines.ToList();
			Log.Info($"Got {refs.Count} refs");
			return R.From(refs);
		}


		public async Task<R<IReadOnlyList<string>>> CleanWorkingFolderAsync(CancellationToken ct)
		{
			R<IReadOnlyList<string>> result = await UndoAndCleanFolderAsync("-fxd", ct);
			if (result.IsFaulted)
			{
				return Error.From("Failed to clean working folder", result);
			}

			Log.Info("Cleaned working folder");
			return result;
		}


		private static R DeleteFile(string path, CmdResult2 result)
		{
			try
			{
				string fullPath = Path.Combine(result.WorkingDirectory, path);
				if (File.Exists(fullPath))
				{
					File.Delete(fullPath);
					Log.Debug($"Deleted {fullPath}");
				}

				return R.Ok;
			}
			catch (Exception e)
			{
				return e;
			}
		}


		private static bool IsFileUnkwon(CmdResult2 result, string path) =>
			result.Error.StartsWith($"error: pathspec '{path}' did not match any file(s) known to git.");


		private async Task<R<IReadOnlyList<string>>> UndoAndCleanFolderAsync(
			string cleanArgs, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync("reset --hard", ct);
			if (result.IsFaulted)
			{
				return Error.From("Reset failed.", result);
			}

			CmdResult2 cleanResult = await gitCmdService.RunCmdAsync($"clean {cleanArgs}", ct);
			if (cleanResult.IsFaulted)
			{
				if (IsFailedToRemoveSomeFiles(cleanResult, out IReadOnlyList<string> failedFiles))
				{
					Log.Warn($"Failed to clean {failedFiles.Count} files");
					return R.From(failedFiles);
				}

				return Error.From(cleanResult.ToString());
			}

			return R.From(EmptyFileList);
		}

		private GitConflicts ParseConflicts(CmdResult2 result)
		{
			List<GitConflictFile> files = new List<GitConflictFile>();

			string filePath = null;
			string baseId = null;
			string localId = null;
			string remoteId = null;

			// Parsing lines, where there are 1,2 or 3 lines for one file before the next file lines
			foreach (string line in result.OutputLines)
			{
				string[] parts1 = line.Split("\t".ToCharArray());
				string[] parts2 = parts1[0].Split(" ".ToCharArray());
				string path = parts1[1].Trim();

				if (path != filePath && filePath != null)
				{
					// Next file, store previous file
					GitFileStatus status = GetConflictStatus(baseId, localId, remoteId);
					files.Add(new GitConflictFile(result.WorkingDirectory, filePath, baseId, localId, remoteId, status));
					baseId = null;
					localId = null;
					remoteId = null;
				}

				filePath = path;
				SetIds(parts2, ref baseId, ref localId, ref remoteId);
			}

			if (filePath != null)
			{
				// Add last file
				GitFileStatus status = GetConflictStatus(baseId, localId, remoteId);
				files.Add(new GitConflictFile(result.WorkingDirectory, filePath, baseId, localId, remoteId, status));
			}

			return new GitConflicts(files);
		}


		private static void SetIds(
			string[] parts, ref string baseId, ref string localId, ref string remoteId)
		{
			string id = parts[1].Trim();
			string type = parts[2].Trim();

			if (type == "1")
			{
				baseId = id;
			}
			else if (type == "2")
			{
				localId = id;
			}
			else
			{
				remoteId = id;
			}
		}


		private GitFileStatus GetConflictStatus(string baseId, string localId, string remoteId)
		{
			if (baseId != null && localId != null && remoteId != null)
			{
				return GitFileStatus.Conflict | GitFileStatus.ConflictMM;
			}
			else if (baseId != null && localId != null)
			{
				return GitFileStatus.Conflict | GitFileStatus.ConflictMD;
			}
			else if (baseId != null && remoteId != null)
			{
				return GitFileStatus.Conflict | GitFileStatus.ConflictDM;
			}
			else if (localId != null && remoteId != null)
			{
				return GitFileStatus.Conflict | GitFileStatus.ConflictAA;
			}

			throw new InvalidOperationException("Unexpected state");
		}


		private GitStatus2 ParseStatus(CmdResult2 result)
		{
			IReadOnlyList<GitFile2> files = ParseFiles(result);

			int added = files.Count(file => file.Status.HasFlag(GitFileStatus.Added));
			int deleted = files.Count(file => file.Status.HasFlag(GitFileStatus.Deleted));
			int conflicted = files.Count(file => file.Status.HasFlag(GitFileStatus.Conflict));
			int modified = files.Count - (added + deleted + conflicted);

			bool isMergeInProgress = GetMergeStatus(result, out string mergeMessage);

			return new GitStatus2(
				modified, added, deleted, conflicted, isMergeInProgress, mergeMessage, files);
		}


		private static bool GetMergeStatus(CmdResult2 result, out string mergeMessage)
		{
			bool isMergeInProgress = false;
			mergeMessage = null;
			string mergeIpPath = Path.Combine(result.WorkingDirectory, ".git", "MERGE_HEAD");
			string mergeMsgPath = Path.Combine(result.WorkingDirectory, ".git", "MERGE_MSG");
			if (File.Exists(mergeIpPath))
			{
				isMergeInProgress = true;
				mergeMessage = File.ReadAllText(mergeMsgPath).Trim();
			}

			return isMergeInProgress;
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
						line.StartsWith("UA "))
				{
					// How to do reproduce this ???
					status = GitFileStatus.Conflict;
				}
				else if (line.StartsWith("UU "))
				{
					status = GitFileStatus.Conflict | GitFileStatus.ConflictMM;
				}
				else if (line.StartsWith("AA "))
				{
					status = GitFileStatus.Conflict | GitFileStatus.ConflictAA;
				}
				else if (line.StartsWith("UD "))
				{
					status = GitFileStatus.Conflict | GitFileStatus.ConflictMD;
				}
				else if (line.StartsWith("DU "))
				{
					status = GitFileStatus.Conflict | GitFileStatus.ConflictDM;
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

		private static bool IsFailedToRemoveSomeFiles(CmdResult2 result, out IReadOnlyList<string> failedFiles)
		{
			// Check if error message contains any "warning: failed to remove <file>:"
			failedFiles = CleanOutputRegEx.Matches(result.Error).OfType<Match>()
				.Select(match => match.Groups[1].Value).ToList();

			return failedFiles.Any();
		}
	}
}