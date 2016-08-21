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
			string workingFolder = viewModel.WorkingFolder;
			Window owner = viewModel.Owner;

			viewModel.SetIsInternalDialog(true);

			CrateBranchDialog dialog = new CrateBranchDialog(owner);
			if (dialog.ShowDialog() == true)
			{
				Progress.ShowDialog(owner, $"Create branch {dialog.BranchName} ...", async () =>
				{
					string branchName = dialog.BranchName;
					string commitId = branch.TipCommit.Id;
					if (commitId == Commit.UncommittedId)
					{
						commitId = branch.TipCommit.FirstParent.Id;
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