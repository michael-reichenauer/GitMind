using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Diffing;
using GitMind.Features.StatusHandling;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.RepositoryViews;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.Features.Commits.Private
{
	internal class CommitsService : ICommitsService
	{
		private readonly Func<
			BranchName,
			IEnumerable<CommitFile>,
			string,
			bool,
			CommitDialog> commitDialogProvider;


		private readonly IMessage message;
		private readonly IRepositoryCommands repositoryCommands;
		private readonly Func<SetBranchPromptDialog> setBranchPromptDialogProvider;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;
		private readonly IGitCommitService gitCommitService;
		private readonly IDiffService diffService;
		private readonly ILinkService linkService;
		private readonly IRepositoryMgr repositoryMgr;
		private readonly IProgressService progress;
		private readonly IStatusService statusService;
		private readonly IGitStatusService2 gitStatusService2;


		public CommitsService(
			IMessage message,
			IRepositoryCommands repositoryCommands,
			Func<SetBranchPromptDialog> setBranchPromptDialogProvider,
			IGitCommitBranchNameService gitCommitBranchNameService,
			IDiffService diffService,
			ILinkService linkService,
			IRepositoryMgr repositoryMgr,
			IProgressService progressService,
			IStatusService statusService,
			IGitCommitService gitCommitService,
			IGitStatusService2 gitStatusService2,
			Func<
				BranchName,
				IEnumerable<CommitFile>,
				string,
				bool,
				CommitDialog> commitDialogProvider)
		{
			this.commitDialogProvider = commitDialogProvider;
			this.gitCommitService = gitCommitService;
			this.gitStatusService2 = gitStatusService2;
			this.message = message;
			this.repositoryCommands = repositoryCommands;
			this.setBranchPromptDialogProvider = setBranchPromptDialogProvider;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
			this.diffService = diffService;
			this.linkService = linkService;
			this.repositoryMgr = repositoryMgr;
			this.progress = progressService;
			this.statusService = statusService;
		}


		public async Task CommitChangesAsync(string mergeCommitMessage = null)
		{
			Repository repository = repositoryMgr.Repository;
			var uncommitted = repository.UnComitted;
			if (uncommitted != null)
			{
				if (uncommitted.HasConflicts)
				{
					message.ShowInfo("There are merge conflicts that needs be resolved before committing.");
					repositoryCommands.ShowCommitDetails();
					return;
				}
			}
			else
			{
				message.ShowInfo("No changes, nothing to commit.");
				return;
			}

			BranchName branchName = repository.CurrentBranch.Name;

			using (statusService.PauseStatusNotifications())
			{
				if (repository.CurrentBranch.IsDetached)
				{
					message.ShowInfo(
						"Current branch is in detached head status.\n" +
						"You must first create or switch to branch before commit.");
					return;
				}

				IEnumerable<CommitFile> commitFiles = Enumerable.Empty<CommitFile>();
				if (repositoryCommands.UnCommited != null)
				{
					CommitDetails commitDetails = await repositoryCommands.UnCommited.FilesTask;
					commitFiles = commitDetails.Files;
				}

				string commitMessage = mergeCommitMessage ?? repository.Status.MergeMessage;

				CommitDialog dialog = commitDialogProvider(
					branchName,
					commitFiles,
					commitMessage,
					repository.Status.IsMerging);

				if (dialog.ShowDialog() == true)
				{
					using (progress.ShowDialog($"Committing current branch {branchName} ..."))
					{
						R<GitCommit> gitCommit = await CommitAsync(
							dialog.CommitMessage, branchName, dialog.CommitFiles);

						if (!gitCommit.IsOk)
						{
							message.ShowWarning("Failed to commit");
						}
					}

					Log.Debug("After commit dialog, refresh done");
				}
				else if (repository.Status.IsMerging && !commitFiles.Any())
				{
					await gitStatusService2.UndoAllUncommittedAsync(CancellationToken.None);
				}
			}
		}



		public async Task UnCommitAsync(Commit commit)
		{
			using (progress.ShowDialog($"Uncommitting in {commit} ..."))
			{
				R result = await gitCommitService.UnCommitAsync(CancellationToken.None);

				if (result.IsFaulted)
				{
					message.ShowWarning($"Failed to uncommit.\n{result.Message}");
				}
			}
		}

		public async Task UndoCommitAsync(Commit commit)
		{
			using (progress.ShowDialog($"Undo commit {commit} ..."))
			{
				R result = await gitCommitService.UndoCommitAsync(commit.RealCommitSha.Sha, CancellationToken.None);

				if (result.IsFaulted)
				{
					message.ShowWarning($"Failed to undo.\n{result.Message}");
				}
			}
		}


		public Links GetIssueLinks(string text) => linkService.ParseIssues(text);
		public Links GetTagLinks(string text) => linkService.ParseTags(text);


		public bool CanUnCommit(Commit commit)
		{
			return commit != null
				&& !commit.IsUncommitted
				&& commit.IsCurrent
				&& commit.IsLocalAhead
				&& commit.Branch.LocalAheadCount > 0;
		}


		public bool CanUndoCommit(Commit commit)
		{
			return commit != null && !commit.IsUncommitted;
		}


		public async Task EditCommitBranchAsync(Commit commit)
		{
			SetBranchPromptDialog dialog = setBranchPromptDialogProvider();
			dialog.PromptText = commit.SpecifiedBranchName;
			dialog.IsAutomatically = commit.SpecifiedBranchName == null;
			foreach (Branch childBranch in commit.Branch.GetChildBranches())
			{
				if (!childBranch.IsMultiBranch && !childBranch.Name.StartsWith("_"))
				{
					dialog.AddBranchName(childBranch.Name);
				}
			}

			using (statusService.PauseStatusNotifications(Refresh.Repo))
			{
				if (dialog.ShowDialog() == true)
				{
					BranchName branchName = dialog.IsAutomatically ? null : dialog.PromptText?.Trim();

					if (commit.SpecifiedBranchName != branchName)
					{
						using (progress.ShowDialog($"Setting commit branch name {branchName} ..."))
						{
							await SetSpecifiedCommitBranchAsync(
								commit.RealCommitSha, commit.Repository.RootCommit.RealCommitSha, branchName);
							if (branchName != null)
							{
								repositoryCommands.ShowBranch(branchName);
							}
						}
					}
				}
			}
		}


		public async Task UndoUncommittedChangesAsync()
		{
			using (statusService.PauseStatusNotifications())
			using (progress.ShowDialog("Undoing changes in working folder ..."))
			{
				await gitStatusService2.UndoAllUncommittedAsync(CancellationToken.None);
			}
		}


		public async Task CleanWorkingFolderAsync()
		{
			R<IReadOnlyList<string>> failedPaths;

			using (statusService.PauseStatusNotifications())
			using (progress.ShowDialog("Cleaning untracked/ignored files in working folder  ..."))
			{
				failedPaths = await gitStatusService2.CleanWorkingFolderAsync(CancellationToken.None);
			}

			if (failedPaths.IsFaulted)
			{
				message.ShowWarning(failedPaths.ToString());
			}
			else if (failedPaths.Value.Any())
			{
				int count = failedPaths.Value.Count;
				string text = $"Failed to clean working folder.\n{count} items where locked:\n";
				foreach (string path in failedPaths.Value.Take(10))
				{
					text += $"\n   {path}";
				}
				if (count > 10)
				{
					text += "   ...";
				}

				message.ShowWarning(text);
			}
		}


		public async Task ShowUncommittedDiffAsync()
		{
			if (repositoryMgr.Repository.UnComitted == null)
			{
				message.ShowInfo("There are no uncommitted changes");
				return;
			}

			await diffService.ShowDiffAsync(CommitSha.Uncommitted);
		}


		public async Task UndoUncommittedFileAsync(string path)
		{
			using (progress.ShowDialog($"Undoing file change in {path} ..."))
			{
				await gitStatusService2.UndoUncommittedFileAsync(path, CancellationToken.None);
			}
		}



		public Task SetSpecifiedCommitBranchAsync(
			CommitSha commitSha, CommitSha rootSha, BranchName branchName)
		{
			return gitCommitBranchNameService.EditCommitBranchNameAsync(commitSha, rootSha, branchName);
		}

		private async Task<R<GitCommit>> CommitAsync(
			string message, string branchName, IReadOnlyList<CommitFile> paths)
		{
			Log.Debug($"Commit {paths.Count} files: {message} ...");

			R<GitCommit> commit = await gitCommitService.CommitAllChangesAsync(message, CancellationToken.None);
			if (commit.IsOk)
			{
				CommitSha commitSha = commit.Value.Sha;
				await gitCommitBranchNameService.SetCommitBranchNameAsync(commitSha, branchName);
			}

			return commit;
		}
	}
}