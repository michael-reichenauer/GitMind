using System.Collections.Generic;
using System.Threading.Tasks;
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

		IReadOnlyList<GitSpecifiedNames> GetSpecifiedNames(string workingFolder);

		Task FetchBranchAsync(string workingFolder, string branchName);
		Task MergeCurrentBranchFastForwardOnlyAsync(string workingFolder);
		Task MergeCurrentBranchAsync(string workingFolder);
		Task PushCurrentBranchAsync(string workingFolder);
		Task PushBranchAsync(string workingFolder, string name);

		Task CommitAsync(string workingFolder, string message, IReadOnlyList<CommitFile> paths);
		Task SwitchToBranchAsync(string workingFolder, string branchName);
		Task UndoFileInCurrentBranchAsync(string workingFolder, string path);
	}
}