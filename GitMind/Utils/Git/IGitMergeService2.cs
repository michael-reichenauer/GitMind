using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitMergeService2
	{
		Task<R> MergeAsync(string name, CancellationToken ct);
		Task<R<bool>> TryMergeFastForwardAsync(string name, CancellationToken ct);
	}


}