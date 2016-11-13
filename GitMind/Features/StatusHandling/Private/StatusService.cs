using System;
using GitMind.MainWindowViews;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Features.StatusHandling.Private
{
	[SingleInstance]
	internal class StatusService : IStatusService
	{
		private readonly IFolderMonitorService folderMonitorService;
		private readonly IMainWindowService mainWindowService;
		private readonly IRepositoryCommands repositoryCommands;
		private bool isPaused = false;

		public StatusService(
			IFolderMonitorService folderMonitorService,
			IMainWindowService mainWindowService,
			IRepositoryCommands repositoryCommands)
		{
			this.folderMonitorService = folderMonitorService;
			this.mainWindowService = mainWindowService;
			this.repositoryCommands = repositoryCommands;

			folderMonitorService.FileChanged += (s, e) => OnFileChanged(e);
			folderMonitorService.RepoChanged += (s, e) => OnRepoChanged(e);
		}


		public event EventHandler<FileEventArgs> FileChanged;

		public event EventHandler<FileEventArgs> RepoChanged;


		public void Monitor(string workingFolder)
		{
			folderMonitorService.Monitor(workingFolder);
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
				FileChanged?.Invoke(this, fileEventArgs);
			}
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