using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Features.Committing
{
	internal class CommitService : ICommitService
	{
		private readonly IGitService gitService;
		private readonly IDiffService diffService;


		public CommitService()
			: this(new GitService(), new DiffService())
		{
		}


		public CommitService(
			IGitService gitService,
			IDiffService diffService)
		{
			this.gitService = gitService;
			this.diffService = diffService;
		}


		public async Task CommitChangesAsync(IRepositoryCommands repositoryCommands)
		{
			Window owner = repositoryCommands.Owner;
			Repository repository = repositoryCommands.Repository;
			string branchName = repository.CurrentBranch.Name;
			string workingFolder = repositoryCommands.WorkingFolder;

			using (repositoryCommands.DisableStatus())
			{
				if (repository.CurrentBranch.IsDetached)
				{
					MessageDialog.ShowInfo(owner, 
						"Current branch is in detached head status.\n" +
						"You must first create or switch to branch before commit.");
					return;
				}

				IEnumerable<CommitFile> commitFiles = await repositoryCommands.UnCommited.FilesTask;
				string commitMessage = repository.Status.Message;

				CommitDialog dialog = new CommitDialog(
					owner,
					repositoryCommands,
					branchName,
					workingFolder,
					commitFiles,
					commitMessage,
					repository.Status.IsMerging);

				if (dialog.ShowDialog() == true)
				{
					Progress.ShowDialog(owner, $"Commit current branch {branchName} ...", async () =>
					{
						R<GitCommit> gitCommit = await gitService.CommitAsync(
							workingFolder, dialog.CommitMessage, dialog.CommitFiles);

						if (gitCommit.HasValue)
						{
							await gitService.SetCommitBranchAsync(workingFolder, gitCommit.Value.Id, branchName);

							await repositoryCommands.RefreshAfterCommandAsync(false);
						}
						else
						{
							MessageDialog.ShowWarning(owner, "Failed to commit");
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
			}
		}


		public async Task ShowUncommittedDiff(IRepositoryCommands repositoryCommands)
		{
			string workingFolder = repositoryCommands.WorkingFolder;

			await diffService.ShowDiffAsync(Commit.UncommittedId, workingFolder);
		}


		public Task UndoUncommittedFileAsync(IRepositoryCommands repositoryCommands, string path)
		{
			Window owner = repositoryCommands.Owner;
			string workingFolder = repositoryCommands.WorkingFolder;
			Progress.ShowDialog(owner, $"Undo file change in {path} ...", async () =>
			{
				await gitService.UndoFileInCurrentBranchAsync(workingFolder, path);
			});

			return Task.CompletedTask;
		}
	}
}