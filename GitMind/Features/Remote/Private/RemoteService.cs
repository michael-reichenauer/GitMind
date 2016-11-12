using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Branches.Private;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Features.Remote.Private
{
	internal class RemoteService : IRemoteService
	{
		private readonly IRepositoryCommands repositoryCommands;
		private readonly IProgressService progress;
		private readonly IMessage message;
		private readonly IGitBranchService gitBranchService;
		private readonly IGitNetworkService gitNetworkService;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;

		

		public RemoteService(
			IRepositoryCommands repositoryCommands,
			IProgressService progress,
			IMessage message,
			IGitBranchService gitBranchService,
			IGitNetworkService gitNetworkService,
			IGitCommitBranchNameService gitCommitBranchNameService)
		{
			this.repositoryCommands = repositoryCommands;
			this.progress = progress;
			this.message = message;
			this.gitBranchService = gitBranchService;
			this.gitNetworkService = gitNetworkService;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
		}

		private Repository Repository => repositoryCommands.Repository;


		public Task<R> FetchAsync()
		{
			return gitNetworkService.FetchAsync();
		}



		public Task<R> PushBranchAsync(BranchName branchName)
		{
			return gitNetworkService.PushBranchAsync(branchName);
		}


		public Task PushNotesAsync(string rootId)
		{
			return gitCommitBranchNameService.PushNotesAsync(rootId);
		}


		public Task<R> FetchAllNotesAsync()
		{
			return gitCommitBranchNameService.FetchAllNotesAsync();
		}


		public bool CanExecuteTryUpdateAllBranches()
		{
			return Repository?.Branches.Any(b => b.CanBeUpdated) ?? false;
		}


		public async Task TryUpdateAllBranchesAsync()
		{
			Log.Debug("Try update all branches");

			using (repositoryCommands.DisableStatus())
			{
				using (progress.ShowDialog("Update all branches ..."))
				{
					Branch currentBranch = Repository.CurrentBranch;

					R result = await FetchAsync();

					if (result.IsOk && currentBranch.CanBeUpdated)
					{
						progress.SetText($"Update current branch {currentBranch.Name} ...");
						result = await gitBranchService.MergeCurrentBranchAsync();
					}

					if (result.IsFaulted)
					{
						message.ShowWarning(
							$"Failed to update current branch {currentBranch.Name}\n{result.Error.Exception.Message}.");
					}

					IEnumerable<Branch> updatableBranches = Repository.Branches
						.Where(b => !b.IsCurrentBranch && b.CanBeUpdated)
						.ToList();

					foreach (Branch branch in updatableBranches)
					{
						progress.SetText($"Update branch {branch.Name} ...");

						await gitNetworkService.FetchBranchAsync(branch.Name);
					}

					progress.SetText("Update all branches ...");
					await FetchAllNotesAsync();

					progress.SetText($"Update status after update all branches ...");
					await repositoryCommands.RefreshAfterCommandAsync(false);
				}
			}
		}


		public async Task PullCurrentBranchAsync()
		{
			using (repositoryCommands.DisableStatus())
			{
				BranchName branchName = Repository.CurrentBranch.Name;
				using (progress.ShowDialog($"Update current branch {branchName} ..."))
				{
					R result = await FetchAsync();
					if (result.IsOk)
					{
						result = await gitBranchService.MergeCurrentBranchAsync();

						await FetchAllNotesAsync();
					}

					if (result.IsFaulted)
					{
						message.ShowWarning(
							$"Failed to update current branch {branchName}.\n{result.Error.Exception.Message}");
					}

					progress.SetText($"Update status after pull current branch {branchName} ...");
					await repositoryCommands.RefreshAfterCommandAsync(false);
				}
			}
		}


		public bool CanExecutePullCurrentBranch()
		{
			return Repository.CurrentBranch.CanBeUpdated;
		}

		public async Task PushCurrentBranchAsync()
		{
			using (repositoryCommands.DisableStatus())
			{
				BranchName branchName = Repository.CurrentBranch.Name;
				using (progress.ShowDialog($"Push current branch {branchName} ..."))
				{
					await PushNotesAsync(Repository.RootId);

					R result = await gitNetworkService.PushCurrentBranchAsync();

					if (result.IsFaulted)
					{
						message.ShowWarning(
							 $"Failed to push current branch {branchName}.\n{result.Error.Exception.Message}");
					}

					progress.SetText($"Updating status after push {branchName} ...");
					await repositoryCommands.RefreshAfterCommandAsync(true);
				}
			}
		}


		public bool CanExecutePushCurrentBranch()
		{
			return Repository.CurrentBranch.CanBePushed;
		}


		public async Task TryPushAllBranchesAsync()
		{
			Log.Debug("Try push all branches");
			using (repositoryCommands.DisableStatus())
			{
				using (progress.ShowDialog("Push all branches ..."))
				{
					Branch currentBranch = Repository.CurrentBranch;

					await PushNotesAsync(Repository.RootId);

					R result = R.Ok;
					if (currentBranch.CanBePushed)
					{
						progress.SetText($"Push current branch {currentBranch.Name} ...");
						result = await gitNetworkService.PushCurrentBranchAsync();
					}

					if (result.IsFaulted)
					{
						message.ShowWarning(
							$"Failed to push current branch {currentBranch.Name}.\n{result.Error.Exception.Message}");
					}

					IEnumerable<Branch> pushableBranches = Repository.Branches
						.Where(b => !b.IsCurrentBranch && b.CanBePushed)
						.ToList();

					foreach (Branch branch in pushableBranches)
					{
						progress.SetText($"Push branch {branch.Name} ...");

						await PushBranchAsync(branch.Name);
					}

					progress.SetText("Update status after push all branches ...");
					await repositoryCommands.RefreshAfterCommandAsync(true);
				}
			}
		}


		public bool CanExecuteTryPushAllBranches()
		{
			return Repository.Branches.Any(b => b.CanBePushed);
		}
	}
}