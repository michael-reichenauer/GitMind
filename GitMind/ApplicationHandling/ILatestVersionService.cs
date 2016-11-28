using System;
using System.Threading.Tasks;


namespace GitMind.ApplicationHandling
{
	internal interface ILatestVersionService
	{
		event EventHandler OnNewVersionAvailable;

		void StartCheckForLatestVersion();

		Task<bool> StartLatestInstalledVersionAsync();
	}
}