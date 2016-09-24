using System.Collections.Generic;
using System.Threading.Tasks;


namespace GitMind.Git.Private
{
	internal interface IGitNotesService
	{
		Task SetManualCommitBranchAsync(string workingFolder, string commitId, BranchName branchName);
		Task SetCommitBranchAsync(string workingFolder, string commitId, BranchName branchName);
		IReadOnlyList<CommitBranchName> GetSpecifiedNames(string workingFolder, string rootId);
		IReadOnlyList<CommitBranchName> GetCommitBranches(string workingFolder, string rootId);


		Task PushNotesAsync(
			string workingFolder, string rootId, ICredentialHandler credentialHandler);


		Task FetchAllNotesAsync(string workingFolder);
	}
}