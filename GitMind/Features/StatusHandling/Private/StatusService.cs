using System;
using System.Threading.Tasks;
using GitMind.MainWindowViews;
using GitMind.Utils;


namespace GitMind.Features.StatusHandling.Private
{
	[SingleInstance]
	internal class StatusService : IStatusService
	{
		private readonly IFolderMonitorService folderMonitorService;
		private readonly IMainWindowService mainWindowService;
		private readonly IGitStatusService gitStatusService;

		private bool isPaused = false;
		private Status oldStatus = Status.Default;
		private Task currentStatusTask = Task.CompletedTask;
		private int currentCheckCount = 0;

		public StatusService(
			IFolderMonitorService folderMonitorService,
			IMainWindowService mainWindowService,
			IGitStatusService gitStatusService)
		{
			this.folderMonitorService = folderMonitorService;
			this.mainWindowService = mainWindowService;
			this.gitStatusService = gitStatusService;


			folderMonitorService.FileChanged += (s, e) => OnFileChanged(e);
			folderMonitorService.RepoChanged += (s, e) => OnRepoChanged(e);
		}


		public event EventHandler<StatusChangedEventArgs> StatusChanged;

		public event EventHandler<FileEventArgs> RepoChanged;


		public void Monitor(string workingFolder)
		{
			folderMonitorService.Monitor(workingFolder);
		}


		public async Task<Status> GetStatusAsync()
		{
			R<Status> status = await gitStatusService.GetCurrentStatusAsync();
			if (status.HasValue)
			{
				oldStatus = status.Value;
				return status.Value;
			}

			Log.Warn($"Failed to retrieve status, {status}");

			return Status.Default;
		}


		public IDisposable PauseStatusNotifications()
		{
			isPaused = true;

			return new Disposable(() =>
			{
				isPaused = false;
				mainWindowService.SetMainWindowFocus();
			});
		}


		private void OnFileChanged(FileEventArgs fileEventArgs)
		{
			if (!isPaused)
			{
				StartCheckStatusAsync(fileEventArgs).RunInBackground();
			}
		}


		private async Task StartCheckStatusAsync(FileEventArgs fileEventArgs)
		{
			Log.Debug($"File change at {fileEventArgs.DateTime}");

			if (await IsCheckAlreadyStartedAsync())
			{
				return;
			}

			Task<R<Status>> newStatusTask = gitStatusService.GetCurrentStatusAsync();
			currentStatusTask = newStatusTask;

			R<Status> status = await newStatusTask;
			if (status.HasValue)
			{
				Status newStatus = status.Value;
				if (!newStatus.IsSame(oldStatus))
				{
					Log.Debug($"Changed status {oldStatus} => {newStatus}");
					TriggerStatusChanged(fileEventArgs, newStatus, oldStatus);
				}
				else
				{
					Log.Debug($"Same status {oldStatus} == {newStatus}");
				}

				oldStatus = status.Value;
			}
			else
			{
				Log.Warn($"Failed to get new status {status.Error}");
			}
		}


		private async Task<bool> IsCheckAlreadyStartedAsync()
		{
			int checkCount = ++currentCheckCount;
			await currentStatusTask;

			if (checkCount != currentCheckCount)
			{
				// Some other trigger will handle the check
				Log.Debug("Status already being checked");
				return true;
			}
			return false;
		}


		private void TriggerStatusChanged(
			FileEventArgs fileEventArgs, Status newStatus, Status oldStatus)
		{
			StatusChanged?.Invoke(this, new StatusChangedEventArgs(
				newStatus, oldStatus, fileEventArgs.DateTime));
		}


		private void OnRepoChanged(FileEventArgs fileEventArgs)
		{
			if (!isPaused)
			{
				RepoChanged?.Invoke(this, fileEventArgs);
			}
		}
	}
}