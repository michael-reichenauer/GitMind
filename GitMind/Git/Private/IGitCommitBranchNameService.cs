using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal interface IGitCommitBranchNameService
	{
		Task EditCommitBranchNameAsync(string workingFolder, string commitId, string rootId, BranchName branchName);
		Task SetCommitBranchNameAsync(string workingFolder, string commitId, BranchName branchName);
		IReadOnlyList<CommitBranchName> GetEditedBranchNames(string workingFolder, string rootId);
		IReadOnlyList<CommitBranchName> GetCommitBrancheNames(string workingFolder, string rootId);

		Task PushNotesAsync(string workingFolder, string rootId);

		Task<R> FetchAllNotesAsync(string workingFolder);
	}
}