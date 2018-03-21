using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;


namespace GitMind.Utils.Git
{
	internal interface IGitStatus
	{
		Task<Status> GetAsync(CancellationToken ct);
	}
}