using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Common.ProgressHandling;
using GitMind.MainWindowViews;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Features.StatusHandling.Private
{
	[SingleInstance]
	internal class StatusService : IStatusService
	{
		private static readonly IReadOnlyList<string> None = Enumerable.Empty<string>().ToReadOnlyList();

		private readonly IFolderMonitorService folderMonitorService;
		private readonly IMainWindowService mainWindowService;
		private readonly IGitStatusService gitStatusService;
		private readonly IProgressService progress;
		private readonly IRepositoryCommands repositoryCommands;

		private bool isPaused = false;
		private bool isStatusChanged = false;
		private bool isRepoChanged = false;

		//private Status oldStatus = Status.Default;
		private Task currentStatusTask = Task.CompletedTask;
		private int currentStatusCheckCount = 0;

		private IReadOnlyList<string> oldBranchIds = None;
		private Task currentRepoTask = Task.CompletedTask;
		private int currentRepoCheckCount = 0;


		public StatusService(
			IFolderMonitorService folderMonitorService,
			IMainWindowService mainWindowService,
			IGitStatusService gitStatusService,
			IProgressService progress,
			IRepositoryCommands repositoryCommands)
		{
			this.folderMonitorService = folderMonitorService;
			this.mainWindowService = mainWindowService;
			this.gitStatusService = gitStatusService;
			this.progress = progress;
			this.repositoryCommands = repositoryCommands;


			folderMonitorService.FileChanged += (s, e) => OnFileChanged(e);
			folderMonitorService.RepoChanged += (s, e) => OnRepoChanged(e);
		}


		public event EventHandler<StatusChangedEventArgs> StatusChanged;

		public event EventHandler<RepoChangedEventArgs> RepoChanged;


		public void Monitor(string workingFolder)
		{
			folderMonitorService.Monitor(workingFolder);
		}


		public Task<Status> GetStatusAsync()
		{
			return GetFreshStatusAsync();
		}


		public IDisposable PauseStatusNotifications(Refresh refresh = Refresh.None)
		{
			Log.Debug("Pause status");
			isPaused = true;
			isStatusChanged = false;
			isRepoChanged = false;

			return new Disposable(() =>
			{
				mainWindowService.SetMainWindowFocus();
				ShowStatusProgressAsync(refresh, isStatusChanged, isRepoChanged).RunInBackground();
			});
		}


		private async Task ShowStatusProgressAsync(Refresh refresh, bool isStatus, bool isRepo)
		{
			try
			{
				isStatusChanged = false;
				isRepoChanged = false;

				if (refresh == Refresh.Repo || isRepo)
				{
					using (progress.ShowDialog("Updating branch structure ... "))
					{
						await repositoryCommands.RefreshAfterCommandAsync(true);
					}
				}
				else
				{
					IReadOnlyList<string> newBranchIds = await GetFreshBranchIdsAsync();
					if (!oldBranchIds.SequenceEqual(newBranchIds))
					{
						oldBranchIds = newBranchIds;
						using (progress.ShowDialog("Updating branch structure ... "))
						{
							await repositoryCommands.RefreshAfterCommandAsync(true);
						}
					}
					else
					{
						oldBranchIds = newBranchIds;
						using (progress.ShowDialog("Updating status ... "))
						{
							await repositoryCommands.RefreshAfterCommandAsync(false);
						}
					}
				}
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn($"Failed to check status, {e}");
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
				isStatusChanged = true;
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
				isRepoChanged = true;
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

			if (!oldBranchIds.SequenceEqual(newBranchIds))
			{
				Log.Debug("Changed repo");
				TriggerRepoChanged(fileEventArgs);
			}
			else
			{
				Log.Debug("Same repo");
			}

			oldBranchIds = newBranchIds;
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
			R<IReadOnlyList<string>> branchIds = await gitStatusService.GetBrancheIdsAsync();
			t.Log($"Got  {branchIds.Or(None).Count} branch ids");

			if (branchIds.IsFaulted)
			{
				Log.Warn($"Failed to get branch ids {branchIds.Error}");
				return oldBranchIds;
			}

			return branchIds.Value;
		}


		private async Task<Status> GetFreshStatusAsync()
		{
			Timing t = new Timing();
			R<Status> status = await gitStatusService.GetCurrentStatusAsync();
			t.Log($"Got status {status}");

			if (status.IsFaulted)
			{
				Log.Warn($"Failed to read status");
				return Status.Default;
			}

			return status.Value;
		}



		private void TriggerStatusChanged(FileEventArgs fileEventArgs, Status newStatus)
		{
			StatusChanged?.Invoke(this, new StatusChangedEventArgs(newStatus, fileEventArgs.DateTime));
		}


		private void TriggerRepoChanged(FileEventArgs fileEventArgs)
		{
			RepoChanged?.Invoke(this, new RepoChangedEventArgs(fileEventArgs.DateTime));
		}
	}
}