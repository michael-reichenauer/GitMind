using System.Threading.Tasks;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.Git
{
	internal interface IGitService
	{
		Error GitNotInstalledError { get; }
		Error GitCommandError { get; }

		Task<Result<IGitRepo>> GetRepoAsync(string path);

		Task FetchAsync(string path);

		Task<Result<string>> GetCurrentBranchNameAsync(string path);

		Result<string> GetCurrentRootPath(string path);

		Task<Result<CommitDiff>> GetCommitDiffAsync(string commitId);

		Task<Result<GitStatus>> GetStatusAsync(string path);
	}
}