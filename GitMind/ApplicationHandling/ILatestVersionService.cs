using System;


namespace GitMind.ApplicationHandling
{
	internal interface ILatestVersionService
	{
		event EventHandler OnNewVersionAvailable;

		void StartCheckForLatestVersion();
	}
}