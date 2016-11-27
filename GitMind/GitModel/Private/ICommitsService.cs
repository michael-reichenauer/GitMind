using GitMind.Features.StatusHandling;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface ICommitsService
	{
		void AddBranchCommits(GitRepository gitRepository, MRepository repository);
	}
}