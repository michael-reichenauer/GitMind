using System.Collections.Generic;


namespace GitMind.GitModel.Private
{
	internal interface ICommitBranchNameService
	{
		void SetCommitBranchNames(
			IReadOnlyList<MCommit> commits,
			IReadOnlyList<SpecifiedBranchName> specifiedBranches,
			MRepository repository);

		void SetCommitBranchNames(
			IReadOnlyList<MSubBranch> branches, 
			IReadOnlyList<MCommit> commits, 
			MRepository repository);

		string GetBranchName(MCommit commit);
		void SetBranchCommits(IReadOnlyList<MSubBranch> branches, MRepository repository);
	}
}
