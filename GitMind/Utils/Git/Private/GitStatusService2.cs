using System;
using System.Collections.Generic;
using System.IO;
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
	}
}