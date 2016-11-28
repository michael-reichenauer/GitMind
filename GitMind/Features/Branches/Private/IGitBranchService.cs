using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Branches.Private
{
	internal interface IGitBranchService
	{
		Task<R> CreateBranchAsync(BranchName branchName, string commitId);

		Task<R> SwitchToBranchAsync(BranchName branchName);

		Task<R<BranchName>> SwitchToCommitAsync(string commitId, BranchName branchName);

		Task<R> MergeCurrentBranchFastForwardOnlyAsync();

		Task<R> MergeCurrentBranchAsync();

		Task<R> MergeAsync(BranchName branchName);

		R<GitDivergence> CheckAheadBehind(string localTip, string remoteTip);

		Task<R> DeleteLocalBranchAsync(BranchName branchName);
	}
}