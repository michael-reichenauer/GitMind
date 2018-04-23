using System;
using System.Threading.Tasks;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		Repository Repository { get; }

		bool IsPaused { get;}

		event EventHandler<RepositoryUpdatedEventArgs> RepositoryUpdated;

		event EventHandler<RepositoryErrorEventArgs> RepositoryErrorChanged;

		bool IsRepositoryCached(string workingFolder);

		Task LoadFreshRepositoryAsync(string workingFolder);

		Task GetFreshRepositoryAsync();

		Task CheckLocalRepositoryAsync();

		Task CheckBranchTipCommitsAsync();

		Task UpdateRepositoryAfterCommandAsync();

		Task RefreshAfterCommandAsync(bool useFreshRepository);

		Task CheckRemoteChangesAsync(bool isFetchNotes, bool isManual = false);

		Task GetRemoteAndFreshRepositoryAsync(bool isManual);
		Task<bool> LoadCachedRepositoryAsync(string workingFolder);
	}
}