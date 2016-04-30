﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitService : IGitService
	{
		private static readonly string LegacyGitPath = "C:\\Program Files (x86)\\Git\\bin\\git.exe";
		private static readonly string GitPath = "C:\\Program Files\\Git\\bin\\git.exe";
		private static readonly string Origin = "origin/";
		private static readonly char[] IdSplitter = " ".ToCharArray();
		private static readonly char[] LogSplitter = "|".ToCharArray();
		private static readonly string[] NoParent = new string[0];
	

		private readonly ICmd cmd;
		private readonly IGitDiffParser gitDiffParser;


		public GitService(ICmd cmd, IGitDiffParser gitDiffParser)
		{
			this.cmd = cmd;
			this.gitDiffParser = gitDiffParser;
		}


		public GitService()
			: this(new Cmd(), new GitDiffParser())
		{
		}


		public Error GitNotInstalledError { get; } = new Error("Compatible git installation not found");
		public Error GitCommandError { get; } = new Error("Git command failed: ");


		public async Task<Result<IGitRepo>> GetRepoAsync(string path)
		{
			//string time = DateTime.Now.ToShortTimeString().Replace(":", "-");
			//string date = DateTime.Now.ToShortDateString().Replace(":", "-");

			Result<IReadOnlyList<GitTag>> tags = await GetTagsAsync(path);
			if (tags.IsFaulted) return tags.Error;

			Result<IReadOnlyList<GitBranch>> branches = await GetBranchesAsync(path);
			if (branches.IsFaulted) return branches.Error;

			Result<IReadOnlyList<GitCommit>> commits = await GetCommitsAsync(path);
			if (commits.IsFaulted) return commits.Error;

			Result<GitCommit> currentCommit = await GetCurrentCommitAsync(path, commits.Value);
			if (currentCommit.IsFaulted) return currentCommit.Error;

			// Getting current branch to be included in stored data
			await GetCurrentBranchNameAsync(path);

			return new GitRepo(branches.Value, commits.Value, tags.Value, currentCommit.Value);
		}


		public async Task<Result<string>> GetCurrentBranchNameAsync(string path)
		{
			string args = "rev-parse --abbrev-ref HEAD";

			Result<IReadOnlyList<string>> currentBranch = await GitAsync(path, args);
			if (currentBranch.IsFaulted) return currentBranch.Error;

			return currentBranch.Value[0].Trim();
		}


		public Result<string> GetCurrentRootPath(string path)
		{
			string args = "rev-parse --show-toplevel";

			CmdResult result = cmd.Run("git", args);

			if (0 == result.ExitCode)
			{
				return result.Output[0].Trim();
			}
			else
			{
				return GitCommandError.With(result.ToString());
			}
		}


		public async Task<Result<GitStatus>> GetStatusAsync(string path)
		{
			string args = "status -s";

			Result<IReadOnlyList<string>> status = await GitAsync(path, args);
			if (status.IsFaulted) return status.Error;

			int modified = 0;
			int added = 0;
			int deleted = 0;
			int other = 0;

			foreach (string line in status.Value)
			{
				if (line.StartsWith(" M "))
				{
					modified++;
				}
				else if (line.StartsWith("?? "))
				{
					added++;
				}
				else if (line.StartsWith(" D "))
				{
					deleted++;
				}
				else
				{
					other++;
				}
			}

			return new GitStatus(modified, added, deleted, other);
		}


		public async Task<Result<CommitDiff>> GetCommitDiffAsync(string commitId)
		{
			string args;
			if (commitId != null)
			{
				int index = commitId.IndexOf("_");
				if (index > 0)
				{
					commitId = commitId.Substring(0, index);
				}

				args = $"diff --unified=5 -M {commitId}^ {commitId}";
			}
			else
			{
				args = $"diff --unified=5 -M";
			}

			Result<IReadOnlyList<string>> diff = await GitAsync(null, args);
			if (diff.IsFaulted) return diff.Error;

			IReadOnlyList<string> diffLInes = diff.Value;

			if (commitId == null)
			{
				Result<IReadOnlyList<string>> linesNew = await GetAddFileLinesAsync();
				if (linesNew.IsFaulted) return linesNew.Error;

				diffLInes = diffLInes.Concat(linesNew.Value).ToList();
			}

			return await gitDiffParser.ParseAsync(commitId, diffLInes);
		}


		public async Task FetchAsync(string path)
		{
			string args = "fetch";

			Result<IReadOnlyList<string>> fetchResult = await GitAsync(path, args);

			fetchResult.OnError(e =>
			{
				// Git fetch failed, but ignore that for now
				Log.Warn($"Git Fetch failed {e}");
			});
		}



		private async Task<Result<IReadOnlyList<string>>> GetAddFileLinesAsync()
		{
			string args = "status -s";

			List<string> addFilesLines = new List<string>();

			Result<IReadOnlyList<string>> status = await GitAsync(null, args);
			if (status.IsFaulted) return status.Error;

			foreach (string line in status.Value)
			{
				if (line.StartsWith("?? ") && line.EndsWith("/"))
				{
					string directoryPath = line.Substring(3).Trim();
					AddDirectory(directoryPath, addFilesLines);
				}
				else if (line.StartsWith("?? "))
				{
					string filePath = line.Substring(3).Trim();

					AddFile(filePath, addFilesLines);
				}
			}

			return addFilesLines;
		}


		private void AddDirectory(string directoryPath, List<string> addFilesLines)
		{
			if (Directory.Exists(directoryPath))
			{
				IEnumerable<string> files = Directory.EnumerateFiles(directoryPath);
				foreach (string path in files)
				{
					AddFile(path, addFilesLines);
				}

				IEnumerable<string> directories = Directory.EnumerateDirectories(directoryPath);

				foreach (string path in directories)
				{
					AddDirectory(path, addFilesLines);
				}
			}
		}


		private static void AddFile(string filePath, List<string> addFilesLines)
		{
			string[] allLines = File.ReadAllLines(filePath);

			addFilesLines.Add("diff");
			addFilesLines.Add("--- /dev/null");
			addFilesLines.Add("+++ a/" + filePath);
			addFilesLines.Add("@@ ");

			foreach (string fileLine in allLines)
			{
				addFilesLines.Add("+" + fileLine);
			}
		}



		private async Task<Result<GitCommit>> GetCurrentCommitAsync(
			string path, IReadOnlyList<GitCommit> commits)
		{
			string args = "rev-parse HEAD";

			Result<IReadOnlyList<string>> currentCommit = await GitAsync(path, args);
			if (currentCommit.IsFaulted) return currentCommit.Error;

			string commitId = currentCommit.Value[0].Trim();

			return commits.First(c => c.Id == commitId);
		}




		public async Task<Result<IReadOnlyList<GitTag>>> GetTagsAsync(string path)
		{
			List<GitTag> tags = new List<GitTag>();

			string args = "show-ref --tags -d";
			Result<IReadOnlyList<string>> showResult = await GitAsync(path, args);
			if (showResult.IsFaulted) return showResult.Error;

			foreach (string line in showResult.Value)
			{
				string commitId = line.Substring(0, 40);
				string tagName = line.Substring(51);
				if (tagName.EndsWith("^{}"))
				{
					tagName = tagName.Substring(0, tagName.Length - 3);
				}

				tags.Add(new GitTag(commitId, tagName));
			}

			return tags;
		}



		public async Task<Result<IReadOnlyList<GitBranch>>> GetBranchesAsync(string path)
		{
			List<GitBranch> branches = new List<GitBranch>();

			// Get list of local branches
			string args = "branch -vv --no-color --no-abbrev";
			Result<IReadOnlyList<string>> localBranches = await GitAsync(path, args);
			if (localBranches.IsFaulted) return localBranches.Error;

			// Get list of remote branches
			Result<IReadOnlyList<string>> remoteBranches = await GitAsync(path, args + " -r");
			if (remoteBranches.IsFaulted) return remoteBranches.Error;

			// Make one list, but prefix a "r" on remote branch lines
			var lines = localBranches.Value
				.Concat(remoteBranches.Value.Select(l => "r " + l));

			foreach (string line in lines)
			{
				int refIndex = line.IndexOf(" -> ");
				int refIndex2 = line.IndexOf(" ");
				if (refIndex > 1 && refIndex == refIndex2)
				{
					continue;
				}

				// Check if first column marks the branch as current or remote
				bool isCurrent = false;
				bool isRemote = false;
				if (line.StartsWith("*"))
				{
					isCurrent = true;
				}
				else if (line.StartsWith("r"))
				{
					isRemote = true;
				}

				// Skip current and remote marker column
				string newLine = line.Substring(1).Trim();

				// Parse branch name and skip to next column
				int index = newLine.IndexOf(" ");
				string branchName = newLine.Substring(0, index);
				newLine = newLine.Substring(index).Trim();

				// Parse latest commit id and skip to next column
				index = newLine.IndexOf(" ");
				string latestCommitId = newLine.Substring(0, index);
				newLine = newLine.Substring(index).Trim();

				// Try to parse remote tracking branch
				string trackingBranchName = null;
				if (!isRemote && newLine.StartsWith("["))
				{
					int index2 = newLine.IndexOf(":");
					index = newLine.IndexOf("]");
					if (index2 > -1 && index2 < index)
					{
						// The remote tracking branch contained a ": X Behind or ": X ahead"
						index = index2;
					}

					trackingBranchName = newLine.Substring(1, index - 1);
				}

				if (!isRemote)
				{
					// Is local branch, which might have a tracking branch, if so this item will be updated
					// once the corresponding remote branch is reached
					GitBranch branch = new GitBranch(
						branchName, latestCommitId, isCurrent, trackingBranchName, null, false, false);
					branches.Add(branch);
				}
				else
				{
					// This is a remote branch, lets check if a corresponding local branch is tracking
					GitBranch branch = branches.FirstOrDefault(b => b.TrackingBranchName == branchName);
					if (branch != null)
					{
						// There is a local branch tracking this remote branch, update the local branch info
						// With info about the remote tracking branch name and latest commit id
						branches.Remove(branch);
						branch = new GitBranch(
							branch.Name, branch.LatestCommitId, branch.IsCurrent, branchName, latestCommitId, false, false);
					}
					else
					{
						// A remote branch with no local branch tracking it
						if (branchName.StartsWith(Origin))
						{
							branchName = branchName.Substring(Origin.Length);
						}

						branch = new GitBranch(branchName, latestCommitId, false, null, null, true, false);
					}

					branches.Add(branch);
				}
			}

			return branches;
		}


		public async Task<Result<IReadOnlyList<GitCommit>>> GetCommitsAsync(string path)
		{
			IDictionary<string, string> branchNames = ParseCommitBranchNames(path);

			string args = "log --all --pretty=\"%H|%ai|%ci|%an|%P|%s\"";

			Log.Debug("Getting log ...");
			Timestamp time = new Timestamp();
			Result<IReadOnlyList<string>> logResult = await GitAsync(path, args);
			Log.Error($"Time for log {time}");

			if (logResult.IsFaulted) return logResult.Error;

			IReadOnlyList<string> lines = logResult.Value;

			List<GitCommit> logItems = new List<GitCommit>(lines.Count);

			for (int i = 0; i < lines.Count; i++)
			{
				string[] parts = lines[i].Split(LogSplitter);

				if (parts.Length < 5)
				{
					return GitCommandError.With("Unknown log format");
				}

				string subject;
				if (parts.Length == 5)
				{
					subject = parts[5];
				}
				else
				{
					subject = string.Join("|", parts.Skip(5));
				}

				string CommitId = parts[0];
				string shortId = CommitId.Substring(0, 6);

				string branchName = null;
				if (branchNames.TryGetValue(shortId, out branchName))
				{
					// Log.Debug($"Commit {shortId} - {branchName}");
				}

				var gitCommit = new GitCommit(
					CommitId,
					subject,
					parts[3],
					string.IsNullOrEmpty(parts[4]) ? NoParent : parts[4].Split(IdSplitter),
					DateTime.Parse(parts[1]),
					DateTime.Parse(parts[2]),
					branchName);

				logItems.Add(gitCommit);
			}

			Log.Debug($"Parsed {time} ({time.DiffMs})");
			return logItems;
		}


		private IDictionary<string, string> ParseCommitBranchNames(string path)
		{
			Dictionary<string, string> branchNames = new Dictionary<string, string>();

			path = path ?? Environment.CurrentDirectory;
			string filePath = path + "\\.gitmind";
			if (File.Exists(filePath))
			{
				string[] lines = File.ReadAllLines(filePath);
				foreach (string line in lines)
				{
					string[] parts = line.Split(" ".ToCharArray());
					branchNames[parts[0]] = parts[1];
				}
			}

			return branchNames;
		}


		private async Task<Result<IReadOnlyList<string>>> GitAsync(
			string path, string args)
		{
			string gitArgs = path != null ? $"--git-dir \"{path}\\.git\" {args}" : args;

			return await Task.Run(() => GitCommand(gitArgs));
		}


		private Result<IReadOnlyList<string>> GitCommand(string gitArgs)
		{
			Result<string> gitBinPath = GetGitBinPath();
			if (gitBinPath.IsFaulted) return gitBinPath.Error;

			CmdResult result = cmd.Run(gitBinPath.Value, gitArgs);

			if (0 == result.ExitCode || 1 == result.ExitCode)
			{
				return Result.From(result.Output);
			}
			else
			{
				Log.Warn($"Error: git {gitArgs}, {result.ExitCode}, {string.Join("\n", result.Error)}");
				return GitCommandError.With(result.ToString());
			}
		}


		private Result<string> GetGitBinPath()
		{
			if (File.Exists(GitPath))
			{
				return GitPath;
			}
			else if (File.Exists(LegacyGitPath))
			{
				return LegacyGitPath;
			}
			else
			{
				string appdataPath = Environment.GetFolderPath(
					Environment.SpecialFolder.LocalApplicationData);
			
				string gitPath = Path.Combine(appdataPath, "Programs", "Git", "cmd", "git.exe");

				if (File.Exists(gitPath))
				{
					return gitPath;
				}
			}

			return GitNotInstalledError.With("Git binary not found");
		}
	}
}