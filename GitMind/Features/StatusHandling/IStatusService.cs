using System;
using GitMind.Features.StatusHandling.Private;


namespace GitMind.Features.StatusHandling
{
	internal interface IStatusService
	{
		event EventHandler<FileEventArgs> FileChanged;
		event EventHandler<FileEventArgs> RepoChanged;

		void Monitor(string workingFolder);
		IDisposable PauseStatusNotifications();
	}
}