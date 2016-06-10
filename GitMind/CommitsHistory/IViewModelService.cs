using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal interface IViewModelService
	{
		void Update(RepositoryViewModel repositoryViewModel, IReadOnlyList<string> specifiedBranchNames);
		int ToggleMergePoint(RepositoryViewModel repositoryViewModel, Commit commit);
		Task SetFilterAsync(RepositoryViewModel repositoryViewModel);
		void ShowBranch(RepositoryViewModel repositoryViewModel, Branch branch);
	}
}