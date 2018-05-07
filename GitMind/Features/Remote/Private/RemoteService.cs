using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Branches.Private;
using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.Features.Remote.Private
{
	internal class RemoteService : IRemoteService
	{
		private readonly IRepositoryMgr repositoryMgr;
		private readonly IProgressService progress;
		private readonly IMessage message;
		private readonly IStatusService statusService;

		private readonly IGitFetchService gitFetchService;
		private readonly IGitMergeService2 gitMergeService2;
		private readonly IGitPushService gitPushService;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;


		public RemoteService(
			IRepositoryMgr repositoryMgr,
			IProgressService progress,
			IMessage message,
			IStatusService statusService,
			IGitFetchService gitFetchService,
			IGitMergeService2 gitMergeService2,
			IGitPushService gitPushService,
			IGitCommitBranchNameService gitCommitBranchNameService)
		{
			this.repositoryMgr = repositoryMgr;
			this.progress = progress;
			this.message = message;
			this.statusService = statusService;
			this.gitFetchService = gitFetchService;
			this.gitMergeService2 = gitMergeService2;
			this.gitPushService = gitPushService;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
		}

		private Repository Repository => repositoryMgr.Repository;


		public async Task<R> FetchAsync()
		{
			return await gitFetchService.FetchAsync(CancellationToken.None);
		}


		public async Task<R> PushBranchAsync(BranchName branchName)
		{
			return await gitPushService.PushBranchAsync(branchName, CancellationToken.None);
		}


		public Task PushNotesAsync(CommitSha rootId)
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

			using (statusService.PauseStatusNotifications())
			using (progress.ShowDialog("Updating all branches ..."))
			{
				Branch currentBranch = Repository.CurrentBranch;

				R result = await FetchAsync();

				if (result.IsOk && currentBranch.CanBeUpdated)
				{
					progress.SetText($"Updating current branch {currentBranch.Name} ...");
					result = await MergeCurrentBranchAsync();
				}

				if (result.IsFaulted)
				{
					message.ShowWarning(
						$"Failed to update current branch {currentBranch.Name}\n{result.Message}.");
				}

				IEnumerable<Branch> updatableBranches = Repository.Branches
					.Where(b => !b.IsCurrentBranch && b.CanBeUpdated)
					.ToList();

				foreach (Branch branch in updatableBranches)
				{
					progress.SetText($"Updating branch {branch.Name} ...");
					string[] refspecs = { $"{branch.Name}:{branch.Name}" };
					await gitFetchService.FetchRefsAsync(refspecs, CancellationToken.None);
				}

				progress.SetText("Updating all branches ...");
				await FetchAllNotesAsync();
			}
		}


		public async Task PullCurrentBranchAsync()
		{
			using (statusService.PauseStatusNotifications())
			{
				BranchName branchName = Repository.CurrentBranch.Name;
				using (progress.ShowDialog($"Updating current branch {branchName} ..."))
				{
					R result = await FetchAsync();
					if (result.IsOk)
					{
						result = await MergeCurrentBranchAsync();

						await FetchAllNotesAsync();
					}

					if (result.IsFaulted)
					{
						message.ShowWarning(
							$"Failed to update current branch {branchName}.\n{result.Message}");
					}
				}
			}
		}


		public bool CanExecutePullCurrentBranch()
		{
			return Repository.CurrentBranch.CanBeUpdated;
		}

		public async Task PushCurrentBranchAsync()
		{
			BranchName branchName = Repository.CurrentBranch.Name;

			using (statusService.PauseStatusNotifications())
			using (progress.ShowDialog($"Pushing current branch {branchName} ..."))
			{
				await PushNotesAsync(Repository.RootCommit.RealCommitSha);

				R result = await gitPushService.PushAsync(CancellationToken.None);

				if (!result.IsOk)
				{
					message.ShowWarning($"Failed to push current branch {branchName}.\n{result.AllMessages}");
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
			using (statusService.PauseStatusNotifications())
			using (progress.ShowDialog("Pushing all branches ..."))
			{
				Branch currentBranch = Repository.CurrentBranch;

				await PushNotesAsync(Repository.RootCommit.RealCommitSha);

				R result = R.Ok;
				if (currentBranch.CanBePushed)
				{
					progress.SetText($"Pushing current branch {currentBranch.Name} ...");
					result = await gitPushService.PushAsync(CancellationToken.None);
				}

				if (result.IsFaulted)
				{
					message.ShowWarning(
						$"Failed to push current branch {currentBranch.Name}.\n{result.Message}");
				}

				IEnumerable<Branch> pushableBranches = Repository.Branches
				.Where(b => !b.IsCurrentBranch && b.CanBePushed)
				.ToList();

				foreach (Branch branch in pushableBranches)
				{
					progress.SetText($"Pushing branch {branch.Name} ...");

					await PushBranchAsync(branch.Name);
				}
			}
		}


		public bool CanExecuteTryPushAllBranches()
		{
			return Repository.Branches.Any(b => b.CanBePushed);
		}



		private async Task<R> MergeCurrentBranchAsync()
		{
			R<bool> ffResult = await gitMergeService2.TryMergeFastForwardAsync(null, CancellationToken.None);
			if (ffResult.IsFaulted)
			{
				return R.Error("Failed to merge current branch", ffResult.Exception);
			}

			if (!ffResult.Value)
			{
				R result = await gitMergeService2.MergeAsync(null, CancellationToken.None);
				if (result.IsFaulted)
				{
					return R.Error("Failed to merge current branch", ffResult.Exception);
				}
			}

			return R.Ok;
		}
	}
}