using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;


namespace GitMind.Utils.Git
{
	public interface IGitBranchService2
	{
		GitException NotFullyMergedException { get; }
		//Task<R<GitAheadBehind>> GetAheadBehindAsync(string branchName, CancellationToken ct);
		Task<R<IReadOnlyList<GitBranch2>>> GetBranchesAsync(CancellationToken ct);

		Task<R> BranchAsync(string name, bool isCheckout, CancellationToken ct);
		Task<R> BranchFromCommitAsync(string name, string sha, bool isCheckout, CancellationToken ct);
		Task<R> DeleteLocalBranchAsync(string name, bool isForce, CancellationToken ct);

		Task<R<string>> GetCommonAncestorAsync(string sha1, string sha2, CancellationToken ct);
	}
}