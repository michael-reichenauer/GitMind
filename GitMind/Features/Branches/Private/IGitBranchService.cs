using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Branches.Private
{
	internal interface IGitBranchService
	{
		Task<R> CreateBranchAsync(BranchName branchName, CommitId commitId);

		Task<R> SwitchToBranchAsync(BranchName branchName);

		Task<R<BranchName>> SwitchToCommitAsync(CommitId commitId, BranchName branchName);

		Task<R> MergeCurrentBranchFastForwardOnlyAsync();

		Task<R> MergeCurrentBranchAsync();

		Task<R> MergeAsync(BranchName branchName);

		R<GitDivergence> CheckAheadBehind(CommitId localTip, CommitId remoteTip);

		Task<R> DeleteLocalBranchAsync(BranchName branchName);
	}
}