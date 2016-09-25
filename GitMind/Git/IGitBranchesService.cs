using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git
{
	internal interface IGitBranchesService
	{
		Task CreateBranchAsync(string workingFolder, BranchName branchName, string commitId);

		Task<R> PublishBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler);

		Task SwitchToBranchAsync(string workingFolder, BranchName branchName);

		Task<R<BranchName>> SwitchToCommitAsync(string workingFolder, string commitId, BranchName branchName);

		Task MergeCurrentBranchFastForwardOnlyAsync(string workingFolder);

		Task MergeCurrentBranchAsync(string workingFolder);

		Task<R<GitCommit>> MergeAsync(string workingFolder, BranchName branchName);

		Task<R> DeleteBranchAsync(string workingFolder, BranchName branchName, bool isRemote, ICredentialHandler credentialHandler);
	}
}