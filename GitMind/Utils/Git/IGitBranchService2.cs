using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;


namespace GitMind.Utils.Git
{
	public interface IGitBranchService2
	{
		//Task<R<GitAheadBehind>> GetAheadBehindAsync(string branchName, CancellationToken ct);
		Task<R<IReadOnlyList<GitBranch2>>> GetBranchesAsync(CancellationToken ct);

		Task<R> BranchAsync(string name, bool isCheckout, CancellationToken ct);
	}
}