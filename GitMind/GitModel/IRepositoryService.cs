using GitMind.Git;


namespace GitMind.GitModel
{
	internal interface IRepositoryService
	{
		Repository XGetModel(IGitRepo gitRepo);
	}
}