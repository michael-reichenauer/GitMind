using System.Collections.Generic;
using GitMind.Utils.Git.Private;


namespace GitMind.GitModel.Private
{
	internal interface IBranchService
	{
		void AddActiveBranches(IReadOnlyList<GitBranch> branches, MRepository repository);

		void AddInactiveBranches(MRepository repository);

		void AddMissingInactiveBranches(MRepository repository);

		void AddMultiBranches(MRepository repository);
	}
}