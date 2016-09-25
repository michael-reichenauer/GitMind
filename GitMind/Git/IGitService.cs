using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;



namespace GitMind.Git
{
	internal interface IGitService
	{
		Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId);

		Task EditCommitBranchAsync(string workingFolder, string commitId, string rootId, BranchName branchName, ICredentialHandler credentialHandler);
	
		IReadOnlyList<CommitBranchName> GetSpecifiedNames(string workingFolder, string rootId);
		IReadOnlyList<CommitBranchName> GetCommitBranches(string workingFolder, string rootId);

	
		Task<R<GitCommit>> CommitAsync(string workingFolder, string message, string branchName, IReadOnlyList<CommitFile> paths);
		
		Task UndoFileInCurrentBranchAsync(string workingFolder, string path);
		R<string> GetFullMessage(string workingFolder, string commitId);


		Task<R<IReadOnlyList<string>>> UndoCleanWorkingFolderAsync(string workingFolder);
		Task UndoWorkingFolderAsync(string workingFolder);
	}
}