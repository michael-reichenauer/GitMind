using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface IBranchService
	{
		IReadOnlyList<MSubBranch> AddActiveBranches(
			IReadOnlyList<GitBranch> gitBranches, MRepository repository);

		IReadOnlyList<MSubBranch> AddInactiveBranches(
			IReadOnlyList<MCommit> commits, MRepository repository);

		IReadOnlyList<MSubBranch> AddMultiBranches(
			IReadOnlyList<MCommit> commits, MRepository repository);

		IReadOnlyList<MSubBranch> AddMissingInactiveBranches(
			IReadOnlyList<MCommit> commits, MRepository repository);

		void SetBranchHierarchy(IReadOnlyList<MSubBranch> subBranches, MRepository mRepository);
	}
}