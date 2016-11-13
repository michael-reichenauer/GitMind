using System.Threading.Tasks;
using GitMind.Git;
using GitMind.GitModel;


namespace GitMind.RepositoryViews
{
	internal interface IRepositoryCommands
	{
		Repository Repository { get; }

		void ShowCommitDetails();
		void ToggleCommitDetails();
		void ShowUncommittedDetails();

		void ShowBranch(Branch branch);
		void ShowCurrentBranch();
		void ShowDiff(Commit commit);

		Task ShowSelectedDiffAsync();

		Commit UnCommited { get; }

		DisabledStatus DisableStatus();
		void ShowBranch(BranchName branchName);
		Task RefreshAfterCommandAsync(bool useFreshRepository);
		void SetCurrentMerging(Branch branch);
	}
}