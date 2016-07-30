using System.Threading.Tasks;
using System.Windows.Media;
using GitMind.GitModel;


namespace GitMind.RepositoryViews
{
	internal interface IViewModelService
	{
		void UpdateViewModel(RepositoryViewModel repositoryViewModel);

		int ToggleMergePoint(RepositoryViewModel repositoryViewModel, Commit commit);

		Task SetFilterAsync(RepositoryViewModel repositoryViewModel, string filterText);

		void ShowBranch(RepositoryViewModel repositoryViewModel, Branch branch);

		void HideBranch(RepositoryViewModel repositoryViewModel, Branch branch);
		Brush GetSubjectBrush(Commit commit);
	}
}