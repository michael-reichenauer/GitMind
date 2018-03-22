using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;


namespace GitMind.Utils.Git
{
	public interface IGitFetch
	{
		Task<GitResult> FetchAsync(CancellationToken ct);
		Task<GitResult> FetchRefsAsync(IEnumerable<string> refspecs, CancellationToken ct);
	}
}