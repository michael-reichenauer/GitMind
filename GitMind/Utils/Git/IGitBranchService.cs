using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;


namespace GitMind.Utils.Git
{
	public interface IGitBranchService
	{
		GitException NotFullyMergedException { get; }

		Task<R<IReadOnlyList<GitBranch>>> GetBranchesAsync(CancellationToken ct);

		Task<R> BranchAsync(string name, bool isCheckout, CancellationToken ct);

		Task<R> BranchFromCommitAsync(string name, string sha, bool isCheckout, CancellationToken ct);

		Task<R> DeleteLocalBranchAsync(string name, bool isForce, CancellationToken ct);
	}
}