using System.Threading.Tasks;
using GitMind.Git;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		Task<Repository> GetRepositoryAsync(IGitRepo gitRepo);
		Task<Repository> GetRepositoryAsync();
	}
}