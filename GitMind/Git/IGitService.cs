using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using GitMind.GitModel;
using GitMind.Utils;



namespace GitMind.Git
{
	internal interface IGitService
	{
		Error GitNotInstalledError { get; }
		Error GitCommandError { get; }

		GitRepository OpenRepository(string workingFolder);

		Task FetchAsync(string workingFolder);

		Task<R<string>> GetCurrentBranchNameAsync(string workingFolder);

		R<string> GetCurrentRootPath(string workingFolder);

		Task<R<GitStatus>> GetStatusAsync(string workingFolder);

		Task<R<CommitDiff>> GetCommitDiffAsync(string workingFolder, string commitId);
		Task<R<CommitDiff>> GetCommitDiffRangeAsync(string workingFolder, string id1, string id2);

		Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string name);

		Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId);

		Task SetSpecifiedCommitBranchAsync(string workingFolder, string commitId, string branchName);
		Task SetCommitBranchAsync(string workingFolder, string commitId, string branchName);

		IReadOnlyList<BranchName> GetSpecifiedNames(string workingFolder, string rootId);
		IReadOnlyList<BranchName> GetCommitBranches(string workingFolder, string rootId);

		Task FetchBranchAsync(string workingFolder, string branchName);
		Task MergeCurrentBranchFastForwardOnlyAsync(string workingFolder);
		Task MergeCurrentBranchAsync(string workingFolder);
		Task PushCurrentBranchAsync(string workingFolder);
		Task PushBranchAsync(string workingFolder, string name);

		Task<GitCommit> CommitAsync(string workingFolder, string message, IReadOnlyList<CommitFile> paths);
		Task SwitchToBranchAsync(string workingFolder, string branchName);
		Task UndoFileInCurrentBranchAsync(string workingFolder, string path);
		Task<GitCommit> MergeAsync(string workingFolder, string branchName);
		Task<string> SwitchToCommitAsync(string workingFolder, string commitId, string proposedBranchName);
		Task CreateBranchAsync(string workingFolder, string branchName, string commitId, bool isPublish);
		string GetFullMessage(string workingFolder, string commitId);

		Task PushNotesAsync(string workingFolder, string rootId);
		Task FetchNotesAsync(string workingFolder);
		Task<int> UndoCleanWorkingFolderAsync(string workingFolder);
		Task UndoWorkingFolderAsync(string workingFolder);
		void GetFile(string workingFolder, string fileId, string filePath);
		Task ResolveAsync(string workingFolder, string path);
	}
}