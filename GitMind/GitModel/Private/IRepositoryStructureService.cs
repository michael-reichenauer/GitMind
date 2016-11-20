using System.Threading.Tasks;


namespace GitMind.GitModel.Private
{
	internal interface IRepositoryStructureService
	{
		Task<MRepository> UpdateAsync(MRepository mRepository);
	}
}