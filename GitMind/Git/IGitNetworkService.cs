using System.Threading.Tasks;


namespace GitMind.Git
{
	internal interface IGitNetworkService
	{
		Task FetchAsync(string workingFolder);

		Task FetchBranchAsync(string workingFolder, BranchName branchName);

		Task PushCurrentBranchAsync(string workingFolder, ICredentialHandler credentialHandler);

		Task PushBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler);

		Task PushNotesAsync(string workingFolder, string rootId, ICredentialHandler credentialHandler);

		Task FetchAllNotesAsync(string workingFolder);
	}
}