using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;



namespace GitMind.Git
{
	internal interface IGitCommitsService
	{
		Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId);

		Task EditCommitBranchAsync(string workingFolder, string commitId, string rootId, BranchName branchName, ICredentialHandler credentialHandler);
	
		IReadOnlyList<CommitBranchName> GetSpecifiedNames(string workingFolder, string rootId);
		IReadOnlyList<CommitBranchName> GetCommitBranches(string workingFolder, string rootId);

	
		Task<R<GitCommit>> CommitAsync(string workingFolder, string message, string branchName, IReadOnlyList<CommitFile> paths);

		R<string> GetFullMessage(string workingFolder, string commitId);


		Task<R<IReadOnlyList<string>>> UndoCleanWorkingFolderAsync(string workingFolder);

		Task UndoFileInWorkingFolderAsync(string workingFolder, string path);

		Task UndoWorkingFolderAsync(string workingFolder);
		Task<R> ResetMerge(string workingFolder);
		Task<R> UnCommitAsync(string workingFolder);
	}
}