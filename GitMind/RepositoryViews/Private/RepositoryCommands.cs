using System;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews.Private
{
	internal class RepositoryCommands : IRepositoryCommands
	{
		private readonly Lazy<RepositoryViewModel> lazyRepositoryViewModel;


		public RepositoryCommands(Lazy<RepositoryViewModel> lazyRepositoryViewModel)
		{
			this.lazyRepositoryViewModel = lazyRepositoryViewModel;
		}


		private RepositoryViewModel viewModel => lazyRepositoryViewModel.Value;

		public Repository Repository => viewModel.Repository;

		public bool IsShowCommitDetails => viewModel.IsShowCommitDetails;
		public void ShowCommitDetails() => viewModel.ShowCommitDetails();

		public void ToggleCommitDetails() => viewModel.ToggleCommitDetails();
		public void ShowUncommittedDetails() => viewModel.ShowUncommittedDetails();


		public void ShowBranch(Branch branch) => viewModel.ShowBranch(branch);
		public void ShowCurrentBranch() => viewModel.ShowCurrentBranch();


		public Commit UnCommited => viewModel.UnCommited;
		public Command<Branch> ShowBranchCommand => viewModel.ShowBranchCommand;
		public Command<Branch> HideBranchCommand => viewModel.HideBranchCommand;
		public Command<Commit> ShowDiffCommand => viewModel.ShowDiffCommand;
		public Command<Commit> SetBranchCommand => viewModel.SetBranchCommand;
		public Command UndoCleanWorkingFolderCommand => viewModel.UndoCleanWorkingFolderCommand;
		public Command ShowSelectedDiffCommand => viewModel.ShowSelectedDiffCommand;
		public DisabledStatus DisableStatus() => viewModel.DisableStatus();

		public void ShowBranch(BranchName branchName) => viewModel.ShowBranch(branchName);

		public Task RefreshAfterCommandAsync(bool useFreshRepository)
			=> viewModel.RefreshAfterCommandAsync(useFreshRepository);

		public void SetCurrentMerging(Branch branch) => viewModel.SetCurrentMerging(branch);
	}
}