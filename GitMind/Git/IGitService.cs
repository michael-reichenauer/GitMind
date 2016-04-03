using System.Threading.Tasks;
using GitMind.Git.Private;


namespace GitMind.Git
{
	internal interface IGitService
	{
		Task<IGitRepo> GetRepoAsync(string path, bool isShift);

		Task<string> GetCurrentBranchNameAsync(string path);

		string GetCurrentRootPath(string path);

		Task<CommitDiff> GetCommitDiffAsync(string commitId);

		Task<GitStatus> GetStatusAsync(string path);
	}
}