using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitPush
	{
		Task<GitResult> PushAsync(CancellationToken ct);

		Task<GitResult> PushRefsAsync(IEnumerable<string> refspecs, CancellationToken ct);
		Task<GitResult> PushBranchAsync(string branchName, CancellationToken ct);
	}
}