using System.Threading.Tasks;
using GitMind.Git;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		bool IsRepositoryCached(string workingFolder);

		Task<Repository> GetCachedOrFreshRepositoryAsync(string workingFolder);

		Task<Repository> GetFreshRepositoryAsync(string workingFolder);

		Task<Repository> UpdateRepositoryAsync(Repository repository);

		Task SetSpecifiedCommitBranchAsync(string commitId, string rootId, BranchName branchName);
	}
}