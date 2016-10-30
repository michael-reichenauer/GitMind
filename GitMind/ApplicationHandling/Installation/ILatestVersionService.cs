using System.Threading.Tasks;


namespace GitMind.ApplicationHandling.Installation
{
	internal interface ILatestVersionService
	{
		void StartCheckForLatestVersion();

		Task<bool> StartLatestInstalledVersionAsync();
	}
}