using System.Threading.Tasks;


namespace GitMind.ApplicationHandling.Installation
{
	internal interface ILatestVersionService
	{
		Task<bool> IsNewVersionAvailableAsync();
		Task<bool> InstallLatestVersionAsync();
		Task<bool> RunLatestVersionAsync();
		bool IsNewVersionInstalled();
	}
}