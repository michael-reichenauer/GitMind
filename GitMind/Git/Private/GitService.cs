using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		private static readonly string CommitBranchNoteNameSpace = "GitMind.Branches";
		private static readonly string ManualBranchNoteNameSpace = "GitMind.Branches.Manual";
		//private static readonly string CommitBranchNoteOriginNameSpace = "origin/GitMind.Branches";
		//private static readonly string ManualBranchNoteOriginNameSpace = "GitMind.Branches.Manual";

		//private static readonly string LegacyGitPath = @"C:\Program Files (x86)\Git\bin\git.exe";
		//private static readonly string GitPath = @"C:\Program Files\Git\bin\git.exe";

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
			return new GitRepository(workingFolder, new LibGit2Sharp.Repository(workingFolder));
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
			SetNoteBranches(workingFolder, ManualBranchNoteNameSpace, commitId, branchName);

			return Task.FromResult(true);
		}


		public IReadOnlyList<BranchName> GetSpecifiedNames(string workingFolder, string rootId)
		{
			return GetNoteBranches(workingFolder, ManualBranchNoteNameSpace, rootId);
		}


		public Task SetCommitBranchAsync(
			string workingFolder, string commitId, string branchName)
		{
			SetNoteBranches(workingFolder, CommitBranchNoteNameSpace, commitId, branchName);

			return Task.CompletedTask;
		}


		public IReadOnlyList<BranchName> GetCommitBranches(string workingFolder, string rootId)
		{
			return GetNoteBranches(workingFolder, CommitBranchNoteNameSpace, rootId);
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
			//try
			//{
			//	// Sometimes, a fetch to GitHub takes just forever, don't know why
			//	await FetchUsingCmdAsync(workingFolder)
			//		.WithCancellation(new CancellationTokenSource(FetchTimeout).Token);
			//}
			//catch (Exception e)
			//{
			//	Log.Warn($"Failed to fetch {workingFolder}, {e.Message}");
			//}

			//Log.Debug($"Fetching repository in {workingFolder} ...");

			Log.Warn("Fetching .... ");
			CancellationTokenSource cts = new CancellationTokenSource(FetchTimeout);
			bool result = false;
			try
			{
				result = await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							Log.Debug("Before fetch");
							gitRepository.Fetch();
							Log.Debug("After fetch");
						}

						Log.Debug("Fetched repository");
						return true;
					}
					catch (Exception e)
					{
						if (e.Message == "Unsupported URL protocol")
						{
							Log.Warn("Unsupported URL protocol");
							return false;
						}
						else
						{
							Log.Warn($"Failed to fetch, {e.Message}");
							return true;
						}
					}
				})
				.WithCancellation(cts.Token);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to fetch {e}");
				result = false;
			}

			Log.Warn("Done fetching");
			if (!result)
			{
				Log.Warn("Failed to fetch");
				//await FetchUsingCmdAsync(workingFolder);
			}
		}


		public async Task FetchNotesAsync(string workingFolder)
		{
			try
			{
				// Sometimes, a fetch to GitHub takes just forever, don't know why
				await FetchNotesUsingCmdAsync(
					workingFolder, CommitBranchNoteNameSpace, ManualBranchNoteNameSpace)
					.WithCancellation(new CancellationTokenSource(FetchTimeout).Token);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to fetch notes {workingFolder}, {e.Message}");
			}


		}


		public async Task<IReadOnlyList<string>> UndoCleanWorkingFolderAsync(string workingFolder)
		{
			try
			{
				Log.Debug("Undo and clean ...");

				return await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							return gitRepository.UndoCleanWorkingFolder();
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to undo and clean, {e.Message}");
						return new[] { $"Error {e.Message}" };
					}
				});
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to undo and clean {workingFolder}, {e.Message}");
				return new[] { $"Error {e.Message}" };
			}
		}


		public async Task UndoWorkingFolderAsync(string workingFolder)
		{
			try
			{
				Log.Debug("Undo ...");

				await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							gitRepository.UndoWorkingFolder();
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to undo, {e.Message}");
					}
				});
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to undo {workingFolder}, {e.Message}");
			}
		}


		public void GetFile(string workingFolder, string fileId, string filePath)
		{
			try
			{
				using (GitRepository gitRepository = OpenRepository(workingFolder))
				{
					gitRepository.GetFile(fileId, filePath);
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to get file contents, {e.Message}");
			}
		}


		public async Task ResolveAsync(string workingFolder, string path)
		{
			try
			{
				Log.Debug("Resolve ...");

				await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							gitRepository.Resolve(path);
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to resolve, {e.Message}");
					}
				});
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to resolve {path}, {e.Message}");
			}
		}


		public Task<bool> TryDeleteBranchAsync(
			string workingFolder, 
			string branchName,
			bool isRemote,
			bool isUseForce, 
			ICredentialHandler credentialHandler)
		{
			if (isRemote)
			{
				return TryDeleteRemoteBranchAsync(workingFolder, branchName, isUseForce, credentialHandler);
			}
			else
			{
				return TryDeleteLocalBranchAsync(workingFolder, branchName, isUseForce);
			}
		}


		private async Task<bool> TryDeleteLocalBranchAsync(
			string workingFolder, string branchName, bool isUseForce)
		{
			try
			{
				Log.Debug($"Delete branch {branchName} ...");

				return await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							return gitRepository.TryDeleteBranch(branchName, false, isUseForce);
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to delete branch {branchName}, {e.Message}");
						return false;
					}
				});
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to delete branch {branchName}, {e.Message}");
				return false;
			}
		}

		private async Task<bool> TryDeleteRemoteBranchAsync(
			string workingFolder, string branchName, bool isUseForce, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Delete branch {branchName} ...");

			CancellationToken ct = credentialHandler.GetTimeoutToken(PushTimeout);

			try
			{
				Log.Debug($"Push delete branch {branchName} branch using cmd... {workingFolder}");
				return await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							if (!isUseForce)
							{
								if (!gitRepository.IsBranchMerged(branchName, true))
								{
									return false;
								}
							}

							gitRepository.DeleteRemoteBranch(branchName, credentialHandler);
							return true;
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to delete branch {branchName}, {e.Message}");
						return false;
					}
				})
				.WithCancellation(ct);


				//string args = $"push origin :{branchName}";

				//R<IReadOnlyList<string>> pushResult = await GitAsync(workingFolder, args)
				//	.WithCancellation(new CancellationTokenSource(PushTimeout).Token);

				//if (pushResult.HasValue)
				//{
				//	Log.Debug($"Pushed delete {branchName} branch using cmd");
				//	return true;
				//}
				//else
				//{
				//	Log.Warn($"Git push delete {branchName} branch failed {pushResult}");
				//}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to push delete {branchName} branch {workingFolder}, {e.Message}");
			}

			return false;
		}


		//private async Task FetchUsingCmdAsync(string workingFolder)
		//{
		//	Log.Debug("Fetching repository using cmd ...");

		//	string args = "fetch";

		//	R<IReadOnlyList<string>> fetchResult = await GitAsync(workingFolder, args);

		//	fetchResult.OnValue(_ => Log.Debug("Fetched repository using cmd"));

		//	// Ignoring fetch errors for now
		//	fetchResult.OnError(e => Log.Warn($"Git fetch failed {e.Message}"));
		//}


		public async Task FetchBranchAsync(string workingFolder, string branchName)
		{
			try
			{
				await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							gitRepository.FetchBranch(branchName);
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to fetch, {e.Message}");
					}
				});

				//Log.Debug("Update branch using cmd fetch...");

				//string args = $"fetch origin {branchName}:{branchName}";

				//R<IReadOnlyList<string>> fetchResult = await GitAsync(workingFolder, args)
				//	.WithCancellation(new CancellationTokenSource(UpdateTimeout).Token);

				//fetchResult.OnValue(_ => Log.Debug("updated branch using cmd fetch"));

				//// Ignoring fetch errors for now
				//fetchResult.OnError(e => Log.Warn($"Git update branch fetch failed {e.Message}"));
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
								// Failed with fast forward merge, trying no fast forward.
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


		public async Task PushCurrentBranchAsync(string workingFolder, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Push current branch ... {workingFolder}");

			CancellationToken ct = credentialHandler.GetTimeoutToken(PushTimeout);
			try
			{
				await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							Log.Debug("Before push");
							gitRepository.PushCurrentBranch(credentialHandler);
							Log.Debug("After push");
						}

						Log.Debug("push current branch");
						return true;
					}
					catch (Exception e)
					{
						if (e.Message == "Unsupported URL protocol")
						{
							Log.Warn("Unsupported URL protocol");
							return false;
						}
						else
						{
							Log.Warn($"Failed to push, {e.Message}");
							return true;
						}
					}
				})
				.WithCancellation(ct);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to push {e}");
			}


			//try
			//{
			//	Log.Debug($"Push current branch using cmd... {workingFolder}");

			//	string args = "push origin HEAD";

			//	R<IReadOnlyList<string>> pullResult = await GitAsync(workingFolder, args)
			//		.WithCancellation(new CancellationTokenSource(PushTimeout).Token);

			//	pullResult.OnValue(_ => Log.Debug("Pushed current branch using cmd"));

			//	// Ignoring fetch errors for now
			//	pullResult.OnError(e => Log.Warn($"Git push current branch failed {e.Message}"));
			//}
			//catch (Exception e)
			//{
			//	Log.Warn($"Failed to push current branch {workingFolder}, {e.Message}");
			//}
		}




		public async Task PushNotesAsync(string workingFolder, string rootId, ICredentialHandler credentialHandler)
		{
			await PushNotesUsingCmdAsync(workingFolder, CommitBranchNoteNameSpace, rootId, credentialHandler);
			await PushNotesUsingCmdAsync(workingFolder, ManualBranchNoteNameSpace, rootId, credentialHandler);
		}


		public async Task PushBranchAsync(
			string workingFolder, string branchName, ICredentialHandler credentialHandler)
		{
			try
			{
				await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							gitRepository.PushBranch(branchName, credentialHandler);
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to push branch {branchName}, {e.Message}");
					}
				});

				//Log.Debug($"Push {name} branch using cmd... {workingFolder}");

				//string args = $"push origin {name}:{name}";

				//R<IReadOnlyList<string>> pullResult = await GitAsync(workingFolder, args)
				//	.WithCancellation(new CancellationTokenSource(PushTimeout).Token);

				//pullResult.OnValue(_ => Log.Debug($"Pushed {name} branch using cmd"));

				//// Ignoring fetch errors for now.
				//pullResult.OnError(e => Log.Warn($"Git push {name} branch failed {e.Message}"));
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to push {branchName} branch {workingFolder}, {e.Message}");
			}
		}


		public Task<GitCommit> CommitAsync(string workingFolder, string message, IReadOnlyList<CommitFile> paths)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.Add(paths);

						return gitRepository.Commit(message);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to commit, {e.Message}");
					return null;
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


		public Task<GitCommit> MergeAsync(string workingFolder, string branchName)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						return gitRepository.MergeBranchNoFastForward(branchName);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to merge {branchName}, {e.Message}");
					return null;
				}
			});
		}


		public Task<string> SwitchToCommitAsync(string workingFolder, string commitId, string proposedBranchName)
		{
			return Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						return gitRepository.SwitchToCommit(commitId, proposedBranchName);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed switch to {commitId}, {e.Message}");
					return null;
				}
			});
		}


		public async Task CreateBranchAsync(string workingFolder, string branchName, string commitId)
		{
			await Task.Run(() =>
			{
				try
				{
					Log.Debug($"Create branch {branchName} at {commitId} ...");
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.CreateBranch(branchName, commitId);
					}

					Log.Debug($"Created branch {branchName} at {commitId}...");
				}
				catch (Exception e)
				{
					Log.Warn($"Failed create branch {branchName}, {e.Message}");
				}
			});
		}


		public async Task<bool> PublishBranchAsync(
			string workingFolder, string branchName, ICredentialHandler credentialHandler)
		{
			try
			{

				return await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = OpenRepository(workingFolder))
						{
							gitRepository.PublishBranch(branchName);

							gitRepository.PushBranch(branchName, credentialHandler);
							return true;
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed publish to {branchName}, {e.Message}");
						return false;
					}
				});

				//Log.Debug($"Push {branchName} branch using cmd... {workingFolder}");

				//string args = $"push --set-upstream origin {branchName}";

				//R<IReadOnlyList<string>> pullResult = await GitAsync(workingFolder, args)
				//	.WithCancellation(new CancellationTokenSource(PushTimeout).Token);

				//if (pullResult.HasValue)
				//{
				//	Log.Debug($"Pushed {branchName} branch using cmd");
				//	return true;
				//}

				//// Ignoring fetch errors for now.
				//Log.Warn($"Git push {branchName} branch failed ");
			}
			catch (Exception e)
			{
				Log.Error($"Failed to publish branch {branchName}, {e}");
			}

			return false;
		}


		public string GetFullMessage(string workingFolder, string commitId)
		{
			try
			{
				using (GitRepository gitRepository = OpenRepository(workingFolder))
				{
					return gitRepository.GetFullMessage(commitId);
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed get full message {commitId}, {e.Message}");
				return null;
			}
		}


		//private async Task<R<IReadOnlyList<string>>> GitAsync(
		//	string gitRepositoryPath, string args)
		//{
		//	string gitArgs = gitRepositoryPath != null ? $"--git-dir \"{gitRepositoryPath}\\.git\" {args}" : args;

		//	return await Task.Run(() => GitCommand(gitArgs));
		//}


		//private R<IReadOnlyList<string>> GitCommand(string gitArgs)
		//{
		//	R<string> gitBinPath = GetGitBinPath();
		//	if (gitBinPath.IsFaulted) return gitBinPath.Error;

		//	CmdResult result = cmd.Run(gitBinPath.Value, gitArgs);

		//	if (0 == result.ExitCode || 1 == result.ExitCode)
		//	{
		//		return R.From(result.Output);
		//	}
		//	else
		//	{
		//		Log.Warn($"Error: git {gitArgs}, {result.ExitCode}, {string.Join("\n", result.Error)}");
		//		return GitCommandError.With(result.ToString());
		//	}
		//}


		//private R<string> GetGitBinPath()
		//{
		//	if (File.Exists(GitPath))
		//	{
		//		return GitPath;
		//	}
		//	else if (File.Exists(LegacyGitPath))
		//	{
		//		return LegacyGitPath;
		//	}
		//	else
		//	{
		//		string appdataPath = Environment.GetFolderPath(
		//			Environment.SpecialFolder.LocalApplicationData);

		//		string gitPath = Path.Combine(appdataPath, "Programs", "Git", "cmd", "git.exe");

		//		if (File.Exists(gitPath))
		//		{
		//			return gitPath;
		//		}
		//	}

		//	return GitNotInstalledError.With("Git binary not found");
		//}


		private void SetNoteBranches(
			string workingFolder, string nameSpace, string commitId, string branchName)
		{
			Log.Debug($"Set {nameSpace}: {commitId} {branchName}");

			try
			{
				string file = Path.Combine(workingFolder, ".git", nameSpace);
				File.AppendAllText(file, $"{commitId} {branchName}\n");
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to add commit name for {commitId} {branchName}, {e}");
			}
		}


		private IReadOnlyList<BranchName> GetNoteBranches(
			string workingFolder, string nameSpace, string rootId)
		{
			List<BranchName> branchNames = new List<BranchName>();

			Log.Debug($"Getting {nameSpace} ...");

			try
			{

				string notesText = "";
				using (GitRepository gitRepository = OpenRepository(workingFolder))
				{
					IReadOnlyList<GitNote> notes = gitRepository.GetCommitNotes(rootId);
					GitNote note = notes.FirstOrDefault(n => n.NameSpace == $"origin/{nameSpace}");

					notesText = note?.Message ?? "";
				}

				try
				{
					string file = Path.Combine(workingFolder, ".git", nameSpace);
					if (File.Exists(file))
					{
						notesText += File.ReadAllText(file);
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to read local {nameSpace}, {e}");
				}

				branchNames = ParseBranchNames(notesText);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to get note branches, {e.Message}");
			}

			Log.Debug($"Got {branchNames.Count} branches for {nameSpace}");

			return branchNames;
		}


		private List<BranchName> ParseBranchNames(string text)
		{
			List<BranchName> branchNames = new List<BranchName>();

			if (string.IsNullOrWhiteSpace(text))
			{
				return branchNames;
			}

			string[] lines = text.Split("\n".ToCharArray());
			foreach (string line in lines)
			{
				string[] parts = line.Split(" ".ToCharArray());
				if (parts.Length == 2)
				{
					string commitId = parts[0];
					string branchName = parts[1].Trim();
					branchNames.Add(new BranchName(commitId, branchName));
				}
			}

			return branchNames;
		}


		private async Task PushNotesUsingCmdAsync(
			string workingFolder, string nameSpace, string rootId, ICredentialHandler credentialHandler)
		{
			// git push origin refs/notes/GitMind.Branches
			// git notes --ref=GitMind.Branches merge -s cat_sort_uniq refs/notes/origin/GitMind.Branches
			// git fetch origin refs/notes/GitMind.Branches:refs/notes/origin/GitMind.Branches

			string addedNotesText = "";
			try
			{
				string file = Path.Combine(workingFolder, ".git", nameSpace);
				if (File.Exists(file))
				{
					addedNotesText = File.ReadAllText(file);
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to read local {nameSpace}, {e}");
			}

			if (string.IsNullOrWhiteSpace(addedNotesText))
			{
				Log.Debug("Notes is empty, no need to push notes");
				return;
			}
			else
			{
				Log.Debug($"Adding notes:\n{addedNotesText}");
			}

			await FetchNotesUsingCmdAsync(workingFolder, nameSpace);

			string originNotesText = "";
			using (GitRepository gitRepository = OpenRepository(workingFolder))
			{
				IReadOnlyList<GitNote> notes = gitRepository.GetCommitNotes(rootId);
				GitNote note = notes.FirstOrDefault(n => n.NameSpace == $"origin/{nameSpace}");

				originNotesText = note?.Message ?? "";
			}

			string notesText = originNotesText + addedNotesText;

			try
			{
				using (GitRepository gitRepository = OpenRepository(workingFolder))
				{
					GitNote gitNote = new GitNote(nameSpace, notesText);

					gitRepository.SetCommitNote(rootId, gitNote);
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to set note branch, {e.Message}");
			}


			await Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.PushBranch($"refs/notes/{nameSpace}", credentialHandler);

						Log.Debug($"Pushed notes {nameSpace}");
						string file = Path.Combine(workingFolder, ".git", nameSpace);
						if (File.Exists(file))
						{
							File.Delete(file);
						}

						return true;
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed push notes to {nameSpace}, {e.Message}");
					return false;
				}
			});


			//Log.Debug($"Push {nameSpace} notes using cmd ...");

			//string args = $"push origin refs/notes/{nameSpace}";

			//R<IReadOnlyList<string>> fetchResult = await GitAsync(workingFolder, args);

			//fetchResult.OnValue(_ =>
			//{
			//	Log.Debug($"Pushed notes {nameSpace} using cmd");
			//	string file = Path.Combine(workingFolder, ".git", nameSpace);
			//	if (File.Exists(file))
			//	{
			//		File.Delete(file);
			//	}
			//});

			//// Ignoring fetch errors for now
			//fetchResult.OnError(e => Log.Warn($"Git push notes {nameSpace} failed {e.Message}"));

			//Log.Debug($"Pushed {nameSpace} notes using cmd");
		}


		private async Task FetchNotesUsingCmdAsync(string workingFolder, string nameSpace)
		{
			Log.Debug($"Fetching {nameSpace} notes ...");
			await Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.FetchRefsBranch($"refs/notes/{nameSpace}:refs/notes/origin/{nameSpace}");
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to fetch, {e.Message}");
				}
			});


			//Log.Debug($"Fetching {nameSpace} notes using cmd ...");

			//string args = $"fetch origin refs/notes/{nameSpace}:refs/notes/origin/{nameSpace}";

			//R<IReadOnlyList<string>> fetchResult = await GitAsync(workingFolder, args);

			//fetchResult.OnValue(_ => Log.Debug($"Fetched notes {nameSpace} using cmd"));

			//// Ignoring fetch errors for now
			//fetchResult.OnError(e => Log.Warn($"Git fetch notes {nameSpace} failed {e.Message}"));
		}


		private async Task FetchNotesUsingCmdAsync(string workingFolder, string nameSpace, string nameSpace2)
		{
			Log.Debug($"Fetching {nameSpace} and {nameSpace2} ...");

			await Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = OpenRepository(workingFolder))
					{
						gitRepository.FetchRefsBranch($"refs/notes/{nameSpace}:refs/notes/origin/{nameSpace}");
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to fetch, {e.Message}");
				}
			});

			//Log.Debug($"Fetching {nameSpace} and {nameSpace2} notes using cmd ...");

			//string args = $"fetch origin refs/notes/{nameSpace}:refs/notes/origin/{nameSpace}";
			//args += $" refs/notes/{nameSpace2}:refs/notes/origin/{nameSpace2}";

			//R<IReadOnlyList<string>> fetchResult = await GitAsync(workingFolder, args);

			//fetchResult.OnValue(_ => Log.Debug($"Fetched notes {nameSpace} and {nameSpace2} using cmd"));

			//// Ignoring fetch errors for now
			//fetchResult.OnError(e => Log.Warn($"Git failed to notes {e.Message}"));
		}
	}
}