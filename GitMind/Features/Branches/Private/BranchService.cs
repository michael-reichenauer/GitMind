using System;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Commits;
using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.MainWindowViews;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Features.Branches.Private
{
	/// <summary>
	/// Branch service
	/// </summary>
	internal class BranchService : IBranchService
	{
		private readonly IGitBranchService gitBranchService;
		private readonly IGitNetworkService gitNetworkService;
		private readonly ICommitsService commitsService;
		private readonly IProgressService progress;
		private readonly IMessage message;
		private readonly WindowOwner owner;
		private readonly IRepositoryCommands repositoryCommands;
		private readonly Lazy<IRepositoryService> repositoryService;
		private readonly IStatusService statusService;


		public BranchService(
			IGitBranchService gitBranchService,
			IGitNetworkService gitNetworkService,
			ICommitsService commitsService,
			IProgressService progressService,
			IMessage message,
			WindowOwner owner,
			IRepositoryCommands repositoryCommands,
			Lazy<IRepositoryService> repositoryService,
			IStatusService statusService)
		{
			this.gitBranchService = gitBranchService;
			this.gitNetworkService = gitNetworkService;
			this.commitsService = commitsService;
			this.progress = progressService;
			this.message = message;
			this.owner = owner;
			this.repositoryCommands = repositoryCommands;
			this.repositoryService = repositoryService;
			this.statusService = statusService;
		}


		public Task CreateBranchAsync(Branch branch)
		{
			return CreateBranchFromCommitAsync(branch.TipCommit);
		}


		public async Task CreateBranchFromCommitAsync(Commit commit)
		{
			using (statusService.PauseStatusNotifications())
			{
				CrateBranchDialog dialog = new CrateBranchDialog(owner);

				if (dialog.ShowDialog() == true)
				{
					BranchName branchName = dialog.BranchName;
					Log.Debug($"Create branch {dialog.BranchName}, from {commit.Branch} ...");

					using (progress.ShowDialog($"Creating branch {dialog.BranchName} ..."))
					{
						R result = await gitBranchService.CreateBranchAsync(branchName, commit.RealCommitSha);
						if (result.IsOk)
						{
							Log.Debug($"Created branch {branchName}, from {commit.Branch}");

							if (dialog.IsPublish)
							{
								progress.SetText($"Publishing branch {dialog.BranchName}...");

								R publish = await gitNetworkService.PublishBranchAsync(branchName);
								if (publish.IsFaulted)
								{
									message.ShowWarning($"Failed to publish the branch {branchName}.");
								}
							}

							repositoryCommands.ShowBranch(branchName);
						}
						else
						{
							message.ShowWarning($"Failed to create branch {branchName}\n{result.Message}");
						}
					}
				}
			}
		}


		public async Task PublishBranchAsync(Branch branch)
		{
			using (statusService.PauseStatusNotifications())
			using (progress.ShowDialog($"Publishing branch {branch.Name} ..."))
			{
				R publish = await gitNetworkService.PublishBranchAsync(branch.Name);

				if (publish.IsFaulted)
				{
					message.ShowWarning($"Failed to publish the branch {branch.Name}.\n{publish.Message}");
				}
			}
		}


		public async Task PushBranchAsync(Branch branch)
		{
			using (statusService.PauseStatusNotifications())
			using (progress.ShowDialog($"Pushing branch {branch.Name} ..."))
			{
				R result = await gitNetworkService.PushBranchAsync(branch.Name);

				if (result.IsFaulted)
				{
					message.ShowWarning($"Failed to push the branch {branch.Name}.\n{result.Message}");
				}
			}
		}


		public async Task UpdateBranchAsync(Branch branch)
		{
			using (statusService.PauseStatusNotifications())
			using (progress.ShowDialog($"Updating branch {branch.Name} ..."))
			{
				R result;
				if (branch == branch.Repository.CurrentBranch ||
					branch.IsMainPart && branch.LocalSubBranch == branch.Repository.CurrentBranch)
				{
					Log.Debug("Update current branch");
					result = await gitNetworkService.FetchAsync();
					if (result.IsOk)
					{
						result = await gitBranchService.MergeCurrentBranchAsync();
					}
				}
				else
				{
					Log.Debug($"Update branch {branch.Name}");
					result = await gitNetworkService.FetchBranchAsync(branch.Name);
				}

				if (result.IsFaulted)
				{
					message.ShowWarning($"Failed to update the branch {branch.Name}.\n{result.Message}");
				}
			}
		}


		public async Task SwitchBranchAsync(Branch branch)
		{
			using (statusService.PauseStatusNotifications())
			using (progress.ShowDialog($"Switching to branch {branch.Name} ..."))
			{
				R result = await gitBranchService.SwitchToBranchAsync(branch.Name);
				if (result.IsFaulted)
				{
					message.ShowWarning($"Failed to switch,\n{result.Message}");
				}
			}
		}


		public bool CanExecuteSwitchBranch(Branch branch)
		{
			return
				branch.Repository.Status.ConflictCount == 0
				&& !branch.Repository.Status.IsMerging
				&& !branch.IsCurrentBranch;
		}



		public async Task SwitchToBranchCommitAsync(Commit commit)
		{
			if (commit.IsRemoteAhead)
			{
				message.ShowInfo("Commit is remote, you must first update before switching to this commit.");
				return;
			}


			using (statusService.PauseStatusNotifications(Refresh.Repo))
			using (progress.ShowDialog("Switching to commit ..."))
			{
				BranchName branchName = commit == commit.Branch.TipCommit ? commit.Branch.Name : null;

				R<BranchName> switchedNamed = await gitBranchService.SwitchToCommitAsync(commit.RealCommitSha, branchName);

				if (switchedNamed.IsOk)
				{
					repositoryCommands.ShowBranch(switchedNamed.Value);
				}
				else
				{
					// Show current branch
					message.ShowWarning($"Failed to switch to the branch {branchName}.\n{switchedNamed.Message}");
					repositoryCommands.ShowBranch((BranchName)null);
				}
			}
		}


		public bool CanExecuteSwitchToBranchCommit(Commit commit)
		{
			return
				commit.Repository.Status.ChangedCount == 0
				&& !commit.Repository.Status.IsMerging
				&& commit.Repository.Status.ConflictCount == 0;
		}


		public async Task DeleteBranchAsync(Branch branch)
		{
			using (statusService.PauseStatusNotifications(Refresh.Repo))
			{
				if (branch.Name == BranchName.Master)
				{
					message.ShowWarning("You cannot delete master branch.");
					return;
				}

				if (!branch.IsRemote && branch == branch.Repository.CurrentBranch)
				{
					message.ShowWarning("You cannot delete current local branch.");
					return;
				}

				DeleteBranchDialog dialog = new DeleteBranchDialog(
					owner,
					branch.Name,
					branch.IsLocal && branch != branch.Repository.CurrentBranch,
					branch.IsRemote);

				if (dialog.ShowDialog() == true)
				{
					if (dialog.IsLocal && branch == branch.Repository.CurrentBranch)
					{
						message.ShowWarning("You cannot delete current local branch.");
						return;
					}

					if (!dialog.IsLocal && !dialog.IsRemote)
					{
						message.ShowWarning("Neither local nor remote branch was selected.");
						return;
					}

					await DeleteBranchAsync(branch, dialog.IsLocal, dialog.IsRemote);
				}
			}
		}


		public bool CanDeleteBranch(Branch branch)
		{
			return branch?.IsActive ?? false;
		}


		private async Task DeleteBranchAsync(
			Branch branch,
			bool isLocal,
			bool isRemote)
		{
			using (progress.ShowDialog())
			{
				if (isLocal)
				{
					progress.SetText($"Deleting local branch {branch.Name} ...");
					await DeleteBranchImplAsync(branch, false);
				}

				if (isRemote)
				{
					progress.SetText($"Deleting remote branch {branch.Name} ...");
					await DeleteBranchImplAsync(branch, true);
				}
			}
		}

		private async Task DeleteBranchImplAsync(
			Branch branch,
			bool isRemote)
		{
			string text = isRemote ? "Remote" : "Local";

			R deleted;
			if (isRemote)
			{
				deleted = await gitNetworkService.DeleteRemoteBranchAsync(branch.Name);
			}
			else
			{
				deleted = await gitBranchService.DeleteLocalBranchAsync(branch.Name);
			}

			if (deleted.IsFaulted)
			{
				message.ShowWarning($"Failed to delete {text} branch '{branch.Name}'\n{deleted.Message}");
			}
		}


		public async Task MergeBranchAsync(Branch branch)
		{
			using (statusService.PauseStatusNotifications())
			{
				if (branch == branch.Repository.CurrentBranch)
				{
					message.ShowWarning("You cannot merge current branch into it self.");
					return;
				}

				if (branch.Repository.Status.ConflictCount > 0 || branch.Repository.Status.ChangedCount > 0)
				{
					message.ShowInfo("You must first commit uncommitted changes before merging.");
					return;
				}

				Branch currentBranch = branch.Repository.CurrentBranch;
				using (progress.ShowDialog($"Merging branch {branch.Name} into {currentBranch.Name} ..."))
				{
					await gitBranchService.MergeAsync(branch.Name);

					repositoryCommands.SetCurrentMerging(branch);
			
					await repositoryService.Value.CheckLocalRepositoryAsync();
				}

				Status status = repositoryService.Value.Repository.Status;
				if (status.ConflictCount == 0)
				{
					await commitsService.CommitChangesAsync();
				}
				else
				{
					repositoryCommands.ShowUncommittedDetails();
				}
			}
		}

		public async Task MergeBranchCommitAsync(Commit commit)
		{
			using (statusService.PauseStatusNotifications())
			{
				if (commit.Branch == commit.Repository.CurrentBranch)
				{
					message.ShowWarning("You cannot merge current branch into it self.");
					return;
				}

				if (commit.Repository.Status.ConflictCount > 0 || commit.Repository.Status.ChangedCount > 0)
				{
					message.ShowInfo("You must first commit uncommitted changes before merging.");
					return;
				}

				Branch currentBranch = commit.Repository.CurrentBranch;
				using (progress.ShowDialog($"Merging branch commit {commit.RealCommitSha.ShortSha} into {currentBranch.Name} ..."))
				{
					await gitBranchService.MergeAsync(commit.RealCommitSha);

					repositoryCommands.SetCurrentMerging(commit.Branch);

					await repositoryService.Value.CheckLocalRepositoryAsync();
				}

				Status status = repositoryService.Value.Repository.Status;
				if (status.ConflictCount == 0)
				{
					await commitsService.CommitChangesAsync();
				}
				else
				{
					repositoryCommands.ShowUncommittedDetails();
				}
			}
		}
	}
}