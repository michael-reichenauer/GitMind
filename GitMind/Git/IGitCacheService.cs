using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git
{
	internal interface IGitCacheService
	{
		Task<R<IGitRepo>> GetRepoAsync(string path);

		Task UpdateAsync(string path, IGitRepo gitRepo);
	}
}