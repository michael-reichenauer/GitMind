using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Git.Private
{
	internal class GitService : IGitService
	{
		private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(10);
		private static readonly TimeSpan UpdateTimeout = TimeSpan.FromSeconds(15);
		private static readonly TimeSpan PushTimeout = TimeSpan.FromSeconds(15);

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


		public R<string> GetCurrentRootPath(string workingFolder)
		{
			try
			{
				while (!string.IsNullOrEmpty(workingFolder))
				{
					if (LibGit2Sharp.Repository.IsValid(workingFolder))
					{
						return workingFolder;
					}

					workingFolder = Path.GetDirectoryName(workingFolder);
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
						if (commitId == GitCommit.UncommittedId)
						{
							return R.From(gitRepository.Status.CommitFiles);
						}

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
			string workingFolder, string commitId, string branchName)
		{
			try
			{
				string file = Path.Combine(workingFolder, ".git", "gitmind.specified");
				File.AppendAllText(file, $"{commitId} {branchName}\n");
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to add specified branch name for {commitId} {branchName}, {e}");
			}

			return Task.FromResult(true);
		}



		public IReadOnlyList<GitSpecifiedNames> GetSpecifiedNames(string workingFolder)
		{
			List<GitSpecifiedNames> branchNames = new List<GitSpecifiedNames>();

			try
			{
				string filePath = Path.Combine(workingFolder, ".git", "gitmind.specified");
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


		public Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string name)
		{
			return Task.Run(async () =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						string patch = gitRepository.Diff.GetFilePatch(commitId, name);

						return R.From(await gitDiffParser.ParseAsync(commitId, patch, false));
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
						string patch = gitRepository.Diff.GetPatch(commitId);

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


		public Task<R<CommitDiff>> GetCommitDiffRangeAsync(string workingFolder, string id1, string id2)
		{
			return Task.Run(async () =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						string patch = gitRepository.Diff.GetPatchRange(id1, id2);

						return R.From(await gitDiffParser.ParseAsync(null, patch));
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to get diff, {e.Message}");
					return Error.From(e);
				}
			});
		}


		public async Task FetchAsync(string workingFolder)
		{
			try
			{
				// Sometimes, a fetch to GitHub takes just forever, don't know why
				await FetchUsingCmdAsync(workingFolder)
					.WithCancellation(new CancellationTokenSource(FetchTimeout).Token);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to fetch {workingFolder}, {e.Message}");
			}

			//Log.Debug($"Fetching repository in {workingFolder} ...");

			//CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			//bool result = false;
			//try
			//{
			//	result = await Task.Run(() =>
			//	{
			//		try
			//		{
			//			using (GitRepository gitRepository = OpenRepository(workingFolder))
			//			{
			//				Log.Debug("Before fetch");
			//				gitRepository.Fetch();
			//				Log.Debug("After fetch");
			//			}

			//			Log.Debug("Fetched repository");
			//			return true;
			//		}
			//		catch (Exception e)
			//		{
			//			if (e.Message == "Unsupported URL protocol")
			//			{
			//				Log.Debug("Unsupported URL protocol");
			//				return false;
			//			}
			//			else
			//			{
			//				Log.Warn($"Failed to fetch, {e.Message}");
			//				return true;
			//			}
			//		}
			//	})
			//	.WithCancellation(cts.Token);
			//}
			//catch (Exception e)
			//{
			//	Log.Warn($"Failed to fetch {e}");
			//	result = false;
			//}

			//Log.Debug("???????");
			//if (!result)
			//{
			//	await FetchUsingCmdAsync(workingFolder);
			//}
		}


		private async Task FetchUsingCmdAsync(string workingFolder)
		{
			Log.Debug("Fetching repository using cmd ...");

			string args = "fetch";

			R<IReadOnlyList<string>> fetchResult = await GitAsync(workingFolder, args);

			fetchResult.OnValue(_ => Log.Debug("Fetched repository using cmd"));

			// Ignoring fetch errors for now
			fetchResult.OnError(e => Log.Warn($"Git fetch failed {e.Message}"));
		}


		public async Task FetchBranchAsync(string workingFolder, string branchName)
		{
			try
			{
				Log.Debug("Update branch using cmd fetch...");

				string args = $"fetch origin {branchName}:{branchName}";

				R<IReadOnlyList<string>> fetchResult = await GitAsync(workingFolder, args)
					.WithCancellation(new CancellationTokenSource(UpdateTimeout).Token);

				fetchResult.OnValue(_ => Log.Debug("updated branch using cmd fetch"));

				// Ignoring fetch errors for now
				fetchResult.OnError(e => Log.Warn($"Git update branch fetch failed {e.Message}"));
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to fetch branch fetch {workingFolder}, {e.Message}");
			}
		}


		public async Task MergeCurrentBranchFastForwardOnlyAsync(string workingFolder)
		{
			try
			{
				Log.Debug("Merge current branch fast forward ...");

				await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							gitRepository.MergeCurrentBranchFastForwardOnly();
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to merge current branch, {e.Message}");
					}
				});
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to merge current branch {workingFolder}, {e.Message}");
			}
		}


		public async Task MergeCurrentBranchAsync(string workingFolder)
		{
			try
			{
				Log.Debug($"Merge current branch (try ff, then no-ff) ... {workingFolder}");

				await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							try
							{
								gitRepository.MergeCurrentBranchFastForwardOnly();
							}
							catch (NonFastForwardException)
							{
								// Failed with fast forward merge, trying no fast forward
								gitRepository.MergeCurrentBranchNoFastForwardy();
							}
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to merge current branch, {e.Message}");
					}
				});
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to merge current branch {workingFolder}, {e.Message}");
			}
		}


		//public async Task MergeCurrentBranchFastForwardOnlyAsync(string workingFolder)
		//{
		//	try
		//	{
		//		Log.Debug("Update current branch using cmd...");

		//		await FetchAsync(workingFolder);

		//		string args = "merge --ff-only";

		//		R<IReadOnlyList<string>> mergeResult = await GitAsync(workingFolder, args)
		//			.WithCancellation(new CancellationTokenSource(UpdateTimeout).Token);

		//		mergeResult.OnValue(_ => Log.Debug("updated current branch using cmd"));

		//		// Ignoring fetch errors for now
		//		mergeResult.OnError(e => Log.Warn($"Git update current branch failed {e.Message}"));
		//	}
		//	catch (Exception e)
		//	{
		//		Log.Warn($"Failed to update current branch {workingFolder}, {e.Message}");
		//	}
		//}


		//public async Task MergeCurrentBranchAsync(string workingFolder)
		//{
		//	try
		//	{
		//		Log.Debug($"Pull current branch using cmd... {workingFolder}");

		//		await FetchAsync(workingFolder);

		//		string args = "merge --ff";

		//		R<IReadOnlyList<string>> mergeResult = await GitAsync(workingFolder, args)
		//			.WithCancellation(new CancellationTokenSource(UpdateTimeout).Token);

		//		mergeResult.OnValue(_ => Log.Debug("Pulled current branch using cmd"));

		//		// Ignoring fetch errors for now
		//		mergeResult.OnError(e => Log.Warn($"Git pull current branch failed {e.Message}"));
		//	}
		//	catch (Exception e)
		//	{
		//		Log.Warn($"Failed to pull current branch {workingFolder}, {e.Message}");
		//	}
		//}


		public async Task PushCurrentBranchAsync(string workingFolder)
		{
			try
			{
				Log.Debug($"Push current branch using cmd... {workingFolder}");

				string args = "push";

				R<IReadOnlyList<string>> pullResult = await GitAsync(workingFolder, args)
					.WithCancellation(new CancellationTokenSource(PushTimeout).Token);

				pullResult.OnValue(_ => Log.Debug("Pushed current branch using cmd"));

				// Ignoring fetch errors for now
				pullResult.OnError(e => Log.Warn($"Git push current branch failed {e.Message}"));
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to push current branch {workingFolder}, {e.Message}");
			}
		}


		public async Task PushBranchAsync(string workingFolder, string name)
		{
			try
			{
				Log.Debug($"Push {name} branch using cmd... {workingFolder}");

				string args = $"push origin {name}:{name}";

				R<IReadOnlyList<string>> pullResult = await GitAsync(workingFolder, args)
					.WithCancellation(new CancellationTokenSource(PushTimeout).Token);

				pullResult.OnValue(_ => Log.Debug($"Pushed {name} branch using cmd"));

				// Ignoring fetch errors for now.
				pullResult.OnError(e => Log.Warn($"Git push {name} branch failed {e.Message}"));
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to push {name} branch {workingFolder}, {e.Message}");
			}
		}


		public Task CommitAsync(string workingFolder, string message, IReadOnlyList<CommitFile> paths)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.Add(paths);

						gitRepository.Commit(message);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to commit, {e.Message}");
				}
			});
		}


		public Task SwitchToBranchAsync(string workingFolder, string branchName)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.Checkout(branchName);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to checkout {branchName}, {e.Message}");
				}
			});
		}


		public Task UndoFileInCurrentBranchAsync(string workingFolder, string path)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.UndoFileInCurrentBranch(path);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to undo {path}, {e.Message}");
				}
			});
		}


		public Task MergeAsync(string workingFolder, string branchName)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.MergeBranchNoFastForward(branchName);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to merge {branchName}, {e.Message}");
				}
			});
		}


		public Task SwitchToCommitAsync(string workingFolder, string commitId, string proposedBranchName)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.SwitchToCommit(commitId, proposedBranchName);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed switch to {commitId}, {e.Message}");
				}
			});
		}


		public Task CreateBranchAsync(string workingFolder, string branchName, string commitId)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.CreateBranch(branchName, commitId);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed create branch {branchName}, {e.Message}");
				}
			});
		}


		private async Task<R<IReadOnlyList<string>>> GitAsync(
			string gitRepositoryPath, string args)
		{
			string gitArgs = gitRepositoryPath != null ? $"--git-dir \"{gitRepositoryPath}\\.git\" {args}" : args;

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