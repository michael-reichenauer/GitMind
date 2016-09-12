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
		private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan PushTimeout = TimeSpan.FromSeconds(30);

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
						Log.Debug($"Root folder for {folder} is {rootFolder}");
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
			return UseRepoAsync(workingFolder, repo => repo.Status);
		}


		public Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId)
		{
			Log.Debug($"Getting files for {commitId} ...");
			return UseRepoAsync(workingFolder, repo =>
			{
				if (commitId == GitCommit.UncommittedId)
				{
					return repo.Status.CommitFiles;
				}

				return repo.Diff.GetFiles(commitId);
			});
		}


		public Task SetManualCommitBranchAsync(
			string workingFolder, string commitId, string branchName)
		{
			Log.Debug($"Set manual branch name {branchName} for commit {commitId} ...");
			SetNoteBranches(workingFolder, ManualBranchNoteNameSpace, commitId, branchName);

			return Task.FromResult(true);
		}


		public Task SetCommitBranchAsync(
			string workingFolder, string commitId, string branchName)
		{
			Log.Debug($"Set commit branch name {branchName} for commit {commitId} ...");
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


		public Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string path)
		{
			Log.Debug($"Get diff for file {path} for commit {commitId} ...");
			return UseRepoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetFilePatch(commitId, path);

				return await gitDiffParser.ParseAsync(commitId, patch, false);
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffAsync(string workingFolder, string commitId)
		{
			Log.Debug($"Get diff for commit {commitId} ...");
			return UseRepoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetPatch(commitId);

				return await gitDiffParser.ParseAsync(commitId, patch);
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffRangeAsync(string workingFolder, string id1, string id2)
		{
			Log.Debug($"Get diff for commit range {id1}-{id2} ...");
			return UseRepoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetPatchRange(id1, id2);

				return await gitDiffParser.ParseAsync(null, patch);
			});
		}


		public async Task FetchAsync(string workingFolder)
		{
			await UseRepoAsync(workingFolder, FetchTimeout, repo => repo.Fetch());
		}


		public Task FetchBranchAsync(string workingFolder, string branchName)
		{
			Log.Debug($"Fetch branch {branchName}...");
			return UseRepoAsync(workingFolder, repo => repo.FetchBranch(branchName));
		}


		public async Task FetchAllNotesAsync(string workingFolder)
		{
			Log.Debug("Fetch all notes ...");
			string[] noteRefs = {
				$"refs/notes/{CommitBranchNoteNameSpace}:refs/notes/origin/{CommitBranchNoteNameSpace}",
				$"refs/notes/{ManualBranchNoteNameSpace}:refs/notes/origin/{ManualBranchNoteNameSpace}",
			};

			await UseRepoAsync(workingFolder, FetchTimeout, repo => repo.FetchRefs(noteRefs));
		}


		private async Task FetchNotesAsync(string workingFolder, string nameSpace)
		{
			Log.Debug($"Fetch notes for {nameSpace} ...");
			string[] noteRefs = { $"refs/notes/{nameSpace}:refs/notes/origin/{nameSpace}" };

			await UseRepoAsync(workingFolder, FetchTimeout, repo => repo.FetchRefs(noteRefs));
		}


		public Task<R<IReadOnlyList<string>>> UndoCleanWorkingFolderAsync(string workingFolder)
		{
			return UseRepoAsync(workingFolder, repo => repo.UndoCleanWorkingFolder());
		}


		public Task UndoWorkingFolderAsync(string workingFolder)
		{
			return UseRepoAsync(workingFolder, repo => repo.UndoWorkingFolder());
		}


		public void GetFile(string workingFolder, string fileId, string filePath)
		{
			Log.Debug($"Get file {fileId}, {filePath} ...");
			UseRepo(workingFolder, repo => repo.GetFile(fileId, filePath));
		}


		public Task ResolveAsync(string workingFolder, string path)
		{
			Log.Debug($"Resolve {path}  ...");
			return UseRepoAsync(workingFolder, repo => repo.Resolve(path));
		}


		public Task<R> DeleteBranchAsync(
			string workingFolder,
			string branchName,
			bool isRemote,
			ICredentialHandler credentialHandler)
		{
			if (isRemote)
			{
				return DeleteRemoteBranchAsync(workingFolder, branchName, credentialHandler);
			}
			else
			{
				return DeleteLocalBranchAsync(workingFolder, branchName);
			}
		}


		private Task<R> DeleteLocalBranchAsync(string workingFolder, string branchName)
		{
			Log.Debug($"Delete local branch {branchName}  ...");
			return UseRepoAsync(workingFolder, repo => repo.DeleteLocalBranch(branchName));
		}


		private Task<R> DeleteRemoteBranchAsync(
			string workingFolder, string branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Delete remote branch {branchName} ...");
			return UseRepoAsync(workingFolder, PushTimeout, repo =>
				repo.DeleteRemoteBranch(branchName, credentialHandler));
		}


		public Task MergeCurrentBranchFastForwardOnlyAsync(string workingFolder)
		{
			return UseRepoAsync(workingFolder, repo => repo.MergeCurrentBranchFastForwardOnly());
		}


		public Task MergeCurrentBranchAsync(string workingFolder)
		{
			return UseRepoAsync(workingFolder, repo =>
			{
				// First try to update using fast forward merge only
				R result = repo.MergeCurrentBranchFastForwardOnly();

				if (result.Error.Is<NonFastForwardException>())
				{
					// Failed with fast forward merge, trying no fast forward.
					repo.MergeCurrentBranchNoFastForward();
				}
			});
		}


		public Task PushCurrentBranchAsync(
			string workingFolder, ICredentialHandler credentialHandler)
		{
			return UseRepoAsync(workingFolder, PushTimeout,
				repo => repo.PushCurrentBranch(credentialHandler));
		}


		public async Task PushNotesAsync(
			string workingFolder, string rootId, ICredentialHandler credentialHandler)
		{
			await PushNotesAsync(workingFolder, CommitBranchNoteNameSpace, rootId, credentialHandler);
			await PushNotesAsync(workingFolder, ManualBranchNoteNameSpace, rootId, credentialHandler);
		}


		public Task PushBranchAsync(
			string workingFolder, string branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Push branch {branchName} ...");
			return UseRepoAsync(workingFolder, PushTimeout,
				repo => repo.PushBranch(branchName, credentialHandler));
		}


		public Task<R<GitCommit>> CommitAsync(
			string workingFolder, string message, IReadOnlyList<CommitFile> paths)
		{
			Log.Debug($"Commit {paths.Count} files: {message} ...");
			return UseRepoAsync(workingFolder,
				repo =>
				{
					repo.Add(paths);
					return repo.Commit(message);
				});
		}


		public Task SwitchToBranchAsync(string workingFolder, string branchName)
		{
			Log.Debug($"Switch to branch {branchName} ...");
			return UseRepoAsync(workingFolder, repo => repo.Checkout(branchName));
		}


		public Task<R<string>> SwitchToCommitAsync(
			string workingFolder, string commitId, string proposedBranchName)
		{
			Log.Debug($"Switch to commit {commitId} with proposed branch name {proposedBranchName} ...");
			return UseRepoAsync(workingFolder, repo => repo.SwitchToCommit(commitId, proposedBranchName));
		}


		public Task UndoFileInCurrentBranchAsync(string workingFolder, string path)
		{
			Log.Debug($"Undo uncommitted file {path} ...");
			return UseRepoAsync(workingFolder, repo => repo.UndoFileInCurrentBranch(path));
		}


		public Task<R<GitCommit>> MergeAsync(string workingFolder, string branchName)
		{
			Log.Debug($"Merge branch {branchName} into current branch ...");
			return UseRepoAsync(workingFolder, repo => repo.MergeBranchNoFastForward(branchName));
		}


		public Task CreateBranchAsync(string workingFolder, string branchName, string commitId)
		{
			Log.Debug($"Create branch {branchName} at commit {commitId} ...");
			return UseRepoAsync(workingFolder, repo => repo.CreateBranch(branchName, commitId));
		}


		public Task<R> PublishBranchAsync(
			string workingFolder, string branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Publish branch {branchName} ...");
			return UseRepoAsync(workingFolder, repo => repo.PublishBranch(branchName, credentialHandler));
		}


		public bool IsSupportedRemoteUrl(string workingFolder)
		{
			return UseRepo(workingFolder, repo => repo.IsSupportedRemoteUrl()).Or(false);
		}


		public R<string> GetFullMessage(string workingFolder, string commitId)
		{
			Log.Debug($"Get full commit message for commit {commitId} ...");
			return UseRepo(workingFolder, repo => repo.GetFullMessage(commitId));
		}


		private void SetNoteBranches(
			string workingFolder, string nameSpace, string commitId, string branchName)
		{
			Log.Debug($"Set note {nameSpace} for commit {commitId} with branch {branchName} ...");

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
			Log.Debug($"Getting notes {nameSpace} from root commit {rootId} ...");

			string notesText = UseRepo(workingFolder, repo =>
			{
				IReadOnlyList<GitNote> notes = repo.GetCommitNotes(rootId);
				GitNote note = notes.FirstOrDefault(n => n.NameSpace == $"origin/{nameSpace}");
				return note?.Message ?? "";
			})
			.Or("");

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

			List<BranchName> branchNames = ParseBranchNames(notesText);

			Log.Debug($"Got {branchNames.Count} branch names for {nameSpace}");

			return branchNames;
		}


		private List<BranchName> ParseBranchNames(string text)
		{
			List<BranchName> branchNames = new List<BranchName>();

			try
			{
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
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to parse notes text, error: {e}\n text:\n{text}");
			}

			return branchNames;
		}


		private async Task PushNotesAsync(
			string workingFolder, string nameSpace, string rootId, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Push notes {nameSpace} at root commit {rootId} ...");

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

			await FetchNotesAsync(workingFolder, nameSpace);

			string originNotesText = UseRepo(workingFolder, repo =>
			{
				IReadOnlyList<GitNote> notes = repo.GetCommitNotes(rootId);
				GitNote note = notes.FirstOrDefault(n => n.NameSpace == $"origin/{nameSpace}");

				return note?.Message ?? "";
			})
			.Or("");

			string notesText = originNotesText + addedNotesText;

			UseRepo(workingFolder, repo =>
				repo.SetCommitNote(rootId, new GitNote(nameSpace, notesText)));

			await UseRepoAsync(workingFolder, repo =>
			{
				repo.PushRefs($"refs/notes/{nameSpace}", credentialHandler);

				string file = Path.Combine(workingFolder, ".git", nameSpace);
				if (File.Exists(file))
				{
					File.Delete(file);
				}
			});
		}


		private static R UseRepo(
			string workingFolder,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "")
		{
			Log.Debug($"Start {memberName} in {workingFolder} ...");
			try
			{
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
				{
					doAction(gitRepository);

					Log.Debug($"Done  {memberName} in {workingFolder}");

					return R.Ok;
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
				return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
			}
		}


		private static Task<R> UseRepoAsync(
			string workingFolder,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "")
		{
			return Task.Run(() => UseRepo(workingFolder, doAction, memberName));
		}


		private static async Task<R> UseRepoAsync(
			string workingFolder,
			TimeSpan timeout,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "")
		{
			CancellationTokenSource cts = new CancellationTokenSource(timeout);

			try
			{
				return await Task.Run(() => UseRepo(workingFolder, doAction, memberName), cts.Token)
					.WithCancellation(cts.Token);
			}
			catch (OperationCanceledException e)
			{
				Log.Warn($"Timeout for {memberName} in {workingFolder}, {e.Message}");
				Error error = Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
				return error;
			}
		}


		private static R<T> UseRepo<T>(
			string workingFolder,
			Func<GitRepository, T> doFunction,
			[CallerMemberName] string memberName = "")
		{
			Log.Debug($"Start {memberName} in {workingFolder} ...");
			try
			{
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
				{
					T functionResult = doFunction(gitRepository);

					R<T> result = R.From(functionResult);

					Log.Debug($"Done  {memberName} in {workingFolder}");

					return result;
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
				return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
			}
		}

		private static R UseRepo(
			string workingFolder,
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "")
		{
			Log.Debug($"Start {memberName} in {workingFolder} ...");
			try
			{
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
				{
					R result = doFunction(gitRepository);

					Log.Debug($"Done  {memberName} in {workingFolder}");

					return result;
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
				return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
			}
		}

		private static Task<R> UseRepoAsync(
			string workingFolder,
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "")
		{
			return Task.Run(() => UseRepo(workingFolder, doFunction, memberName));
		}


		private static async Task<R> UseRepoAsync(
			string workingFolder,
			TimeSpan timeout,
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "")
		{
			CancellationTokenSource cts = new CancellationTokenSource(timeout);

			try
			{
				return await Task.Run(() => UseRepo(workingFolder, doFunction, memberName), cts.Token)
					.WithCancellation(cts.Token);
			}
			catch (OperationCanceledException e)
			{
				Log.Warn($"Timeout for {memberName} in {workingFolder}, {e.Message}");
				Error error = Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
				return error;
			}
		}


		private static Task<R<T>> UseRepoAsync<T>(
			string workingFolder,
			Func<GitRepository, T> doFunction,
			[CallerMemberName] string memberName = "")
		{
			return Task.Run(() => UseRepo(workingFolder, doFunction, memberName));
		}


		private static Task<R<T>> UseRepoAsync<T>(
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