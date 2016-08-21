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


		public Task CreateBranchAsync(IRepositoryCommands repositoryCommands, Branch branch)
		{
			return CreateBranchFromCommitAsync(repositoryCommands, branch.TipCommit);
		}


		public Task CreateBranchFromCommitAsync(IRepositoryCommands repositoryCommands, Commit commit)
		{
			using (repositoryCommands.DisableStatus())
			{
				string workingFolder = repositoryCommands.WorkingFolder;
				Window owner = repositoryCommands.Owner;

				CrateBranchDialog dialog = new CrateBranchDialog(owner);

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
						repositoryCommands.AddSpecifiedBranch(branchName);

						await repositoryCommands.RefreshAfterCommandAsync(true);
					});
				}

				return Task.CompletedTask;
			}
		}
	}
}