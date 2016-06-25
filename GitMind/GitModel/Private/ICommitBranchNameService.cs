using System.Collections.Generic;


namespace GitMind.GitModel.Private
{
	internal interface ICommitBranchNameService
	{
		void SetCommitBranchNames(
			IReadOnlyList<MSubBranch> branches,
			IReadOnlyList<MCommit> commits,
			MRepository mRepository);

		string GetBranchName(MCommit mCommit);
		void SetBranchCommits(IReadOnlyList<MSubBranch> branches, MRepository xmodel);
	}
}
