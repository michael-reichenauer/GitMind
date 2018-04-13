using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common.ProgressHandling;
using GitMind.GitModel;
using GitMind.MainWindowViews;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.Features.StatusHandling.Private
{
	[SingleInstance]
	internal class StatusService : IStatusService
	{
		private static readonly IReadOnlyList<string> None = Enumerable.Empty<string>().ToReadOnlyList();

		private readonly IFolderMonitorService folderMonitorService;
		private readonly IMainWindowService mainWindowService;
		private readonly IGitStatusService gitStatusService;
		private readonly IGitStatusService2 gitStatusService2;
		private readonly IProgressService progress;
		private readonly Lazy<IRepositoryService> repositoryService;

		private bool isPaused = false;

		//private Status oldStatus = Status.Default;
		private Task currentStatusTask = Task.CompletedTask;
		private int currentStatusCheckCount = 0;

		//private IReadOnlyList<string> oldBranchIds = None;
		private Task currentRepoTask = Task.CompletedTask;
		private int currentRepoCheckCount = 0;


		public StatusService(
			IFolderMonitorService folderMonitorService,
			IMainWindowService mainWindowService,
			IGitStatusService gitStatusService,
			IGitStatusService2 gitStatusService2,
			IProgressService progress,
			Lazy<IRepositoryService> repositoryService)
		{
			this.folderMonitorService = folderMonitorService;
			this.mainWindowService = mainWindowService;
			this.gitStatusService = gitStatusService;
			this.gitStatusService2 = gitStatusService2;
			this.progress = progress;
			this.repositoryService = repositoryService;


			folderMonitorService.FileChanged += (s, e) => OnFileChanged(e);
			folderMonitorService.RepoChanged += (s, e) => OnRepoChanged(e);
		}


		public event EventHandler<StatusChangedEventArgs> StatusChanged;

		public event EventHandler<RepoChangedEventArgs> RepoChanged;

		public bool IsPaused => isPaused;

		public void Monitor(string workingFolder)
		{
			folderMonitorService.Monitor(workingFolder);
		}


		public Task<Status> GetStatusAsync()
		{
			return GetFreshStatusAsync();
		}


		public Task<IReadOnlyList<string>> GetRepoIdsAsync()
		{
			return GetFreshBranchIdsAsync();
		}


		//public IReadOnlyList<string> GetRepoIds()
		//{
		//	return GetFreshRepoIds();
		//}


		public IDisposable PauseStatusNotifications(Refresh refresh = Refresh.None)
		{
			Log.Debug("Pause status");
			isPaused = true;
	
			return new Disposable(() =>
			{
				mainWindowService.SetMainWindowFocus();
				ShowStatusProgressAsync(refresh).RunInBackground();
			});
		}


		private async Task ShowStatusProgressAsync(Refresh refresh)
		{
			try
			{
				using (progress.ShowDialog("Updating view ... "))
				{
					bool useFreshRepository = refresh == Refresh.Repo;
					await repositoryService.Value.RefreshAfterCommandAsync(useFreshRepository);
				}		
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Exception(e, "Failed to check status");
			}
			finally
			{
				isPaused = false;
			}
		}


		private void OnFileChanged(FileEventArgs fileEventArgs)
		{
			if (!isPaused)
			{
				StartCheckStatusAsync(fileEventArgs).RunInBackground();
			}
			else
			{
				Log.Debug("paused status");
			}
		}

		private void OnRepoChanged(FileEventArgs fileEventArgs)
		{
			if (!isPaused)
			{
				StartCheckRepoAsync(fileEventArgs).RunInBackground();
			}
			else
			{
				Log.Debug("paused status");
			}
		}


		private async Task StartCheckStatusAsync(FileEventArgs fileEventArgs)
		{
			Log.Debug($"Checking status change at {fileEventArgs.DateTime} ...");

			if (await IsStatusCheckStartedAsync())
			{
				return;
			}

			Task<Status> newStatusTask = GetFreshStatusAsync();
			currentStatusTask = newStatusTask;

			Status newStatus = await newStatusTask;
		
			TriggerStatusChanged(fileEventArgs, newStatus);
		}


		private async Task StartCheckRepoAsync(FileEventArgs fileEventArgs)
		{
			Log.Debug($"Checking repo change at {fileEventArgs.DateTime} ...");

			if (await IsRepoCheckStartedAsync())
			{
				return;
			}

			Task<IReadOnlyList<string>> newRepoTask = GetFreshBranchIdsAsync();
			currentRepoTask = newRepoTask;

			IReadOnlyList<string> newBranchIds = await newRepoTask;

			TriggerRepoChanged(fileEventArgs, newBranchIds);	
		}


		private async Task<bool> IsStatusCheckStartedAsync()
		{
			int checkStatusCount = ++currentStatusCheckCount;
			await currentStatusTask;

			if (checkStatusCount != currentStatusCheckCount)
			{
				// Some other trigger will handle the check
				Log.Debug("Status already being checked");
				return true;
			}

			return false;
		}


		private async Task<bool> IsRepoCheckStartedAsync()
		{
			int checkRepoCount = ++currentRepoCheckCount;
			await currentRepoTask;

			if (checkRepoCount != currentRepoCheckCount)
			{
				// Some other trigger will handle the check
				Log.Debug("Repo already being checked");
				return true;
			}

			return false;
		}


		private async Task<IReadOnlyList<string>> GetFreshBranchIdsAsync()
		{
			Timing t = new Timing();
			R<IReadOnlyList<string>> branchIds = await gitStatusService2.GetRefsIdsAsync(CancellationToken.None);
			t.Log($"Got  {branchIds.Or(None).Count} branch ids");

			if (branchIds.IsFaulted)
			{
				Log.Error($"Failed to get branch ids {branchIds.Error}");
				return new List<string>();
			}

			return branchIds.Value;
		}


		private async Task<Status> GetFreshStatusAsync()
		{
			Log.Debug("Getting status ...");
			Timing t = new Timing();
			R<Status> status = await gitStatusService.GetCurrentStatusAsync();
			t.Log($"Got status {status}");

			if (status.IsFaulted)
			{
				Log.Error("Failed to read status");
				return Status.Default;
			}

			return status.Value;
		}





		private void TriggerStatusChanged(FileEventArgs fileEventArgs, Status newStatus)
		{
			StatusChanged?.Invoke(this, new StatusChangedEventArgs(newStatus, fileEventArgs.DateTime));
		}


		private void TriggerRepoChanged(FileEventArgs fileEventArgs, IReadOnlyList<string> newBranchIds)
		{
			RepoChanged?.Invoke(this, new RepoChangedEventArgs(fileEventArgs.DateTime, newBranchIds));
		}
	}
}