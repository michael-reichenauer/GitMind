using System;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitRepoService
	{
		Task<R> InitAsync(string path, CancellationToken ct);

		Task<R> InitBareAsync(string path, CancellationToken ct);

		Task<R> CloneAsync(string uri, string path, Action<string> progress, CancellationToken ct);
	}
}