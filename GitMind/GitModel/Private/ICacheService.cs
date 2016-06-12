using System.Threading.Tasks;


namespace GitMind.GitModel.Private
{
	internal interface ICacheService
	{
		Task CacheAsync(MRepository repository);
		Task<MRepository> TryGetAsync();
	}
}