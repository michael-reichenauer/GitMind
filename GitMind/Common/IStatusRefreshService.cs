using System.Threading.Tasks;


namespace GitMind.Common
{
	internal interface IStatusRefreshService
	{
		void Start();
		Task UpdateStatusAsync(string workingFolder);
	}
}