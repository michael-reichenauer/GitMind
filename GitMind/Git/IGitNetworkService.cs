using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git
{
	internal interface IGitNetworkService
	{
		Task<R> FetchAsync(string workingFolder);

		Task<R> FetchBranchAsync(string workingFolder, BranchName branchName);

		Task<R> FetchRefsAsync(string workingFolder, IEnumerable<string> refspecs);

		Task<R> PushCurrentBranchAsync(string workingFolder, ICredentialHandler credentialHandler);

		Task<R> PushBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler);

		Task<R> PushRefsAsync(
			string workingFolder, IEnumerable<string> refspecs, ICredentialHandler credentialHandler);

		Task<R> PublishBranchAsync(
			string workingFolder, BranchName branchName, ICredentialHandler credentialHandler);

		Task<R> DeleteRemoteBranchAsync(
			string workingFolder, BranchName branchName, ICredentialHandler credentialHandler);
	}
}