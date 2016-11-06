using System.Threading.Tasks;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal interface IRepositoryCommands
	{
		Repository Repository { get; }
		bool IsShowCommitDetails { get; set; }
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
		Command UndoUncommittedChangesCommand { get; }
		Command<Commit> UncommitCommand { get; }
		Command ShowUncommittedDiffCommand { get; }
		Command ShowSelectedDiffCommand { get; }
		Command TryUpdateAllBranchesCommand { get; }
		Command PullCurrentBranchCommand { get; }
		Command TryPushAllBranchesCommand { get; }
		Command PushCurrentBranchCommand { get; }

		DisabledStatus DisableStatus();
		void ShowBranch(BranchName branchName);
		Task RefreshAfterCommandAsync(bool useFreshRepository);
		void SetCurrentMerging(Branch branch);
	}
}