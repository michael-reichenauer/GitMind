using System.Threading.Tasks;
using GitMind.Features.StatusHandling;


namespace GitMind.GitModel.Private
{
	internal interface IRepositoryStructureService
	{
		Task<MRepository> UpdateAsync(MRepository mRepository, Status status);
	}
}