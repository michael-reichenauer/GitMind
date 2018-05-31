using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.Features.Commits.Private
{
	internal interface IGitCommitBranchNameService
	{
		Task EditCommitBranchNameAsync(CommitSha commitSha, CommitSha rootSha, BranchName branchName);
		Task SetCommitBranchNameAsync(CommitSha commitSha, BranchName branchName);
		Task<IReadOnlyList<CommitBranchName>> GetEditedBranchNamesAsync(CommitSha rootSha);
		Task<IReadOnlyList<CommitBranchName>> GetCommitBranchNamesAsync(CommitSha rootId);

		Task PushNotesAsync(CommitSha rootId);

		Task<R> FetchAllNotesAsync();
	}
}