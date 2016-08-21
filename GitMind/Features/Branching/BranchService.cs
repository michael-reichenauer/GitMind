using System.Threading.Tasks;
using System.Windows;
using GitMind.Common.ProgressHandling;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.RepositoryViews;


namespace GitMind.Features.Branching
{
	internal class BranchService : IBranchService
	{
		private readonly IGitService gitService;

		public BranchService()
			: this(new GitService())
		{
		}

		public BranchService(IGitService gitService)
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


		public Task SwitchBranchAsync(IRepositoryCommands repositoryCommands, Branch branch)
		{
			string workingFolder = repositoryCommands.WorkingFolder;
			Window owner = repositoryCommands.Owner;

			using (repositoryCommands.DisableStatus())
			{
				Progress.ShowDialog(owner, $"Switch to branch {branch.Name} ...", async () =>
				{
					await gitService.SwitchToBranchAsync(workingFolder, branch.Name);

					await repositoryCommands.RefreshAfterCommandAsync(false);
				});

				return Task.CompletedTask;
			}
		}


		public bool CanExecuteSwitchBranch(Branch branch)
		{
			return
				branch.Repository.Status.ConflictCount == 0
				&& !branch.Repository.Status.IsMerging
				&& branch.Repository.CurrentBranch.Id != branch.Id;
		}



		public Task SwitchToBranchCommitAsync(IRepositoryCommands repositoryCommands, Commit commit)
		{
			string workingFolder = repositoryCommands.WorkingFolder;
			Window owner = repositoryCommands.Owner;

			using (repositoryCommands.DisableStatus())
			{
				Progress.ShowDialog(owner, "Switch to commit ...", async () =>
				{
					string proposedNamed = commit == commit.Branch.TipCommit
						? commit.Branch.Name
						: $"_{commit.ShortId}";

					string branchName = await gitService.SwitchToCommitAsync(
						workingFolder, commit.CommitId, proposedNamed);

					if (branchName != null)
					{
						repositoryCommands.AddSpecifiedBranch(branchName);
					}

					await repositoryCommands.RefreshAfterCommandAsync(false);
				});

				return Task.CompletedTask;
			}
		}


		public bool CanExecuteSwitchToBranchCommit(Commit commit)
		{
			return
				commit.Repository.Status.StatusCount == 0
				&& !commit.Repository.Status.IsMerging
				&& commit.Repository.Status.ConflictCount == 0;
		}
	}
}