using System;
using GitMind.Utils;


namespace GitMind.Features.StatusHandling.Private
{
	[SingleInstance]
	internal class StatusService : IStatusService
	{
		private readonly IFolderMonitorService folderMonitorService;


		public StatusService(IFolderMonitorService folderMonitorService)
		{
			this.folderMonitorService = folderMonitorService;
		}

		public event EventHandler<FileEventArgs> FileChanged
		{
			add { folderMonitorService.FileChanged += value; }
			remove { folderMonitorService.FileChanged -= value; }
		}

		public event EventHandler<FileEventArgs> RepoChanged
		{
			add { folderMonitorService.RepoChanged += value; }
			remove { folderMonitorService.RepoChanged -= value; }
		}

		public void Monitor(string workingFolder)
		{
			folderMonitorService.Monitor(workingFolder);
		}
	}
}