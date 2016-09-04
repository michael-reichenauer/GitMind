using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
		private static readonly TimeSpan PushTimeout = TimeSpan.FromSeconds(15);

		private static readonly string CommitBranchNoteNameSpace = "GitMind.Branches";
		private static readonly string ManualBranchNoteNameSpace = "GitMind.Branches.Manual";

		private readonly IGitDiffParser gitDiffParser;


		public GitService(IGitDiffParser gitDiffParser)
		{
			this.gitDiffParser = gitDiffParser;
		}


		public GitService()
			: this(new GitDiffParser())
		{
		}


		public R<string> GetCurrentRootPath(string folder)
		{
			try
			{
				// The specified folder, might be a sub folder of the root working folder,
				// lets try to find root folder by testing folder and then its parent folders
				// until a root folder is found or no root folder is found.
				string rootFolder = folder;
				while (!string.IsNullOrEmpty(rootFolder))
				{
					if (LibGit2Sharp.Repository.IsValid(rootFolder))
					{
						return rootFolder;
					}

					// Get the parent folder to test that
					rootFolder = Path.GetDirectoryName(rootFolder);
				}

				return Error.From($"No working folder {folder}");
			}
			catch (Exception e)
			{
				return Error.From(e, $"Failed to root of working folder {folder}, {e.Message}");
			}
		}


		public Task<R<GitStatus>> GetStatusAsync(string workingFolder)
		{
			return DoAsync(workingFolder, repo => repo.Status);
		}


		public Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId)
		{
			return DoAsync(workingFolder, repo =>
			{
				if (commitId == GitCommit.UncommittedId)
				{
					return repo.Status.CommitFiles;
				}

				return repo.Diff.GetFiles(commitId);
			});
		}


		public Task SetSpecifiedCommitBranchAsync(
			string workingFolder, string commitId, string branchName)
		{
			SetNoteBranches(workingFolder, ManualBranchNoteNameSpace, commitId, branchName);

			return Task.FromResult(true);
		}


		public Task SetCommitBranchAsync(
			string workingFolder, string commitId, string branchName)
		{
			SetNoteBranches(workingFolder, CommitBranchNoteNameSpace, commitId, branchName);

			return Task.CompletedTask;
		}


		public IReadOnlyList<BranchName> GetSpecifiedNames(string workingFolder, string rootId)
		{
			return GetNoteBranches(workingFolder, ManualBranchNoteNameSpace, rootId);
		}


		public IReadOnlyList<BranchName> GetCommitBranches(string workingFolder, string rootId)
		{
			return GetNoteBranches(workingFolder, CommitBranchNoteNameSpace, rootId);
		}


		public Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string name)
		{
			return DoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetFilePatch(commitId, name);

				return await gitDiffParser.ParseAsync(commitId, patch, false);
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffAsync(string workingFolder, string commitId)
		{
			return DoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetPatch(commitId);

				return await gitDiffParser.ParseAsync(commitId, patch);
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffRangeAsync(string workingFolder, string id1, string id2)
		{
			return DoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetPatchRange(id1, id2);

				return await gitDiffParser.ParseAsync(null, patch);
			});
		}


		 
		public async Task FetchAsync(string workingFolder)
		{
			await DoAsync(workingFolder, FetchTimeout, repo => repo.Fetch());	
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
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
				Log.Debug($"Push delete branch {branchName} branch ... {workingFolder}");
				return await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to push delete {branchName} branch {workingFolder}, {e.Message}");
			}

			return false;
		}


		public async Task FetchBranchAsync(string workingFolder, string branchName)
		{
			try
			{
				await Task.Run(() =>
				{
					try
					{
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
						{
							gitRepository.FetchBranch(branchName);
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to fetch, {e.Message}");
					}
				});
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
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
						{
							gitRepository.PushBranch(branchName, credentialHandler);
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to push branch {branchName}, {e.Message}");
					}
				});
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
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
						{
							gitRepository.PublishBranch(branchName, credentialHandler);
							return true;
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed publish to {branchName}, {e.Message}");
						return false;
					}
				});
			}
			catch (Exception e)
			{
				Log.Error($"Failed to publish branch {branchName}, {e}");
			}

			return false;
		}


		public bool IsSupportedRemoteUrl(string workingFolder)
		{
			try
			{
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
				{
					return gitRepository.IsSupportedRemoteUrl();
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed get check remote access url, {e.Message}");
			}

			return false;
		}


		public string GetFullMessage(string workingFolder, string commitId)
		{
			try
			{
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
			using (GitRepository gitRepository = GitRepository.Open(workingFolder))
			{
				IReadOnlyList<GitNote> notes = gitRepository.GetCommitNotes(rootId);
				GitNote note = notes.FirstOrDefault(n => n.NameSpace == $"origin/{nameSpace}");

				originNotesText = note?.Message ?? "";
			}

			string notesText = originNotesText + addedNotesText;

			try
			{
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
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
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
					{
						gitRepository.PushRefs($"refs/notes/{nameSpace}", credentialHandler);

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
		}


		private async Task FetchNotesUsingCmdAsync(string workingFolder, string nameSpace)
		{
			Log.Debug($"Fetching {nameSpace} notes ...");
			await Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
					{
						gitRepository.FetchRefs($"refs/notes/{nameSpace}:refs/notes/origin/{nameSpace}");
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to fetch, {e.Message}");
				}
			});
		}


		private async Task FetchNotesUsingCmdAsync(string workingFolder, string nameSpace, string nameSpace2)
		{
			Log.Debug($"Fetching {nameSpace} and {nameSpace2} ...");

			await Task.Run(() =>
			{
				try
				{
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
					{
						gitRepository.FetchRefs($"refs/notes/{nameSpace}:refs/notes/origin/{nameSpace}");
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to fetch, {e.Message}");
				}
			});
		}


		private static Task<R<T>> DoAsync<T>(
			string workingFolder,
			Func<GitRepository, T> doFunction,
			[CallerMemberName] string memberName = "")
		{
			return Task.Run(() =>
			{
				Log.Debug($"{memberName} in {workingFolder} ...");
				try
				{
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
					{
						T functionResult = doFunction(gitRepository);

						R<T> result = R.From(functionResult);

						Log.Debug($"Done {memberName} in {workingFolder}");

						return result;
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
					return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
				}
			});
		}

		private static Task<R> DoAsync(
			string workingFolder,
			TimeSpan timeout,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "")
		{
			CancellationTokenSource cts = new CancellationTokenSource(timeout);

			try
			{
				return Task.Run(() =>
				{
					Log.Debug($"{memberName} in {workingFolder} ...");
					try
					{
						using (GitRepository gitRepository = GitRepository.Open(workingFolder))
						{
							doAction(gitRepository);

							Log.Debug($"Done {memberName} in {workingFolder}");

							return R.Ok;
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
						return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
					}
				},
				cts.Token)
				.WithCancellation(cts.Token);
			}
			catch (OperationCanceledException e)
			{
				Log.Warn($"Timeout for {memberName} in {workingFolder}, {e.Message}");
				Error error = Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
				return Task.FromResult(new R(error));
			}
		}


		private static Task<R<T>> DoAsync<T>(
			string workingFolder,
			Func<GitRepository, Task<T>> doFunction,
			[CallerMemberName] string memberName = "")
		{
			return Task.Run(async () =>
			{
				Log.Debug($"{memberName} in {workingFolder} ...");
				try
				{
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
					{
						T functionResult = await doFunction(gitRepository);

						R<T> result = R.From(functionResult);

						Log.Debug($"Done {memberName} in {workingFolder}");

						return result;
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
					return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
				}
			});
		}
	}
}