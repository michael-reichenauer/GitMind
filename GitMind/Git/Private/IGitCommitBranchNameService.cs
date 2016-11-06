using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal interface IGitCommitBranchNameService
	{
		Task EditCommitBranchNameAsync(string commitId, string rootId, BranchName branchName);
		Task SetCommitBranchNameAsync(string commitId, BranchName branchName);
		IReadOnlyList<CommitBranchName> GetEditedBranchNames(string rootId);
		IReadOnlyList<CommitBranchName> GetCommitBrancheNames(string rootId);

		Task PushNotesAsync(string rootId);

		Task<R> FetchAllNotesAsync();
	}
}