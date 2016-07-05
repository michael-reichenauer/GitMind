using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Git;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		Task<Repository> GetRepositoryAsync(bool useCache);

		Task<Repository> UpdateRepositoryAsync(Repository repository);

		Task SetSpecifiedCommitBranchAsync(string commitId, string branchName);
	}
}