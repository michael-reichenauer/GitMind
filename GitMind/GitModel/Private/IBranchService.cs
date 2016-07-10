namespace GitMind.GitModel.Private
{
	internal interface IBranchService
	{
		void AddActiveBranches(LibGit2Sharp.Repository repo, MRepository repository);

		void AddInactiveBranches(MRepository repository);

		void AddMissingInactiveBranches(MRepository repository);

		void AddMultiBranches(MRepository repository);
	}
}