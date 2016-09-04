using System.Threading.Tasks;
using System.Windows;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Committing;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Features.Branching
{
	internal class BranchService : IBranchService
	{
		private readonly IGitService gitService;
		private readonly ICommitService commitService;

		public BranchService()
			: this(new GitService(), new CommitService())
		{
		}

		public BranchService(
			IGitService gitService,
			ICommitService commitService)
		{
			this.gitService = gitService;
			this.commitService = commitService;
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
					Log.Debug($"Create branch {dialog.BranchName}, from {commit.Branch} ...");
					Progress.ShowDialog(owner, $"Create branch {dialog.BranchName} ...", async progress =>
					{
						string branchName = dialog.BranchName;
						string commitId = commit.Id;
						if (commitId == Commit.UncommittedId)
						{
							commitId = commit.FirstParent.CommitId;
						}

						await gitService.CreateBranchAsync(workingFolder, branchName, commitId);
						Log.Debug($"Created branch {branchName}, from {commit.Branch}");

						if (dialog.IsPublish)
						{
							progress.SetText($"Publish branch {dialog.BranchName}...");

							bool isPublished = await gitService.PublishBranchAsync(
								workingFolder, branchName, repositoryCommands.GetCredentialsHandler());
							if (!isPublished)
							{
								MessageDialog.ShowWarning(owner, $"Failed to publish the branch {branchName}.");
							}
						}
						
						repositoryCommands.AddSpecifiedBranch(branchName);

						progress.SetText("Updating status ...");
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
				Progress.ShowDialog(owner, "Switch to commit ...", async progress =>
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


		public void DeleteLocalBranch(IRepositoryCommands repositoryCommands, Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				Window owner = repositoryCommands.Owner;

				if (branch == branch.Repository.CurrentBranch)
				{
					MessageDialog.ShowWarning(owner, "You cannot delete current local branch.");
					return;
				}

				DeleteBranch(repositoryCommands, branch, false, $"Delete local branch {branch.Name} ...");
			}
		}


		public void DeleteRemoteBranch(IRepositoryCommands repositoryCommands, Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				DeleteBranch(repositoryCommands, branch, true, $"Delete remote branch {branch.Name} ...");
			}
		}


		private void DeleteBranch(
			IRepositoryCommands repositoryCommands,
			Branch branch, 
			bool isRemote, 
			string progressText)
		{
			string workingFolder = repositoryCommands.WorkingFolder;
			Window owner = repositoryCommands.Owner;

			if (branch.Name == "master")
			{
				MessageDialog.ShowWarning(owner, "You cannot delete master branch.");
				return;
			}

			Progress.ShowDialog(owner, progressText, async () =>
			{
				R deleted = await gitService.TryDeleteBranchAsync(
					workingFolder, branch.Name, isRemote, false, repositoryCommands.GetCredentialsHandler());

				if (deleted.IsFaulted)
				{
					if (MessageDialog.ShowWarningAskYesNo(owner,
						$"Branch '{branch.Name}' is not fully merged.\nDo you want to delete the branch anyway?"))
					{
						await gitService.TryDeleteBranchAsync(
							workingFolder, branch.Name, isRemote, true, repositoryCommands.GetCredentialsHandler());
					}
					else
					{
						return;
					}
				}

				await repositoryCommands.RefreshAfterCommandAsync(true);
			});
		}


		public async Task MergeBranchAsync(IRepositoryCommands repositoryCommands, Branch branch)
		{
			string workingFolder = repositoryCommands.WorkingFolder;
			Window owner = repositoryCommands.Owner;

			using (repositoryCommands.DisableStatus())
			{

				if (branch == branch. Repository.CurrentBranch)
				{
					MessageDialog.ShowWarning(owner, "You cannot merge current branch into it self.");
					return;
				}

				if (branch.Repository.Status.ConflictCount > 0 || branch.Repository.Status.StatusCount > 0)
				{
					MessageDialog.ShowInfo(
						owner, "You must first commit uncommitted changes before merging.");
					return;
				}

				Branch currentBranch = branch.Repository.CurrentBranch;
				Progress.ShowDialog(owner, $"Merge branch {branch.Name} into {currentBranch.Name} ...", 
					async () =>
				{				
					GitCommit gitCommit = await gitService.MergeAsync(workingFolder, branch.Name);

					if (gitCommit != null)
					{
						Log.Debug($"Merged {branch.Name} into {currentBranch.Name} at {gitCommit.Id}");
						await gitService.SetCommitBranchAsync(workingFolder, gitCommit.Id, currentBranch.Name);
					}

					repositoryCommands.SetCurrentMerging(branch);
					await repositoryCommands.RefreshAfterCommandAsync(false);
				});

				if (repositoryCommands.Repository.Status.StatusCount == 0)
				{
					MessageDialog.ShowInfo(owner, "No changes in this merge, nothing to merge.");
					return;
				}

				if (branch.Repository.Status.ConflictCount == 0)
				{				
					await commitService.CommitChangesAsync(repositoryCommands);
				}
			}
		}
	}
}