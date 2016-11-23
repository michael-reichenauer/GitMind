using GitMind.Features.StatusHandling;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface IBranchService
	{
		void AddActiveBranches(GitRepository gitRepository, Status gitStatus, MRepository repository);

		void AddInactiveBranches(MRepository repository);

		void AddMissingInactiveBranches(MRepository repository);

		void AddMultiBranches(MRepository repository);
	}
}