using System;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;
using GitMind.GitModel;


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

		public void RefreshView() => viewModel.RefreshView();

		public bool IsShowCommitDetails => viewModel.IsShowCommitDetails;
		public void ShowCommitDetails() => viewModel.ShowCommitDetails();

		public void ToggleCommitDetails() => viewModel.ToggleCommitDetails();
		public void ShowUncommittedDetails() => viewModel.ShowUncommittedDetails();


		public void ShowBranch(Branch branch) => viewModel.ShowBranch(branch);
		public void ShowCurrentBranch() => viewModel.ShowCurrentBranch();
		public void ShowDiff(Commit commit) => viewModel.ShowDiff(commit);
		public Task ShowSelectedDiffAsync() => viewModel.ShowSelectedDiffAsync();

		public Commit UnCommited => viewModel.UnCommited;

		public void ShowBranch(BranchName branchName) => viewModel.ShowBranch(branchName);

		public void SetCurrentMerging(Branch branch, CommitSha commitSha) => viewModel.SetCurrentMerging(branch, commitSha);
	}
}