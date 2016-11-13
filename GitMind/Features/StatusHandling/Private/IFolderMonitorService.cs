using System;


namespace GitMind.Features.StatusHandling.Private
{
	internal interface IFolderMonitorService
	{
		event EventHandler<FileEventArgs> FileChanged;

		event EventHandler<FileEventArgs> RepoChanged;

		void Monitor(string workingFolder);
	}
}