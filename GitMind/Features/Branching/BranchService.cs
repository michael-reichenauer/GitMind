using System.Collections.Generic;
using System.Linq;
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
	/// <summary>
	/// Branch service
	/// </summary>
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

							R publish = await gitService.PublishBranchAsync(
								workingFolder, branchName, repositoryCommands.GetCredentialsHandler());
							if (publish.IsFaulted)
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

					R<string> branchName = await gitService.SwitchToCommitAsync(
						workingFolder, commit.CommitId, proposedNamed);

					if (branchName.HasValue)
					{
						repositoryCommands.AddSpecifiedBranch(branchName.Value);
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

			if (!IsBranchFullyMerged(branch, isRemote))
			{
				if (!MessageDialog.ShowWarningAskYesNo(owner,
					$"Branch '{branch.Name}' is not fully merged.\nDo you want to delete the branch anyway?"))
				{
					return;
				}
			}

			Progress.ShowDialog(owner, progressText, async () =>
			{
				R deleted = await gitService.DeleteBranchAsync(
					workingFolder, branch.Name, isRemote, repositoryCommands.GetCredentialsHandler());

				if (deleted.IsFaulted)
				{
					MessageDialog.ShowWarning(owner, $"Failed to delete Branch '{branch.Name}'");
				}

				await repositoryCommands.RefreshAfterCommandAsync(true);
			});
		}


		private bool IsBranchFullyMerged(Branch branch, bool isRemote)
		{
			if (!isRemote && branch.LocalAheadCount > 0)
			{
				// Trying to delete local branch, which has local unpublished commits
				return false;
			}

			if (branch.TipCommit.IsVirtual && branch.TipCommit.Id != Commit.UncommittedId)
			{
				// OK to delete branch, which is just a branch tip with a commit on another branch
				return true;
			}

			Stack<Commit> stack = new Stack<Commit>();
			stack.Push(branch.TipCommit);

			while (stack.Any())
			{
				Commit commit = stack.Pop();

				if ((commit.Branch.IsLocal && commit.Branch.IsRemote)
					|| (commit.Branch != branch && commit.Branch.IsActive))
				{
					// The commit is on a branch that is both local and remote
					// or the commit is on another active branch
					return true;
				}

				commit.Children.ForEach(child => stack.Push(child));
			}

			return false;
		}


		public async Task MergeBranchAsync(IRepositoryCommands repositoryCommands, Branch branch)
		{
			string workingFolder = repositoryCommands.WorkingFolder;
			Window owner = repositoryCommands.Owner;

			using (repositoryCommands.DisableStatus())
			{

				if (branch == branch.Repository.CurrentBranch)
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
					R<GitCommit> gitCommit = await gitService.MergeAsync(workingFolder, branch.Name);

					// Need to check value != null, since commit may not have been done, but merge is still OK
					if (gitCommit.HasValue && gitCommit.Value != null)
					{
						string commitId = gitCommit.Value.Id;
						Log.Debug($"Merged {branch.Name} into {currentBranch.Name} at {commitId}");
						await gitService.SetCommitBranchAsync(workingFolder, commitId, currentBranch.Name);
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