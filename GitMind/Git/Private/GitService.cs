using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitService : IGitService
	{
		private static readonly string LegacyGitPath = @"C:\Program Files (x86)\Git\bin\git.exe";
		private static readonly string GitPath = @"C:\Program Files\Git\bin\git.exe";


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



		public GitRepository OpenRepository(string workingFolder)
		{
			return new GitRepository(new LibGit2Sharp.Repository(workingFolder));
		}


		public Task<R<string>> GetCurrentBranchNameAsync(string workingFolder)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						return R.From(gitRepository.Head.Name);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to get current branch name, {e.Message}");
					return Error.From(e);
				}
			});
		}


		public R<string> GetCurrentRootPath(string path)
		{
			try
			{
				while (!string.IsNullOrEmpty(path))
				{
					if (LibGit2Sharp.Repository.IsValid(path))
					{
						return path;
					}

					path = Path.GetDirectoryName(path);
				}

				return Error.From("No working folder");
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to get working folder, {e.Message}");
				return Error.From(e);
			}
		}


		public Task<R<GitStatus>> GetStatusAsync(string workingFolder)
		{

			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						return R.From(gitRepository.Status);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to get current branch name, {e.Message}");
					return Error.From(e);
				}
			});
		}




		public Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						return R.From(gitRepository.Diff.GetFiles(commitId));
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to get diff, {e.Message}");
					return Error.From(e);
				}
			});
		}


		public Task SetSpecifiedCommitBranchAsync(
			string commitId, string branchName, string gitRepositoryPath)
		{
			try
			{
				string file = Path.Combine(gitRepositoryPath, "gitmind.specified");
				File.AppendAllText(file, $"{commitId} {branchName}\n");
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to add specified branch name for {commitId} {branchName}, {e}");
			}

			return Task.FromResult(true);
		}


		public Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string name)
		{
			return Task.Run(async () =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						string patch = gitRepository.Diff.GetFilePatch(commitId, name);

						return R.From(await gitDiffParser.ParseAsync(commitId, patch));
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to get diff, {e.Message}");
					return Error.From(e);
				}
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffAsync(string workingFolder, string commitId)
		{

			return Task.Run(async () =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						string patch;
						if (commitId == null)
						{
							patch = gitRepository.Diff.GetPatch();
						}
						else
						{
							patch = gitRepository.Diff.GetPatch(commitId);
						}

						return R.From(await gitDiffParser.ParseAsync(commitId, patch));
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to get diff, {e.Message}");
					return Error.From(e);
				}
			});
		}


		public async Task FetchAsync(string path)
		{
			Log.Debug("Fetching repository ...");
			string args = "fetch";

			R<IReadOnlyList<string>> fetchResult = await GitAsync(path, args);
			Log.Debug("Fetched repository");

			fetchResult.OnError(e =>
			{
				// Git fetch failed, but ignore that for now
				Log.Warn($"Git Fetch failed {e}");
			});
		}


		public IReadOnlyList<GitSpecifiedNames> GetSpecifiedNames(string gitRepositoryPath)
		{
			List<GitSpecifiedNames> branchNames = new List<GitSpecifiedNames>();

			try
			{
				string filePath = Path.Combine(gitRepositoryPath, "gitmind.specified");
				if (File.Exists(filePath))
				{
					string[] lines = File.ReadAllLines(filePath);
					foreach (string line in lines)
					{
						string[] parts = line.Split(" ".ToCharArray());
						branchNames.Add(new GitSpecifiedNames(parts[0], parts[1]?.Trim()));
					}
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to read specified names {e}");
			}

			return branchNames;
		}


		private async Task<R<IReadOnlyList<string>>> GitAsync(
			string gitRepositoryPath, string args)
		{
			string gitArgs = gitRepositoryPath != null ? $"--git-dir \"{gitRepositoryPath}\" {args}" : args;

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