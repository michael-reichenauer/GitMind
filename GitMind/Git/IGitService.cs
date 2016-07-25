using System.Collections.Generic;
using System.Threading.Tasks;
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

		Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string name);

		Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId);

		Task SetSpecifiedCommitBranchAsync(string workingFolder, string commitId, string branchName);

		IReadOnlyList<GitSpecifiedNames> GetSpecifiedNames(string workingFolder);

		Task UpdateBranchAsync(string workingFolder, string branchName);
		Task UpdateCurrentBranchAsync(string workingFolder);
		Task PullCurrentBranchAsync(string workingFolder);
		Task PushCurrentBranchAsync(string workingFolder);
	}
}