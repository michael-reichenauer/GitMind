using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal interface IGitCommitBranchNameService
	{
		Task EditCommitBranchNameAsync(CommitSha commitSha, CommitSha rootSha, BranchName branchName);
		Task SetCommitBranchNameAsync(CommitSha commitSha, BranchName branchName);
		IReadOnlyList<CommitBranchName> GetEditedBranchNames(CommitSha rootSha);
		IReadOnlyList<CommitBranchName> GetCommitBrancheNames(CommitSha rootId);

		Task PushNotesAsync(CommitSha rootId);

		Task<R> FetchAllNotesAsync();
	}
}