using System.Threading.Tasks;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal interface IRepositoryCommands
	{
		Repository Repository { get; }
		Commit UnCommited { get; }
		CredentialHandler GetCredentialsHandler();
		Command<Branch> ShowBranchCommand { get; }
		Command<Branch> HideBranchCommand { get; }
		Command<Branch> DeleteBranchCommand { get; }
		Command<Branch> PublishBranchCommand { get; }
		Command<Branch> PushBranchCommand { get; }
		Command<Branch> UpdateBranchCommand { get; }
		Command<Commit> ShowDiffCommand { get; }
		Command ToggleDetailsCommand { get; }
		Command ShowUncommittedDetailsCommand { get; }
		Command ShowCurrentBranchCommand { get; }
		Command<Commit> SetBranchCommand { get; }
		//Command<Branch> MergeBranchCommand { get; }
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