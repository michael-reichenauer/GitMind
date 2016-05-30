using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal interface IViewModelService
	{
		void Update(RepositoryViewModel repositoryViewModel, Repository repository);
	}
}