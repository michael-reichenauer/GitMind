using GitMind.Git;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		Repository GetRepository(IGitRepo gitRepo);
	}
}