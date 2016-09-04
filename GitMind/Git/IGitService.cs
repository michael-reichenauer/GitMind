using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;



namespace GitMind.Git
{
	internal interface IGitService
	{
		Task FetchAsync(string workingFolder);

		R<string> GetCurrentRootPath(string folder);

		Task<R<GitStatus>> GetStatusAsync(string workingFolder);

		Task<R<CommitDiff>> GetCommitDiffAsync(string workingFolder, string commitId);
		Task<R<CommitDiff>> GetCommitDiffRangeAsync(string workingFolder, string id1, string id2);

		Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string path);

		Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId);

		Task SetManualCommitBranchAsync(string workingFolder, string commitId, string branchName);
		Task SetCommitBranchAsync(string workingFolder, string commitId, string branchName);

		IReadOnlyList<BranchName> GetSpecifiedNames(string workingFolder, string rootId);
		IReadOnlyList<BranchName> GetCommitBranches(string workingFolder, string rootId);

		Task FetchBranchAsync(string workingFolder, string branchName);
		Task MergeCurrentBranchFastForwardOnlyAsync(string workingFolder);
		Task MergeCurrentBranchAsync(string workingFolder);
		Task PushCurrentBranchAsync(string workingFolder, ICredentialHandler credentialHandler);
		Task PushBranchAsync(string workingFolder, string branchName, ICredentialHandler credentialHandler);

		Task<R<GitCommit>> CommitAsync(string workingFolder, string message, IReadOnlyList<CommitFile> paths);
		Task SwitchToBranchAsync(string workingFolder, string branchName);
		Task UndoFileInCurrentBranchAsync(string workingFolder, string path);
		Task<R<GitCommit>> MergeAsync(string workingFolder, string branchName);
		Task<R<string>> SwitchToCommitAsync(string workingFolder, string commitId, string proposedBranchName);
		Task CreateBranchAsync(string workingFolder, string branchName, string commitId);
		string GetFullMessage(string workingFolder, string commitId);

		Task PushNotesAsync(string workingFolder, string rootId, ICredentialHandler credentialHandler);
		Task FetchAllNotesAsync(string workingFolder);
		Task<R<IReadOnlyList<string>>> UndoCleanWorkingFolderAsync(string workingFolder);
		Task UndoWorkingFolderAsync(string workingFolder);
		void GetFile(string workingFolder, string fileId, string filePath);
		Task ResolveAsync(string workingFolder, string path);
		Task<R> TryDeleteBranchAsync(string workingFolder, string branchName, bool isRemote, bool isUseForce, ICredentialHandler credentialHandler);
		Task<bool> PublishBranchAsync(string workingFolder, string branchName, ICredentialHandler credentialHandler);
		bool IsSupportedRemoteUrl(string workingFolder);
	}
}