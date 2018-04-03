using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitRepo
	{
		Task<R> InitAsync(string path, CancellationToken ct);
	}
}