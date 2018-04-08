using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitPullService
	{
		Task<R> PullAsync(CancellationToken ct);
	}
}