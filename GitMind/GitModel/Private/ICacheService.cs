using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal interface ICacheService
	{
		Task<R<IGitRepo>> GetRepoAsync(string path);

		Task UpdateAsync(string path, IGitRepo gitRepo);
		Task Cache(MRepository repository);
	}
}