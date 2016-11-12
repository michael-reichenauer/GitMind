using System.Threading.Tasks;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal interface IRepositoryCommands
	{
		Repository Repository { get; }

		bool IsShowCommitDetails { get;}

		void ShowCommitDetails();
		void ToggleCommitDetails();


		Commit UnCommited { get; }

		Command<Branch> ShowBranchCommand { get; }
		Command<Branch> HideBranchCommand { get; }
	
		Command<Branch> PublishBranchCommand { get; }
		Command<Branch> PushBranchCommand { get; }
		Command<Branch> UpdateBranchCommand { get; }
		Command<Commit> ShowDiffCommand { get; }
		Command ShowUncommittedDetailsCommand { get; }
		Command ShowCurrentBranchCommand { get; }
		Command<Commit> SetBranchCommand { get; }
		Command UndoCleanWorkingFolderCommand { get; }
		Command ShowSelectedDiffCommand { get; }

		DisabledStatus DisableStatus();
		void ShowBranch(BranchName branchName);
		Task RefreshAfterCommandAsync(bool useFreshRepository);
		void SetCurrentMerging(Branch branch);
	}
}