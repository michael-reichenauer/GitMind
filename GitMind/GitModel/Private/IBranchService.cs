using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface IBranchService
	{
		void AddActiveBranches(IReadOnlyList<GitBranch> gitBranches, MRepository repository);

		void AddInactiveBranches(MRepository repository);

		void AddMissingInactiveBranches(MRepository repository);

		void AddMultiBranches(MRepository repository);
	}
}