using System.Threading.Tasks;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.Git
{
	internal interface IGitService
	{
		Error GitNotInstalled { get; }

		Task<IGitRepo> GetRepoAsync(string path, bool isShift);

		Task<string> GetCurrentBranchNameAsync(string path);

		Result<string> GetCurrentRootPath(string path);

		Task<CommitDiff> GetCommitDiffAsync(string commitId);

		Task<GitStatus> GetStatusAsync(string path);
	}
}