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

		Task FetchAsync(string path);

		Task<R<string>> GetCurrentBranchNameAsync(string workingFolder);

		R<string> GetCurrentRootPath(string path);

		Task<R<GitStatus>> GetStatusAsync(string workingFolder);

		Task<R<CommitDiff>> GetCommitDiffAsync(string workingFolder, string commitId);

		Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string name);

		Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId);

		Task SetSpecifiedCommitBranchAsync(string commitId, string branchName, string gitRepositoryPath);

		IReadOnlyList<GitSpecifiedNames> GetSpecifiedNames(string gitRepositoryPath);

	}
}