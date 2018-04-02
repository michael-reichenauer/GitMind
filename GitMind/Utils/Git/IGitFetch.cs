using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitFetch
	{
		Task<R> FetchAsync(CancellationToken ct);
		Task<R> FetchBranchAsync(string branchName, CancellationToken ct);
		Task<R> FetchRefsAsync(IEnumerable<string> refspecs, CancellationToken ct);
		Task<R> FetchPruneTagsAsync(CancellationToken ct);
	}
}