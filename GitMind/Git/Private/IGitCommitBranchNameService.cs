using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal interface IGitCommitBranchNameService
	{
		Task EditCommitBranchNameAsync(CommitId commitId, CommitId rootId, BranchName branchName);
		Task SetCommitBranchNameAsync(CommitId commitId, BranchName branchName);
		IReadOnlyList<CommitBranchName> GetEditedBranchNames(CommitId rootId);
		IReadOnlyList<CommitBranchName> GetCommitBrancheNames(CommitId rootId);

		Task PushNotesAsync(CommitId rootId);

		Task<R> FetchAllNotesAsync();
	}
}