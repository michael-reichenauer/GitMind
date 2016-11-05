using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Features.Committing
{
	internal class CommitService : ICommitService
	{
		private readonly Func<
			Window,
			IRepositoryCommands,
			BranchName,
			IEnumerable<CommitFile>,
			string,
			bool,
			CommitDialog> commitDialogProvider;

		private readonly WorkingFolder workingFolder;
		private readonly IGitCommitsService gitCommitsService;
		private readonly IDiffService diffService;


		public CommitService(
			WorkingFolder workingFolder,
			IGitCommitsService gitCommitsService,
			IDiffService diffService,
			Func<
				Window, 
				IRepositoryCommands, 
				BranchName, 
				IEnumerable<CommitFile>, 
				string,
				bool,
				CommitDialog> commitDialogProvider)
		{
			this.commitDialogProvider = commitDialogProvider;
			this.workingFolder = workingFolder;
			this.gitCommitsService = gitCommitsService;
			this.diffService = diffService;
		}


		public async Task CommitChangesAsync(IRepositoryCommands repositoryCommands)
		{
			Window owner = repositoryCommands.Owner;
			Repository repository = repositoryCommands.Repository;
			BranchName branchName = repository.CurrentBranch.Name;

			using (repositoryCommands.DisableStatus())
			{
				if (repository.CurrentBranch.IsDetached)
				{
					Message.ShowInfo(owner,
						"Current branch is in detached head status.\n" +
						"You must first create or switch to branch before commit.");
					return;
				}

				IEnumerable<CommitFile> commitFiles = Enumerable.Empty<CommitFile>();
				if (repositoryCommands.UnCommited != null)
				{
					commitFiles = await repositoryCommands.UnCommited.FilesTask;
				}

				string commitMessage = repository.Status.Message;

				CommitDialog dialog = commitDialogProvider(
					owner,
					repositoryCommands,
					branchName,
					commitFiles,
					commitMessage,
					repository.Status.IsMerging);

				if (dialog.ShowDialog() == true)
				{
					Progress.ShowDialog(owner, $"Commit current branch {branchName} ...", async () =>
					{
						R<GitCommit> gitCommit = await gitCommitsService.CommitAsync(
							workingFolder, dialog.CommitMessage, branchName, dialog.CommitFiles);

						if (gitCommit.HasValue)
						{
							await repositoryCommands.RefreshAfterCommandAsync(false);
						}
						else
						{
							Message.ShowWarning(owner, "Failed to commit");
						}
					});

					Log.Debug("After commit dialog, refresh done");
				}
				else if (dialog.IsChanged)
				{
					Progress.ShowDialog(owner, "Updating status ...", async () =>
					{
						await repositoryCommands.RefreshAfterCommandAsync(false);
					});
				}
				else if (repository.Status.IsMerging && !commitFiles.Any())
				{
					await gitCommitsService.ResetMerge(workingFolder);
				}
			}
		}


		public Task UnCommitAsync(IRepositoryCommands repositoryCommands, Commit commit)
		{
			Window owner = repositoryCommands.Owner;

			Progress.ShowDialog(owner, $"Uncommit in {commit} ...", async progress =>
			{
				R result = await gitCommitsService.UnCommitAsync(workingFolder);

				if (result.IsFaulted)
				{
					Message.ShowWarning(owner, $"Failed to uncommit.\n{result.Error.Exception.Message}");
				}

				progress.SetText("Update status after uncommit ...");
				await repositoryCommands.RefreshAfterCommandAsync(true);
			});

			return Task.CompletedTask;
		}


		public async Task ShowUncommittedDiff(IRepositoryCommands repositoryCommands)
		{
			await diffService.ShowDiffAsync(Commit.UncommittedId, workingFolder);
		}


		public Task UndoUncommittedFileAsync(IRepositoryCommands repositoryCommands, string path)
		{
			Window owner = repositoryCommands.Owner;
			Progress.ShowDialog(owner, $"Undo file change in {path} ...", async () =>
			{
				await gitCommitsService.UndoFileInWorkingFolderAsync(workingFolder, path);
			});

			return Task.CompletedTask;
		}
	}
}