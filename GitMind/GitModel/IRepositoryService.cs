using System;
using System.Threading.Tasks;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		Repository Repository { get; }

		event EventHandler<RepositoryUpdatedEventArgs> RepositoryUpdated;

		bool IsRepositoryCached(string workingFolder);

		Task LoadRepositoryAsync(string workingFolder);

		Task UpdateFreshRepositoryAsync();

		Task UpdateRepositoryAsync();
	}
}