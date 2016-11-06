using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;



namespace GitMind.Git
{
	internal interface IGitCommitsService
	{
		Task<R<GitCommitFiles>> GetFilesForCommitAsync(string commitId);

		Task EditCommitBranchAsync(string commitId, string rootId, BranchName branchName);
	
		IReadOnlyList<CommitBranchName> GetSpecifiedNames(string rootId);
		IReadOnlyList<CommitBranchName> GetCommitBranches(string rootId);

	
		Task<R<GitCommit>> CommitAsync(string message, string branchName, IReadOnlyList<CommitFile> paths);

		R<string> GetFullMessage(string commitId);


		Task<R<IReadOnlyList<string>>> UndoCleanWorkingFolderAsync();

		Task UndoFileInWorkingFolderAsync(string path);

		Task UndoWorkingFolderAsync();
		Task<R> ResetMerge();
		Task<R> UnCommitAsync();
	}
}