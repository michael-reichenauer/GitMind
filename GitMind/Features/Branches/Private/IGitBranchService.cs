using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Branches.Private
{
	internal interface IGitBranchService
	{
		Task<R> CreateBranchAsync(BranchName branchName, CommitSha commitSha);

		Task<R> SwitchToBranchAsync(BranchName branchName, CommitSha tipCommitRealCommitSha);

		Task<R<BranchName>> SwitchToCommitAsync(CommitSha commitSha, BranchName branchName);

		Task<R> MergeCurrentBranchFastForwardOnlyAsync();

		Task<R> MergeCurrentBranchAsync();

		Task<R> MergeAsync(BranchName branchName);

		R<GitDivergence> CheckAheadBehind(CommitSha localTip, CommitSha remoteTip);

		Task<R> DeleteLocalBranchAsync(BranchName branchName);
		Task<R> MergeAsync(CommitSha commitSha);
	}
}