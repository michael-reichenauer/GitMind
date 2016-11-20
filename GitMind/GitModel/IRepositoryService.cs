using System;
using System.Threading.Tasks;
using GitMind.Features.StatusHandling.Private;
using GitMind.Git;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		void Monitor(string workingFolder);

		event EventHandler<StatusChangedEventArgs> StatusChanged;

		event EventHandler<RepoChangedEventArgs> RepoChanged;

		bool IsRepositoryCached(string workingFolder);

		Task<Repository> GetCachedOrFreshRepositoryAsync(string workingFolder);

		Task<Repository> GetFreshRepositoryAsync(string workingFolder);

		Task<Repository> UpdateRepositoryAsync(Repository repository);
	}
}