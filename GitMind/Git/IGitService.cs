using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;



namespace GitMind.Git
{
	internal interface IGitService
	{
		Task FetchAsync(string workingFolder);

		Task<R<GitStatus>> GetStatusAsync(string workingFolder);

		Task<R<CommitDiff>> GetCommitDiffAsync(string workingFolder, string commitId);
		Task<R<CommitDiff>> GetCommitDiffRangeAsync(string workingFolder, string id1, string id2);

		Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string path);

		Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId);

		Task EditCommitBranchAsync(string workingFolder, string commitId, string rootId, BranchName branchName, ICredentialHandler credentialHandler);
	
		IReadOnlyList<CommitBranchName> GetSpecifiedNames(string workingFolder, string rootId);
		IReadOnlyList<CommitBranchName> GetCommitBranches(string workingFolder, string rootId);

		Task FetchBranchAsync(string workingFolder, BranchName branchName);
		Task MergeCurrentBranchFastForwardOnlyAsync(string workingFolder);
		Task MergeCurrentBranchAsync(string workingFolder);
		Task PushCurrentBranchAsync(string workingFolder, ICredentialHandler credentialHandler);
		Task PushBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler);

		Task<R<GitCommit>> CommitAsync(string workingFolder, string message, string branchName, IReadOnlyList<CommitFile> paths);
		Task SwitchToBranchAsync(string workingFolder, BranchName branchName);
		Task UndoFileInCurrentBranchAsync(string workingFolder, string path);
		Task<R<GitCommit>> MergeAsync(string workingFolder, BranchName branchName);
		Task<R<BranchName>> SwitchToCommitAsync(string workingFolder, string commitId, BranchName branchName);
		Task CreateBranchAsync(string workingFolder, BranchName branchName, string commitId);
		R<string> GetFullMessage(string workingFolder, string commitId);

		Task PushNotesAsync(string workingFolder, string rootId, ICredentialHandler credentialHandler);
		Task FetchAllNotesAsync(string workingFolder);
		Task<R<IReadOnlyList<string>>> UndoCleanWorkingFolderAsync(string workingFolder);
		Task UndoWorkingFolderAsync(string workingFolder);
		void GetFile(string workingFolder, string fileId, string filePath);
		Task ResolveAsync(string workingFolder, string path);
		Task<R> DeleteBranchAsync(string workingFolder, BranchName branchName, bool isRemote, ICredentialHandler credentialHandler);
		Task<R> PublishBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler);
		bool IsSupportedRemoteUrl(string workingFolder);
	}
}