using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface IBranchService
	{
		IReadOnlyList<MSubBranch> AddActiveBranches(
			IReadOnlyList<GitBranch> gitBranches, MRepository repository);

		IReadOnlyList<MSubBranch> AddInactiveBranches(MRepository repository);

		IReadOnlyList<MSubBranch> AddMissingInactiveBranches(MRepository repository);

		IReadOnlyList<MSubBranch> AddMultiBranches(MRepository repository);
	}
}