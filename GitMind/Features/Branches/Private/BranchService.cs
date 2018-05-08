using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.StatusHandling;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.MainWindowViews;
using GitMind.RepositoryViews;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using ICommitsService = GitMind.Features.Commits.ICommitsService;


namespace GitMind.Features.Branches.Private
{
	/// <summary>
	/// Branch service
	/// </summary>
	internal class BranchService : IBranchService
	{
		private readonly IGitFetchService gitFetchService;
		private readonly IGitPushService gitPushService;
		private readonly IGitBranchService gitBranchService;
		private readonly IGitMergeService gitMergeService;
		private readonly IGitCommitService gitCommitService;
		private readonly IGitCheckoutService gitCheckoutService;
		private readonly ICommitsService commitsService;
		private readonly IProgressService progress;
		private readonly IMessage message;
		private readonly WindowOwner owner;
		private readonly IRepositoryCommands repositoryCommands;
		private readonly Lazy<IRepositoryService> repositoryService;
		private readonly IStatusService statusService;


		public BranchService(
			IGitFetchService gitFetchService,
			IGitPushService gitPushService,
			IGitBranchService gitBranchService,
			IGitMergeService gitMergeService,
			IGitCommitService gitCommitService,
			IGitCheckoutService gitCheckoutService,
			ICommitsService commitsService,
			IProgressService progressService,
			IMessage message,
			WindowOwner owner,
			IRepositoryCommands repositoryCommands,
			Lazy<IRepositoryService> repositoryService,
			IStatusService statusService)
		{
			this.gitFetchService = gitFetchService;
			this.gitPushService = gitPushService;
			this.gitBranchService = gitBranchService;
			this.gitMergeService = gitMergeService;
			this.gitCommitService = gitCommitService;
			this.gitCheckoutService = gitCheckoutService;
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
						R result = await gitBranchService.BranchFromCommitAsync(
							branchName, commit.RealCommitSha.Sha, true, CancellationToken.None);
						if (result.IsOk)
						{
							Log.Debug($"Created branch {branchName}, from {commit.Branch}");

							if (dialog.IsPublish)
							{
								progress.SetText($"Publishing branch {dialog.BranchName}...");

								R publish = await gitPushService.PushBranchAsync(branchName, CancellationToken.None);
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
				R publish = await gitPushService.PushBranchAsync(branch.Name, CancellationToken.None);

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
				R result = await gitPushService.PushBranchAsync(branch.Name, CancellationToken.None);

				if (result.IsFaulted)
				{
					message.ShowWarning($"Failed to push the branch {branch.Name}.\n{result.AllMessages}");
				}
			}
		}


		public async Task UpdateBranchAsync(Branch branch)
		{
			using (statusService.PauseStatusNotifications())
			using (progress.ShowDialog($"Updating branch {branch.Name} ..."))
			{
				R result = R.NoValue;
				if (branch == branch.Repository.CurrentBranch ||
					branch.IsMainPart && branch.LocalSubBranch == branch.Repository.CurrentBranch)
				{
					Log.Debug("Update current branch");

					if ((await gitFetchService.FetchAsync(CancellationToken.None)).IsOk)
					{
						result = await MergeCurrentBranchAsync();
					}
				}
				else
				{
					Log.Debug($"Update branch {branch.Name}");
					result = await gitFetchService.FetchBranchAsync(branch.Name, CancellationToken.None);
				}

				if (result.IsFaulted)
				{
					message.ShowWarning($"Failed to update the branch {branch.Name}.\n{result.Message}");
				}
			}
		}


		public async Task SwitchBranchAsync(Branch branch)
		{
			using (statusService.PauseStatusNotifications(Refresh.Repo))
			using (progress.ShowDialog($"Switching to branch {branch.Name} ..."))
			{
				R result = await SwitchToBranchAsync(
					branch.Name, branch.TipCommit.RealCommitSha);
				if (result.IsFaulted)
				{
					message.ShowWarning($"Failed to switch,\n{result.Message}");
				}
			}
		}


		public bool CanExecuteSwitchBranch(Branch branch)
		{
			return
				branch.Repository.Status.Conflicted == 0
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

				R<BranchName> switchedNamed = await SwitchToCommitAsync(commit.RealCommitSha, branchName);

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
				commit.Repository.Status.AllChanges == 0
				&& !commit.Repository.Status.IsMerging
				&& commit.Repository.Status.Conflicted == 0;
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

					await DeleteBranchAsync(branch, dialog.IsLocal, dialog.IsRemote, dialog.IsForce);
				}
			}
		}


		public bool CanDeleteBranch(Branch branch)
		{
			return branch?.IsActive ?? false;
		}


		private async Task DeleteBranchAsync(Branch branch, bool isLocal, bool isRemote, bool IsForce)
		{
			using (progress.ShowDialog())
			{
				if (isRemote)
				{
					progress.SetText($"Deleting remote branch {branch.Name} ...");
					await DeleteBranchImplAsync(branch, true, IsForce);
				}

				if (isLocal)
				{
					progress.SetText($"Deleting local branch {branch.Name} ...");
					await DeleteBranchImplAsync(branch, false, IsForce);
				}				
			}
		}


		private async Task DeleteBranchImplAsync(Branch branch, bool isRemote, bool isForce)
		{
			string text = isRemote ? "Remote" : "Local";

			R result;
			if (isRemote)
			{
				result = await gitPushService.PushDeleteRemoteBranchAsync(branch.Name, CancellationToken.None);
			}
			else
			{
				result = await gitBranchService.DeleteLocalBranchAsync(branch.Name, isForce, CancellationToken.None);
			}

			if (result.IsFaulted)
			{
				if (result.Exception == gitBranchService.NotFullyMergedException)
				{
					message.ShowWarning(
						$"Failed to delete {text} local branch '{branch.Name}'\n" + 
						"The branch is not fully merged.\nCheck 'Force' checkbox to force a delete of the branch");
					return;
				}

				message.ShowWarning($"Failed to delete {text} branch '{branch.Name}'\n{result.AllMessages}");
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

				if (branch.Repository.Status.Conflicted > 0 || branch.Repository.Status.AllChanges > 0)
				{
					message.ShowInfo("You must first commit uncommitted changes before merging.");
					return;
				}

				Branch currentBranch = branch.Repository.CurrentBranch;
				using (progress.ShowDialog($"Merging branch {branch.Name} into {currentBranch.Name} ..."))
				{
					await MergeAsync(branch.Name);

					repositoryCommands.SetCurrentMerging(branch, branch.TipCommit.RealCommitSha);

					await repositoryService.Value.CheckLocalRepositoryAsync();
				}

				GitStatus status = repositoryService.Value.Repository.Status;
				if (status.Conflicted == 0)
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

				if (commit.Repository.Status.Conflicted > 0 || commit.Repository.Status.AllChanges > 0)
				{
					message.ShowInfo("You must first commit uncommitted changes before merging.");
					return;
				}

				Branch currentBranch = commit.Repository.CurrentBranch;
				using (progress.ShowDialog($"Merging branch commit {commit.RealCommitSha.ShortSha} into {currentBranch.Name} ..."))
				{
					await gitMergeService.MergeAsync(commit.RealCommitSha.Sha, CancellationToken.None);

					repositoryCommands.SetCurrentMerging(commit.Branch, commit.RealCommitSha);

					await repositoryService.Value.CheckLocalRepositoryAsync();
				}

				GitStatus status = repositoryService.Value.Repository.Status;
				if (status.Conflicted == 0)
				{
					await commitsService.CommitChangesAsync($"Merge branch '{commit.Branch.Name}'");
				}
				else
				{
					repositoryCommands.ShowUncommittedDetails();
				}
			}
		}

		private async Task<R> MergeAsync(BranchName branchName)
		{
			Log.Debug($"Merge branch {branchName} into current branch ...");

			R<IReadOnlyList<GitBranch>> branches = await gitBranchService.GetBranchesAsync(CancellationToken.None);
			if (branches.IsFaulted)
			{
				return R.Error("Failed to merge", branches.Exception);
			}

			// Trying to get both local and remote branch
			branches.Value.TryGet(branchName, out GitBranch localbranch);
			branches.Value.TryGet($"origin/{branchName}", out GitBranch remoteBranch);

			GitBranch branch = localbranch ?? remoteBranch;
			if (localbranch != null && remoteBranch != null)
			{
				// Both local and remote tip exists, use the branch with the most resent tip
				R<GitCommit> localTipCommit = await gitCommitService.GetCommitAsync(localbranch.TipSha.Sha, CancellationToken.None);
				R<GitCommit> remoteTipCommit = await gitCommitService.GetCommitAsync(remoteBranch.TipSha.Sha, CancellationToken.None);

				if (localTipCommit.IsFaulted || remoteTipCommit.IsFaulted)
				{
					return R.Error("Failed to merge", remoteTipCommit.Exception);
				}

				if (remoteTipCommit.Value.CommitDate > localTipCommit.Value.CommitDate)
				{
					branch = remoteBranch;
				}
			}

			if (branch == null)
			{
				return R.Error($"Failed to Merge, not valid branch {branchName}");
			}


			return await gitMergeService.MergeAsync(branch.Name, CancellationToken.None);
		}


		private async Task<R> SwitchToBranchAsync(BranchName branchName, CommitSha tipSha)
		{
			Log.Debug($"Switch to branch {branchName} ...");

			R<bool> checkoutResult = await gitCheckoutService.TryCheckoutAsync(branchName, CancellationToken.None);
			if (checkoutResult.IsFaulted)
			{
				return R.Error("Failed to switch branch", checkoutResult.Exception);
			}

			if (!checkoutResult.Value)
			{
				Log.Debug($"Branch {branchName} does not exist, lets try to create it");

				var branchResult = await gitBranchService.BranchFromCommitAsync(branchName, tipSha.Sha, true, CancellationToken.None);

				if (branchResult.IsFaulted)
				{
					return R.Error("Failed to switch branch", branchResult.Exception);
				}
			}

			return R.Ok;
		}



		private async Task<R<BranchName>> SwitchToCommitAsync(CommitSha commitSha, BranchName branchName)
		{
			Log.Debug($"Switch to commit {commitSha} with branch name '{branchName}' ...");

			R<IReadOnlyList<GitBranch>> branches = await gitBranchService.GetBranchesAsync(CancellationToken.None);
			if (branches.IsFaulted)
			{
				return R.Error("Failed to switch to commit", branches.Exception);
			}

			if (branches.Value.TryGet(branchName, out GitBranch branch))
			{
				if (branch.TipSha == commitSha)
				{
					R checkoutResult = await gitCheckoutService.CheckoutAsync(branchName, CancellationToken.None);
					if (checkoutResult.IsFaulted)
					{
						return R.Error("Failed to switch to commit", checkoutResult.Exception);
					}

					return branchName;
				}
			}

			R checkoutCommitResult = await gitCheckoutService.CheckoutAsync(commitSha.Sha, CancellationToken.None);
			if (checkoutCommitResult.IsFaulted)
			{
				return R.Error("Failed to switch to commit", checkoutCommitResult.Exception);
			}

			return null;
		}


		private async Task<R> MergeCurrentBranchAsync()
		{
			R<bool> ffResult = await gitMergeService.TryMergeFastForwardAsync(null, CancellationToken.None);
			if (ffResult.IsFaulted)
			{
				return R.Error("Failed to merge current branch", ffResult.Exception);
			}

			if (!ffResult.Value)
			{
				R result = await gitMergeService.MergeAsync(null, CancellationToken.None);
				if (result.IsFaulted)
				{
					return R.Error("Failed to merge current branch", ffResult.Exception);
				}
			}

			return R.Ok;
		}

	}
}