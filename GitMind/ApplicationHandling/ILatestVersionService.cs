using System.Threading.Tasks;


namespace GitMind.ApplicationHandling
{
	internal interface ILatestVersionService
	{
		void StartCheckForLatestVersion();

		Task<bool> StartLatestInstalledVersionAsync();
	}
}