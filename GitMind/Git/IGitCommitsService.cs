using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Features.StatusHandling;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.Utils;



namespace GitMind.Git
{
	internal interface IGitCommitsService
	{
		Task<R<IReadOnlyList<StatusFile>>> GetFilesForCommitAsync(CommitSha commitSha);

		Task EditCommitBranchAsync(CommitSha commitId, CommitSha rootId, BranchName branchName);
	
		IReadOnlyList<CommitBranchName> GetSpecifiedNames(CommitSha rootId);
		IReadOnlyList<CommitBranchName> GetCommitBranches(CommitSha rootId);

	
		Task<R<GitCommit>> CommitAsync(string message, string branchName, IReadOnlyList<CommitFile> paths);

		R<string> GetFullMessage(CommitSha commitSha);


		Task<R<IReadOnlyList<string>>> CleanWorkingFolderAsync();

		Task UndoFileInWorkingFolderAsync(string path);

		Task UndoWorkingFolderAsync();
		Task<R> ResetMerge();
		Task<R> UnCommitAsync();
	}
}