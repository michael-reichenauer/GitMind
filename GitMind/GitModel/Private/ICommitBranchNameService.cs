using System.Collections.Generic;


namespace GitMind.GitModel.Private
{
	internal interface ICommitBranchNameService
	{
		//void SetCommitBranchNames(
		//	IReadOnlyList<MSubBranch> branches, 
		//	IReadOnlyList<MCommit> commits, 
		//	MRepository repository);

		string GetBranchName(MCommit commit);
		void SetBranchTipCommitsNames(IReadOnlyList<MSubBranch> branches, MRepository repository);
		void SetMasterBranchCommits(IReadOnlyList<MSubBranch> branches, MRepository repository);


		void SetSpecifiedCommitBranchNames(
			IReadOnlyList<SpecifiedBranchName> specifiedNames,
			MRepository repository);


		void SetPullMergeCommitBranchNames(IReadOnlyList<MCommit> commits);


		void SetSubjectCommitBranchNames(
			IReadOnlyList<MCommit> commits, MRepository repository);


		void SetNeighborCommitNames(IReadOnlyList<MCommit> commits);
	}
}
