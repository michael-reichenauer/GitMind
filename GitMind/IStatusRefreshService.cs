using System.Threading.Tasks;


namespace GitMind
{
	internal interface IStatusRefreshService
	{
		void Start();
		Task UpdateStatusAsync();
	}
}