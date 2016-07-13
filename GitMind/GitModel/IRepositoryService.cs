using System.Threading.Tasks;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		Task<Repository> GetRepositoryAsync(bool useCache, string workingFolder);

		Task<Repository> UpdateRepositoryAsync(Repository repository);

		Task SetSpecifiedCommitBranchAsync(
			string commitId, string branchName, string gitRepositoryPath);
	}
}