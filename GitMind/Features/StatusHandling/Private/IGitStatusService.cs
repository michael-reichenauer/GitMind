using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Features.StatusHandling.Private
{
	internal interface IGitStatusService
	{
		Task<R<Status>> GetCurrentStatusAsync();
	}
}