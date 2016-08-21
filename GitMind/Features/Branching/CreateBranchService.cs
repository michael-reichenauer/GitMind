using System.Threading.Tasks;
using System.Windows;
using GitMind.Common.ProgressHandling;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.RepositoryViews;


namespace GitMind.Features.Branching
{
	internal class CreateBranchService : ICreateBranchService
	{
		private readonly IGitService gitService;

		public CreateBranchService()
			: this(new GitService())
		{
		}

		public CreateBranchService(IGitService gitService)
		{
			this.gitService = gitService;
		}


		public Task CreateBranchAsync(RepositoryViewModel viewModel, Branch branch)
		{
			return CreateBranchFromCommitAsync(viewModel, branch.TipCommit);
		}


		public Task CreateBranchFromCommitAsync(RepositoryViewModel viewModel, Commit commit)
		{
			string workingFolder = viewModel.WorkingFolder;
			Window owner = viewModel.Owner;

			CrateBranchDialog dialog = new CrateBranchDialog(owner);

			viewModel.SetIsInternalDialog(true);
			if (dialog.ShowDialog() == true)
			{
				Progress.ShowDialog(owner, $"Create branch {dialog.BranchName} ...", async () =>
				{
					string branchName = dialog.BranchName;
					string commitId = commit.Id;
					if (commitId == Commit.UncommittedId)
					{
						commitId = commit.FirstParent.CommitId;
					}

					bool isPublish = dialog.IsPublish;

					await gitService.CreateBranchAsync(workingFolder, branchName, commitId, isPublish);
					viewModel.AddSpecifiedBranch(branchName);
					await viewModel.RefreshAfterCommandAsync(true);
				});
			}

			owner.Focus();

			viewModel.SetIsInternalDialog(false);
			return Task.CompletedTask;
		}
	}
}