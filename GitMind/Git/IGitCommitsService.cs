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
		Task<R<IReadOnlyList<StatusFile>>> GetFilesForCommitAsync(CommitId commitId);

		Task EditCommitBranchAsync(CommitId commitId, CommitId rootId, BranchName branchName);
	
		IReadOnlyList<CommitBranchName> GetSpecifiedNames(CommitId rootId);
		IReadOnlyList<CommitBranchName> GetCommitBranches(CommitId rootId);

	
		Task<R<GitCommit>> CommitAsync(string message, string branchName, IReadOnlyList<CommitFile> paths);

		R<string> GetFullMessage(CommitId commitId);


		Task<R<IReadOnlyList<string>>> CleanWorkingFolderAsync();

		Task UndoFileInWorkingFolderAsync(string path);

		Task UndoWorkingFolderAsync();
		Task<R> ResetMerge();
		Task<R> UnCommitAsync();
	}
}