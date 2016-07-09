using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface ICommitsService
	{
		void AddBranchCommits(IGitRepo gitRepo, MRepository repository);
	}
}