using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Utils.Git;


namespace GitMind.GitModel.Private
{
	internal interface IRepositoryStructureService
	{
		//Task<MRepository> GetAsync(string workingFolder);
		Task<MRepository> UpdateAsync(MRepository mRepository, GitStatus2 status, IReadOnlyList<string> branchIds);
		//Task<MRepository> UpdateStatusAsync(MRepository mRepository, Status status);
		//Task<MRepository> UpdateRepoAsync(MRepository mRepository, Status status);
	}
}