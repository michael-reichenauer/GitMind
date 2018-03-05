using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitFetch
	{
		Task FetchAsync(CancellationToken ct);
	}
}