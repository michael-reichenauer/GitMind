using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitPushService
	{
		Task<R> PushAsync(CancellationToken ct);

		Task<R> PushRefsAsync(IEnumerable<string> refspecs, CancellationToken ct);
		Task<R> PushBranchAsync(string branchName, CancellationToken ct);
		Task<R> PushTagAsync(string tagName, CancellationToken ct);
		Task<R> PushDeleteRemoteBranchAsync(string branchName, CancellationToken ct);
		Task<R> PushDeleteRemoteTagAsync(string tagName, CancellationToken ct);
	}
}