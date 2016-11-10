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
		private readonly Lazy<IRepositoryCommands> lazyRepositoryCommands;
		private readonly IProgressService progress;
		private readonly IMessage message;
		private readonly IGitBranchService gitBranchService;
		private readonly IGitNetworkService gitNetworkService;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;

		

		public RemoteService(
			Lazy<IRepositoryCommands> repositoryCommands,
			IProgressService progress,
			IMessage message,
			IGitBranchService gitBranchService,
			IGitNetworkService gitNetworkService,
			IGitCommitBranchNameService gitCommitBranchNameService)
		{
			this.lazyRepositoryCommands = repositoryCommands;
			this.progress = progress;
			this.message = message;
			this.gitBranchService = gitBranchService;
			this.gitNetworkService = gitNetworkService;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
		}

		private Repository Repository => repositoryCommands.Repository;

		private IRepositoryCommands repositoryCommands => lazyRepositoryCommands.Value;


		public Task<R> FetchAsync()
		{
			return gitNetworkService.FetchAsync();
		}


		public Task<R> FetchBranchAsync(BranchName branchName)
		{
			return gitNetworkService.FetchBranchAsync(branchName);
		}


		public Task<R> PushCurrentBranchAsync()
		{
			return gitNetworkService.PushCurrentBranchAsync();
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


		public void TryUpdateAllBranches()
		{
			Log.Debug("Try update all branches");

			using (repositoryCommands.DisableStatus())
			{
				progress.Show("Update all branches ...", async state =>
				{
					Branch currentBranch = Repository.CurrentBranch;

					R result = await FetchAsync();

					if (result.IsOk && currentBranch.CanBeUpdated)
					{
						state.SetText($"Update current branch {currentBranch.Name} ...");
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
						state.SetText($"Update branch {branch.Name} ...");

						await FetchBranchAsync(branch.Name);
					}

					state.SetText("Update all branches ...");
					await FetchAllNotesAsync();

					state.SetText($"Update status after update all branches ...");
					await repositoryCommands.RefreshAfterCommandAsync(false);
				});
			}
		}


		public void PullCurrentBranch()
		{
			using (repositoryCommands.DisableStatus())
			{
				BranchName branchName = Repository.CurrentBranch.Name;
				progress.Show($"Update current branch {branchName} ...", async state =>
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

					state.SetText($"Update status after pull current branch {branchName} ...");
					await repositoryCommands.RefreshAfterCommandAsync(false);
				});
			}
		}


		public bool CanExecutePullCurrentBranch()
		{
			return Repository.CurrentBranch.CanBeUpdated;
		}

		public void PushCurrentBranch()
		{
			using (repositoryCommands.DisableStatus())
			{
				BranchName branchName = Repository.CurrentBranch.Name;
				progress.Show($"Push current branch {branchName} ...", async state =>
				{
					await PushNotesAsync(Repository.RootId);

					R result = await PushCurrentBranchAsync();

					if (result.IsFaulted)
					{
						message.ShowWarning(
							 $"Failed to push current branch {branchName}.\n{result.Error.Exception.Message}");
					}

					state.SetText($"Updating status after push {branchName} ...");
					await repositoryCommands.RefreshAfterCommandAsync(true);
				});
			}
		}


		public bool CanExecutePushCurrentBranch()
		{
			return Repository.CurrentBranch.CanBePushed;
		}


		public void TryPushAllBranches()
		{
			Log.Debug("Try push all branches");
			using (repositoryCommands.DisableStatus())
			{
				progress.Show("Push all branches ...", async state =>
				{
					Branch currentBranch = Repository.CurrentBranch;

					await PushNotesAsync(Repository.RootId);

					R result = R.Ok;
					if (currentBranch.CanBePushed)
					{
						state.SetText($"Push current branch {currentBranch.Name} ...");
						result = await PushCurrentBranchAsync();
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
						state.SetText($"Push branch {branch.Name} ...");

						await PushBranchAsync(branch.Name);
					}

					state.SetText("Update status after push all branches ...");
					await repositoryCommands.RefreshAfterCommandAsync(true);
				});
			}
		}


		public bool CanExecuteTryPushAllBranches()
		{
			return Repository.Branches.Any(b => b.CanBePushed);
		}

	}
}