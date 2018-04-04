using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitRepoService
	{
		Task<R> InitAsync(string path, CancellationToken ct);
	}
}