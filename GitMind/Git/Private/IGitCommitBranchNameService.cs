using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal interface IGitCommitBranchNameService
	{
		Task EditCommitBranchNameAsync(CommitSha commitId, CommitSha rootId, BranchName branchName);
		Task SetCommitBranchNameAsync(CommitSha commitId, BranchName branchName);
		IReadOnlyList<CommitBranchName> GetEditedBranchNames(CommitSha rootId);
		IReadOnlyList<CommitBranchName> GetCommitBrancheNames(CommitSha rootId);

		Task PushNotesAsync(CommitSha rootId);

		Task<R> FetchAllNotesAsync();
	}
}