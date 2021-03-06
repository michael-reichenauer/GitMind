using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils.Git;


namespace GitMind.RepositoryViews
{
	internal interface IRepositoryCommands
	{
		void ShowCommitDetails();
		void ToggleCommitDetails();
		void ShowUncommittedDetails();

		void ShowBranch(Branch branch);
		void ShowCurrentBranch();
		void ShowDiff(Commit commit);

		Task ShowSelectedDiffAsync();

		Commit UnCommited { get; }

		void ShowBranch(BranchName branchName);

		void SetCurrentMerging(Branch branch, CommitSha commitSha);
		void RefreshView();
	}
}