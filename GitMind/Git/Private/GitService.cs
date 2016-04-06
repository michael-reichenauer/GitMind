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
		private static readonly string cmdPrefix = "d122adb9-1ec3-44f6-826a-e923aef4edc5";
		private static readonly string errorprefix = "d122adb9-1ec3-44f6-826a-e923aef4edc6";
		private static readonly string filePrefix = "C:\\TempGitMind\\gitmind";
		private static readonly string sourceFile = $"{filePrefix}.txt";

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


		public async Task<IGitRepo> GetRepoAsync(string path, bool isShift)
		{
			string time = DateTime.Now.ToShortTimeString().Replace(":", "-");
			string date = DateTime.Now.ToShortDateString().Replace(":", "-");
			string context = $"{filePrefix}_{date}_{time}.txt";

			if (isShift)
			{
				await FetchAsync(path);
			}

			IReadOnlyList<GitTag> tags = await GetTagsAsync(path, context);

			IReadOnlyList<GitBranch> branches = await GetBranchesAsync(path, context);

			IReadOnlyList<GitCommit> commits = await GetCommitsAsync(path, context);

			GitCommit currentCommit = await GetCurrentCommitAsync(path, commits, context);

			// Getting current branch to be included in stored data
			await GetCurrentBranchNameAsync(path, context);

			return new GitRepo(branches, commits, tags, currentCommit);
		}


		public Task<string> GetCurrentBranchNameAsync(string path)
		{
			return GetCurrentBranchNameAsync(path, null);
		}


		public string GetCurrentRootPath(string path)
		{
			string args = "rev-parse --show-toplevel";

			CmdResult result = cmd.Run("git", args);

			if (0 == result.ExitCode)
			{
				return result.Output[0].Trim();
			}
			else
			{
				throw new InvalidDataException($"Git command failed: >git {args}");
			}
		}


		public Task<GitStatus> GetStatusAsync(string path)
		{
			return GetStatusAsync(path, null);
		}


		public async Task<CommitDiff> GetCommitDiffAsync(string commitId)
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

			IReadOnlyList<string> lines = await GitAsync(null, args, null);

			if (commitId == null)
			{
				IReadOnlyList<string> linesNew = await GetAddFileLinesAsync();
				lines = lines.Concat(linesNew).ToList();
			}

			return await gitDiffParser.ParseAsync(commitId, lines);
		}


		private async Task<IReadOnlyList<string>> GetAddFileLinesAsync()
		{
			string args = "status -s";

			List<string> addFilesLines = new List<string>();

			IReadOnlyList<string> lines = await GitAsync(null, args, null);

			foreach (string line in lines)
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


		private async Task FetchAsync(string path)
		{
			try
			{
				string args = "fetch";

				await GitAsync(path, args, null);
			}
			catch (Exception e)
			{
				// Git fetch failed, but ignore that for now
				Log.Warn($"Git Fetch failed {e.Message}");
			}
		}


		private async Task<GitCommit> GetCurrentCommitAsync(
			string path, IReadOnlyList<GitCommit> commits, string context)
		{
			string args = "rev-parse HEAD";

			IReadOnlyList<string> lines = await GitAsync(path, args, context);
			string commitId = lines[0].Trim();

			return commits.First(c => c.Id == commitId);
		}


		private async Task<string> GetCurrentBranchNameAsync(string path, string context)
		{
			string args = "rev-parse --abbrev-ref HEAD";

			IReadOnlyList<string> lines = await GitAsync(path, args, context);

			return lines[0].Trim();
		}


		private async Task<GitStatus> GetStatusAsync(string path, string context)
		{
			string args = "status -s";

			IReadOnlyList<string> lines = await GitAsync(path, args, context);

			int modified = 0;
			int added = 0;
			int deleted = 0;
			int other = 0;

			foreach (string line in lines)
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




		public async Task<IReadOnlyList<GitTag>> GetTagsAsync(string path, string context)
		{
			List<GitTag> tags = new List<GitTag>();

			string args = "show-ref --tags -d";
			IReadOnlyList<string> lines = await GitAsync(path, args, context);

			foreach (string line in lines)
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



		public async Task<IReadOnlyList<GitBranch>> GetBranchesAsync(string path, string context)
		{
			List<GitBranch> branches = new List<GitBranch>();

			// Get list of local branches
			string args = "branch -vv --no-color --no-abbrev";
			IReadOnlyList<string> localLines = await GitAsync(path, args, context);

			// Get list of remote branches
			IReadOnlyList<string> remoteLines = await GitAsync(path, args + " -r", context);

			// Make one list, but prefix a "r" on remote branch lines
			var lines = localLines
				.Concat(remoteLines.Select(l => "r " + l));

			foreach (string line in lines)
			{
				if (-1 != line.IndexOf(" -> "))
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
						// The remote tracking branch contained a ": X Behind ir ": X ahead"
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


		public async Task<IReadOnlyList<GitCommit>> GetCommitsAsync(string path, string context)
		{
			IDictionary<string, string> branchNames = ParseCommitBranchNames(path, context);

			string args = "log --all --pretty=\"%H|%ai|%ci|%an|%P|%s\"";

			IReadOnlyList<string> lines = await GitAsync(path, args, context);

			List<GitCommit> logItems = new List<GitCommit>(lines.Count);

			for (int i = 0; i < lines.Count; i++)
			{
				string[] parts = lines[i].Split(LogSplitter);

				if (parts.Length < 5)
				{
					throw new InvalidDataException("Unknown log format");
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
					Log.Debug($"Commit {shortId} - {branchName}");
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

			return logItems;
		}


		private IDictionary<string, string> ParseCommitBranchNames(string path, string context)
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


		private async Task<IReadOnlyList<string>> GitAsync(
			string path, string args, string context)
		{
			if (!Directory.Exists("C:\\TempGitMind"))
			{
				context = null;
			}

			string gitArgs = path != null ? $"--git-dir \"{path}\\.git\" {args}" : args;

			return await Task.Run(() =>
			{
				if (File.Exists(sourceFile))
				{
					return ReadCommandFromFile(gitArgs);
				}
				else
				{
					if (context != null)
					{
						File.AppendAllText(context, $"{cmdPrefix}:{gitArgs}\n");
					}

					CmdResult result = cmd.Run(GetGitBinPath(), gitArgs);
	
					if (result.ExitCode == 128)
					{
						throw new FileLoadException("No valid git repo found.");
					}

					if (0 == result.ExitCode || 1 == result.ExitCode)
					{
						if (context != null)
						{
							File.AppendAllLines(context, result.Output);
						}

						return result.Output;
					}
					else
					{
						if (context != null)
						{
							File.AppendAllText(context, $"{errorprefix}:{result.ExitCode}\n");
						}
						throw new InvalidDataException(
							$"Git command failed: >git {gitArgs}\nExit code: {result.ExitCode}");
					}
				}
			});
		}


		private static string GetGitBinPath()
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
				// C:\Users\Michael\AppData\Local\GitHub\ncmtfqh2.rws
				// string gitPath = Path.Combine(appdataPath, "GitHub", "ncmtfqh2.rws", "cmd", "git.exe");
				string gitPath = Path.Combine(appdataPath, "Programs", "Git", "cmd", "git.exe");

				if (File.Exists(gitPath))
				{
					return gitPath;
				}
			}

			throw new FileNotFoundException("Git binary not found");
		}


		private IReadOnlyList<string> ReadCommandFromFile(string gitArgs)
		{
			List<string> outputLines = new List<string>();
			Log.Warn($"Reading command 'git {gitArgs}' from file");
			string commandLine = $"{cmdPrefix}:{gitArgs}";
			string[] lines = File.ReadAllLines(sourceFile);
			bool isOutputLine = false;

			for (int i = 0; i < lines.Length; i++)
			{
				if (isOutputLine)
				{
					if (lines[i].StartsWith(cmdPrefix))
					{
						return outputLines;
					}
					else
					{
						outputLines.Add(lines[i]);
					}
				}
				else if (lines[i] == commandLine)
				{
					if (i + 1 < lines.Length && lines[i + 1].StartsWith(errorprefix))
					{
						int exitCode = int.Parse(lines[i + 1].Substring(errorprefix.Length + 1));
						throw new InvalidDataException(
							$"Git command failed: >git {gitArgs}\nExit code: {exitCode}");
					}
					else
					{
						isOutputLine = true;
					}
				}
			}

			return outputLines;
		}


		//private IReadOnlyList<string> Git(string path, string args)
		//{

		//	string gitArgs = path != null ? $"--git-dir \"{path}\\.git\" {args}" : args;

		//	IReadOnlyList<string> lines;
		//	if (0 == cmd.Run("git", gitArgs, out lines))
		//	{
		//		return lines;
		//	}
		//	else
		//	{
		//		throw new InvalidDataException($"Git command failed: >git {gitArgs}");
		//	}
		//}
	}
}