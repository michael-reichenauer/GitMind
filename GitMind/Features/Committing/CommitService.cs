using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Diffing;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.MainWindowViews;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Features.Committing
{
	internal class CommitService : ICommitService
	{
		private readonly Func<
			BranchName,
			IEnumerable<CommitFile>,
			string,
			bool,
			CommitDialog> commitDialogProvider;


		private readonly IMessage message;
		private readonly Lazy<IRepositoryCommands> repositoryCommands;
		private readonly IGitCommitsService gitCommitsService;
		private readonly IDiffService diffService;
		private readonly IProgressService progress;


		public CommitService(
			IMessage message,
			Lazy<IRepositoryCommands> repositoryCommands,
			IGitCommitsService gitCommitsService,
			IDiffService diffService,
			IProgressService progressService,
			Func<
				BranchName,
				IEnumerable<CommitFile>,
				string,
				bool,
				CommitDialog> commitDialogProvider)
		{
			this.commitDialogProvider = commitDialogProvider;
			this.message = message;
			this.repositoryCommands = repositoryCommands;
			this.gitCommitsService = gitCommitsService;
			this.diffService = diffService;
			this.progress = progressService;
		}


		public async Task CommitChangesAsync()
		{
			Repository repository = repositoryCommands.Value.Repository;
			BranchName branchName = repository.CurrentBranch.Name;

			using (repositoryCommands.Value.DisableStatus())
			{
				if (repository.CurrentBranch.IsDetached)
				{
					message.ShowInfo(
						"Current branch is in detached head status.\n" +
						"You must first create or switch to branch before commit.");
					return;
				}

				IEnumerable<CommitFile> commitFiles = Enumerable.Empty<CommitFile>();
				if (repositoryCommands.Value.UnCommited != null)
				{
					commitFiles = await repositoryCommands.Value.UnCommited.FilesTask;
				}

				string commitMessage = repository.Status.Message;

				CommitDialog dialog = commitDialogProvider(
					branchName,
					commitFiles,
					commitMessage,
					repository.Status.IsMerging);

				if (dialog.ShowDialog() == true)
				{
					progress.Show($"Commit current branch {branchName} ...", async () =>
					{
						R<GitCommit> gitCommit = await gitCommitsService.CommitAsync(
							dialog.CommitMessage, branchName, dialog.CommitFiles);

						if (gitCommit.HasValue)
						{
							await repositoryCommands.Value.RefreshAfterCommandAsync(false);
						}
						else
						{
							message.ShowWarning("Failed to commit");
						}
					});

					Log.Debug("After commit dialog, refresh done");
				}
				else if (dialog.IsChanged)
				{
					progress.Show("Updating status ...", async () =>
					{
						await repositoryCommands.Value.RefreshAfterCommandAsync(false);
					});
				}
				else if (repository.Status.IsMerging && !commitFiles.Any())
				{
					await gitCommitsService.ResetMerge();
				}
			}
		}


		public Task UnCommitAsync(Commit commit)
		{
			progress.Show($"Uncommit in {commit} ...", async state =>
			{
				R result = await gitCommitsService.UnCommitAsync();

				if (result.IsFaulted)
				{
					message.ShowWarning($"Failed to uncommit.\n{result.Error.Exception.Message}");
				}

				state.SetText("Update status after uncommit ...");
				await repositoryCommands.Value.RefreshAfterCommandAsync(true);
			});

			return Task.CompletedTask;
		}


		public async Task ShowUncommittedDiff()
		{
			await diffService.ShowDiffAsync(Commit.UncommittedId);
		}


		public Task UndoUncommittedFileAsync(string path)
		{
			progress.Show($"Undo file change in {path} ...", async () =>
			{
				await gitCommitsService.UndoFileInWorkingFolderAsync(path);
			});

			return Task.CompletedTask;
		}
	}
}