using System.Threading.Tasks;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils.UI;


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

		Commit UnCommited { get; }

		Command<Commit> SetBranchCommand { get; }
		Command ShowSelectedDiffCommand { get; }

		DisabledStatus DisableStatus();
		void ShowBranch(BranchName branchName);
		Task RefreshAfterCommandAsync(bool useFreshRepository);
		void SetCurrentMerging(Branch branch);
	}
}