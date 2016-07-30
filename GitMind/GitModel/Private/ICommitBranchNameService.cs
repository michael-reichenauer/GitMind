using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface ICommitBranchNameService
	{
		string GetBranchName(MCommit commit);

		void SetMasterBranchCommits(MRepository repository);

		void SetBranchTipCommitsNames(MRepository repository);

		void SetSpecifiedCommitBranchNames(
			IReadOnlyList<GitSpecifiedNames> specifiedNames, MRepository repository);

		void SetCommitBranchNames(
			IReadOnlyList<GitSpecifiedNames> commitBranches, MRepository repository);

		void SetNeighborCommitNames(MRepository repository);
	}
}
