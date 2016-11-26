using System.Threading.Tasks;
using GitMind.Features.StatusHandling;


namespace GitMind.GitModel.Private
{
	internal interface IRepositoryStructureService
	{
		//Task<MRepository> GetAsync(string workingFolder);
		Task<MRepository> UpdateAsync(MRepository mRepository, Status status);
		//Task<MRepository> UpdateStatusAsync(MRepository mRepository, Status status);
		//Task<MRepository> UpdateRepoAsync(MRepository mRepository, Status status);
	}
}