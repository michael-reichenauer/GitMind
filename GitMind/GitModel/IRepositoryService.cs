using System.Threading.Tasks;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		Task<Repository> GetCachedOrFreshRepositoryAsync(string workingFolder);

		Task<Repository> GetFreshRepositoryAsync(string workingFolder);

		Task<Repository> UpdateRepositoryAsync(Repository repository);

		Task SetSpecifiedCommitBranchAsync(string gitRepositoryPath, string rootId, string commitId, string branchName);
	}
}