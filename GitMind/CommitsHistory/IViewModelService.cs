using System.Threading.Tasks;
using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal interface IViewModelService
	{
		void Update(RepositoryViewModel repositoryViewModel, Repository repository);
		void ToggleMergePoint(RepositoryViewModel repositoryViewModel, Commit commit);
	}
}