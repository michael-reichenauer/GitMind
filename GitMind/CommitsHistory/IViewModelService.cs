using System.Threading.Tasks;
using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal interface IViewModelService
	{
		void Update(RepositoryViewModel repositoryViewModel);
		int ToggleMergePoint(RepositoryViewModel repositoryViewModel, Commit commit);
		void SetFilter(RepositoryViewModel repositoryViewModel, string filterText);
		void ShowBranch(RepositoryViewModel repositoryViewModel, Branch branch);
	}
}