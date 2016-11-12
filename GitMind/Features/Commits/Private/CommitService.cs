using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Diffing;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Features.Commits.Private
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
		private readonly IRepositoryCommands repositoryCommands;
		private readonly Func<SetBranchPromptDialog> setBranchPromptDialogProvider;
		private readonly IGitCommitsService gitCommitsService;
		private readonly IDiffService diffService;
		private readonly IRepositoryService repositoryService;
		private readonly IProgressService progress;



		public CommitService(
			IMessage message,
			IRepositoryCommands repositoryCommands,
			Func<SetBranchPromptDialog> setBranchPromptDialogProvider,
			IGitCommitsService gitCommitsService,
			IDiffService diffService,
			IRepositoryService repositoryService,
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
			this.setBranchPromptDialogProvider = setBranchPromptDialogProvider;
			this.gitCommitsService = gitCommitsService;
			this.diffService = diffService;
			this.repositoryService = repositoryService;
			this.progress = progressService;
		}


		public async Task CommitChangesAsync()
		{
			Repository repository = repositoryCommands.Repository;
			Commit uncommitted;
			if (repository.Commits.TryGetValue(Commit.UncommittedId, out uncommitted))
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

			using (repositoryCommands.DisableStatus())
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
					commitFiles = await repositoryCommands.UnCommited.FilesTask;
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
							await repositoryCommands.RefreshAfterCommandAsync(false);
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
						await repositoryCommands.RefreshAfterCommandAsync(false);
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
				await repositoryCommands.RefreshAfterCommandAsync(true);
			});

			return Task.CompletedTask;
		}


		public bool CanUnCommit(Commit commit)
		{
			return commit != null
				&& commit.Id != Commit.UncommittedId
				&& commit.IsCurrent
				&& commit.IsLocalAhead;
		}


		public Task EditCommitBranchAsync(Commit commit)
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

			using (repositoryCommands.DisableStatus())
			{
				if (dialog.ShowDialog() == true)
				{
					Git.BranchName branchName = dialog.IsAutomatically ? null : dialog.PromptText?.Trim();

					if (commit.SpecifiedBranchName != branchName)
					{
						progress.Show($"Set commit branch name {branchName} ...", async () =>
						{
							await repositoryService.SetSpecifiedCommitBranchAsync(
								commit.Id, commit.Repository.RootId, branchName);
							if (branchName != null)
							{
								repositoryCommands.ShowBranch(branchName);
							}

							await repositoryCommands.RefreshAfterCommandAsync(true);
						});
					}
				}
			}

			return Task.CompletedTask;
		}


		public Task UndoUncommittedChangesAsync()
		{
			using (repositoryCommands.DisableStatus())
			{
				progress.Show($"Undo changes in working folder ...", async () =>
				{
					await gitCommitsService.UndoWorkingFolderAsync();

					await repositoryCommands.RefreshAfterCommandAsync(false);
				});
			}

			return Task.CompletedTask;
		}


		public Task UndoCleanWorkingFolderAsync()
		{
			R<IReadOnlyList<string>> failedPaths = R.From(new string[0].AsReadOnlyList());

			using (repositoryCommands.DisableStatus())
			{
				progress.Show($"Undo changes and clean working folder  ...", async () =>
				{
					failedPaths = await gitCommitsService.UndoCleanWorkingFolderAsync();

					await repositoryCommands.RefreshAfterCommandAsync(false);
				});

				if (failedPaths.IsFaulted)
				{
					message.ShowWarning(failedPaths.ToString());
				}
				else if (failedPaths.Value.Any())
				{
					int count = failedPaths.Value.Count;
					string text = $"Failed to undo and clean working folder.\n{count} items where locked:\n";
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

			return Task.CompletedTask;
		}


		public async Task ShowUncommittedDiffAsync()
		{
			if (!repositoryCommands.Repository.Commits.Contains(Commit.UncommittedId))
			{
				message.ShowInfo("There are no uncommitted changes");
				return;
			}

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