using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface IBranchService
	{
		IReadOnlyList<MSubBranch> AddSubBranches(
			IReadOnlyList<GitBranch> gitBranches, 
			MRepository mRepository, 
			IReadOnlyList<MCommit> commits);

		void SetBranchHierarchy(IReadOnlyList<MSubBranch> subBranches, MRepository mRepository);

		IReadOnlyList<MSubBranch> AddMultiBranches(
			IReadOnlyList<MCommit> commits, 
			IReadOnlyList<MSubBranch> branches,
			MRepository xmodel);
	}
}