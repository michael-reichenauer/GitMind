using System;
using System.Threading.Tasks;
using GitMind.Features.StatusHandling.Private;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		Repository Repository { get; }

		void Monitor(string workingFolder);

		event EventHandler<StatusChangedEventArgs> StatusChanged;

		event EventHandler<RepoChangedEventArgs> RepoChanged;

		bool IsRepositoryCached(string workingFolder);

		Task InitialCachedOrFreshRepositoryAsync(string workingFolder);

		Task UpdateFreshRepositoryAsync();

		Task UpdateRepositoryAsync();
	}
}