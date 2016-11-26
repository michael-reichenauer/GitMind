using System;
using System.Threading.Tasks;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		Repository Repository { get; }

		bool IsPaused { get;}

		event EventHandler<RepositoryUpdatedEventArgs> RepositoryUpdated;

		bool IsRepositoryCached(string workingFolder);

		Task LoadRepositoryAsync(string workingFolder);

		Task GetFreshRepositoryAsync();

		Task UpdateRepositoryAsync();

		Task UpdateRepositoryAfterCommandAsync();
	}
}