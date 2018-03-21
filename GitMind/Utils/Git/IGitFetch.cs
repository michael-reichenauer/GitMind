using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitFetch
	{
		Task<bool> FetchAsync(CancellationToken ct);
	}
}