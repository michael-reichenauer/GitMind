using System;
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
		private static readonly char[] LogRowSplitter = "|".ToCharArray();
		private static readonly string[] NoParents = new string[0];
	

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


		public async Task<R<IGitRepo>> GetRepoAsync(string path)
		{
			//string time = DateTime.Now.ToShortTimeString().Replace(":", "-");
			//string date = DateTime.Now.ToShortDateString().Replace(":", "-");
			Timing t = new Timing();
			R<IReadOnlyList<GitTag>> tags = await GetTagsAsync(path);
			if (tags.IsFaulted) return tags.Error;
			t.Log("Get tags");

			R<IReadOnlyList<GitBranch>> branches = await GetBranchesAsync(path);
			if (branches.IsFaulted) return branches.Error;
			t.Log("Get branches");

			R<IReadOnlyList<GitCommit>> commits = await GetCommitsAsync(path);
			if (commits.IsFaulted) return commits.Error;
			t.Log("Get commits");

			R<GitCommit> currentCommit = await GetCurrentCommitAsync(path, commits.Value);
			if (currentCommit.IsFaulted) return currentCommit.Error;
			t.Log("Get current commit");

			// Getting current branch to be included in stored data
			GitBranch currentBranch = branches.Value.First(b => b.IsCurrent);
			t.Log("Get current branch");

			return new GitRepo(
				branches.Value, commits.Value, tags.Value, currentCommit.Value, currentBranch);
		}


		public async Task<R<string>> GetCurrentBranchNameAsync(string path)
		{
			string args = "rev-parse --abbrev-ref HEAD";

			R<IReadOnlyList<string>> currentBranch = await GitAsync(path, args);
			if (currentBranch.IsFaulted) return currentBranch.Error;

			return currentBranch.Value[0].Trim();
		}


		public R<string> GetCurrentRootPath(string path)
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


		public async Task<R<GitStatus>> GetStatusAsync(string path)
		{
			string args = "status -s";

			R<IReadOnlyList<string>> status = await GitAsync(path, args);
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


		public async Task<R<IReadOnlyList<GitCommitFiles>>> GetCommitsFilesAsync(
			string path, DateTime? dateTime, int max, int skip)
		{

			string args = $"log --all --name-status -m --pretty=\"%H\" --max-count={max} --skip={skip}";
			if (dateTime.HasValue)
			{
				args += " --since=\"" + dateTime.Value.ToString("o") + "\"";
			}

			R<IReadOnlyList<string>> logResult = await GitAsync(path, args);

			if (logResult.IsFaulted) return logResult.Error;

			IReadOnlyList<string> logLines = logResult.Value;

			List<GitCommitFiles> commitsFiles = new List<GitCommitFiles>();

			string commitId = null;
			List<GitFile> files = new List<GitFile>();
			foreach (string line in logLines)
			{
				if (line.StartsWith("M\t", StringComparison.Ordinal))
				{
					files.Add(new GitFile(line.Substring(2), true, false, false));
				}
				else if (line.StartsWith("A\t", StringComparison.Ordinal))
				{
					files.Add(new GitFile(line.Substring(2), false, true, false));
				}
				else if (line.StartsWith("D\t", StringComparison.Ordinal))
				{
					files.Add(new GitFile(line.Substring(2), false, false, true));
				}
				else if (line.Length > 5)
				{
					// A commit id
					if (commitId != null)
					{
						// Got all files in the commit
						commitsFiles.Add(new GitCommitFiles(commitId, files));
						files = new List<GitFile>();
					}

					// Next commit id
					commitId = line.Trim();
				}
			}

			return commitsFiles;
		}


		public async Task<R<CommitDiff>> GetCommitFileDiffAsync(string commitId, string name)
		{
			string args;

			int index = commitId.IndexOf("_");
			if (index > 0)
			{
				commitId = commitId.Substring(0, index);
			}

			args = $"diff --unified=10000 -M {commitId}^ {commitId} {name}";
		

			R<IReadOnlyList<string>> diff = await GitAsync(null, args);
			if (diff.IsFaulted) return diff.Error;

			IReadOnlyList<string> diffLInes = diff.Value;


			R<IReadOnlyList<string>> linesNew = await GetAddFileLinesAsync();
			if (linesNew.IsFaulted) return linesNew.Error;

			diffLInes = diffLInes.Concat(linesNew.Value).ToList();
			

			return await gitDiffParser.ParseAsync(commitId, diffLInes);
		}


		public async Task<R<CommitDiff>> GetCommitDiffAsync(string commitId)
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

			R<IReadOnlyList<string>> diff = await GitAsync(null, args);
			if (diff.IsFaulted) return diff.Error;

			IReadOnlyList<string> diffLInes = diff.Value;

			if (commitId == null)
			{
				R<IReadOnlyList<string>> linesNew = await GetAddFileLinesAsync();
				if (linesNew.IsFaulted) return linesNew.Error;

				diffLInes = diffLInes.Concat(linesNew.Value).ToList();
			}

			return await gitDiffParser.ParseAsync(commitId, diffLInes);
		}


		public async Task FetchAsync(string path)
		{
			string args = "fetch";

			R<IReadOnlyList<string>> fetchResult = await GitAsync(path, args);

			fetchResult.OnError(e =>
			{
				// Git fetch failed, but ignore that for now
				Log.Warn($"Git Fetch failed {e}");
			});
		}



		private async Task<R<IReadOnlyList<string>>> GetAddFileLinesAsync()
		{
			string args = "status -s";

			List<string> addFilesLines = new List<string>();

			R<IReadOnlyList<string>> status = await GitAsync(null, args);
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



		private async Task<R<GitCommit>> GetCurrentCommitAsync(
			string path, IReadOnlyList<GitCommit> commits)
		{
			string args = "rev-parse HEAD";

			R<IReadOnlyList<string>> currentCommit = await GitAsync(path, args);
			if (currentCommit.IsFaulted) return currentCommit.Error;

			string commitId = currentCommit.Value[0].Trim();

			return commits.First(c => c.Id == commitId);
		}


		private async Task<R<IReadOnlyList<GitTag>>> GetTagsAsync(string path)
		{
			List<GitTag> tags = new List<GitTag>();

			string args = "show-ref --tags -d";
			R<IReadOnlyList<string>> showResult = await GitAsync(path, args);
			if (showResult.IsFaulted) return showResult.Error;

			foreach (string line in showResult.Value)
			{
				string commitId = line.Substring(0, 40);
				string tagName = line.Substring(51);
				if (tagName.EndsWith("^{}"))
				{
					// For soem reason some tag names end in strange characters
					tagName = tagName.Substring(0, tagName.Length - 3);
				}

				tags.Add(new GitTag(commitId, tagName));
			}

			return tags;
		}



		private async Task<R<IReadOnlyList<GitBranch>>> GetBranchesAsync(string path)
		{
			List<GitBranch> branches = new List<GitBranch>();

			// Get list of local branches
			string args = "branch -vv --no-color --no-abbrev";
			R<IReadOnlyList<string>> localBranches = await GitAsync(path, args);
			if (localBranches.IsFaulted) return localBranches.Error;

			// Get list of remote branches
			R<IReadOnlyList<string>> remoteBranches = await GitAsync(path, args + " -r");
			if (remoteBranches.IsFaulted) return remoteBranches.Error;

			// Make one list, but prefix a "r" on remote branch lines
			var lines = localBranches.Value
				.Concat(remoteBranches.Value.Select(l => "r " + l));

			foreach (string line in lines)
			{
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
				if (latestCommitId == "->")
				{
					continue;
				}

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

				if (isRemote && branchName.StartsWith(Origin))
				{
					branchName = branchName.Substring(Origin.Length);
				}
	
				GitBranch branch = new GitBranch(
					branchName, latestCommitId, isCurrent, trackingBranchName, isRemote);
				branches.Add(branch);		
			}

			return branches;
		}


		private async Task<R<IReadOnlyList<GitCommit>>> GetCommitsAsync(string path)
		{
			// git log --all --name-status --pretty="%H|%ai|%ci|%an|%P|%s"
			Log.Debug("Getting log ...");
			string args = "log --all --pretty=\"%H|%ai|%ci|%an|%P|%s\"";

			Timing t = new Timing();
			R<IReadOnlyList<string>> logResult = await GitAsync(path, args);
			t.Log("Get commits");

			if (logResult.IsFaulted) return logResult.Error;

			IReadOnlyList<string> logLines = logResult.Value;

			List<GitCommit> commits = new List<GitCommit>(logLines.Count);

			foreach (string line in logLines)
			{
				string[] parts = line.Split(LogRowSplitter);

				if (parts.Length < 6)
				{
					return GitCommandError.With("Unknown log format");
				}
		
				var gitCommit = new GitCommit(
					id: parts[0],
					subject: GetSubject(parts),
					author: parts[3],
					parentIds: GetParentIds(parts),
					authorDate: DateTime.Parse(parts[1]),
					commitDate: DateTime.Parse(parts[2]));

				commits.Add(gitCommit);
			}

			t.Log("Parsing commits");
			return commits;
		}



		private static string[] GetParentIds(string[] logRowParts)
		{
			return string.IsNullOrEmpty(logRowParts[4]) ? NoParents : logRowParts[4].Split(IdSplitter);
		}


		private static string GetSubject(string[] logRowParts)
		{
			string subject = logRowParts[5];
			if (logRowParts.Length > 6)
			{
				// The subject contains one or more "|", so join these parts into original subject
				logRowParts.Skip(5).ForEach(part => subject += "|" + part);
			}
			return subject;
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


		private async Task<R<IReadOnlyList<string>>> GitAsync(
			string path, string args)
		{
			string gitArgs = path != null ? $"--git-dir \"{path}\\.git\" {args}" : args;

			return await Task.Run(() => GitCommand(gitArgs));
		}


		private R<IReadOnlyList<string>> GitCommand(string gitArgs)
		{
			R<string> gitBinPath = GetGitBinPath();
			if (gitBinPath.IsFaulted) return gitBinPath.Error;

			CmdResult result = cmd.Run(gitBinPath.Value, gitArgs);

			if (0 == result.ExitCode || 1 == result.ExitCode)
			{
				return R.From(result.Output);
			}
			else
			{
				Log.Warn($"Error: git {gitArgs}, {result.ExitCode}, {string.Join("\n", result.Error)}");
				return GitCommandError.With(result.ToString());
			}
		}


		private R<string> GetGitBinPath()
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