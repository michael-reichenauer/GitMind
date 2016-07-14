using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface IBranchService
	{
		void AddActiveBranches(GitRepository gitRepository, GitStatus gitStatus, MRepository repository);

		void AddInactiveBranches(MRepository repository);

		void AddMissingInactiveBranches(MRepository repository);

		void AddMultiBranches(MRepository repository);
	}
}