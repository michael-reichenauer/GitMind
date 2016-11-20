using System.Threading.Tasks;


namespace GitMind.GitModel.Private
{
	internal interface ICacheService
	{
		Task CacheAsync(MRepository repository);
		bool IsRepositoryCached(string workingFolder);
		Task<MRepository> TryGetRepositoryAsync(string gitRepositoryPath);
		//Task CacheCommitFilesAsync(List<CommitFiles> commitsFilesTask);
		
	}
}