using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Branching.Private
{
	internal interface IGitBranchService
	{
		Task<R> CreateBranchAsync(string workingFolder, BranchName branchName, string commitId);

		Task<R> SwitchToBranchAsync(string workingFolder, BranchName branchName);

		Task<R<BranchName>> SwitchToCommitAsync(string workingFolder, string commitId, BranchName branchName);

		Task<R> MergeCurrentBranchFastForwardOnlyAsync(string workingFolder);

		Task<R> MergeCurrentBranchAsync(string workingFolder);

		Task<R> MergeAsync(string workingFolder, BranchName branchName);

		R<GitDivergence> CheckAheadBehind(string workingFolder, string localTip, string remoteTip);

		Task<R> DeleteLocalBranchAsync(string workingFolder, BranchName branchName);
	}
}