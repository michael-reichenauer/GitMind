using System.Threading.Tasks;


namespace GitMind.Installation
{
	internal interface ILatestVersionService
	{
		Task<bool> IsNewVersionAvailableAsync();
		Task<bool> InstallLatestVersionAsync();
		Task<bool> RunLatestVersionAsync();
	}
}