using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git
{
	internal interface INetworkService
	{
		Task<R> FetchAsync(string workingFolder);

		Task<R> FetchBranchAsync(string workingFolder, BranchName branchName);

		Task<R> PushCurrentBranchAsync(string workingFolder, ICredentialHandler credentialHandler);

		Task<R> PushBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler);

		Task PushNotesAsync(string workingFolder, string rootId, ICredentialHandler credentialHandler);

		Task<R> FetchAllNotesAsync(string workingFolder);
	}
}