using System.Collections.Generic;
using System.Threading.Tasks;


namespace GitMind.GitModel.Private
{
	internal interface ICacheService
	{
		Task CacheAsync(MRepository repository);
		Task<MRepository> TryGetRepositoryAsync(string gitRepositoryPath);
		//Task CacheCommitFilesAsync(List<CommitFiles> commitsFilesTask);
	}
}