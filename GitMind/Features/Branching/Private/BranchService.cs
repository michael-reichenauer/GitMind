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


namespace GitMind.Features.Branching.Private
{
	/// <summary>
	/// Branch service
	/// </summary>
	internal class BranchService : IBranchService
	{
		private readonly IGitBranchService gitBranchService;
		private readonly IGitNetworkService gitNetworkService;
		private readonly ICommitService commitService;


		public BranchService()
			: this(new GitBranchService(), new GitNetworkService(),  new CommitService())
		{
		}

		public BranchService(
			IGitBranchService gitBranchService,
			IGitNetworkService gitNetworkService,
			ICommitService commitService)
		{
			this.gitBranchService = gitBranchService;
			this.gitNetworkService = gitNetworkService;
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
					BranchName branchName = dialog.BranchName;
					Log.Debug($"Create branch {dialog.BranchName}, from {commit.Branch} ...");

					Progress.ShowDialog(owner, $"Create branch {dialog.BranchName} ...", async progress =>
					{					
						string commitId = commit.Id;
						if (commitId == Commit.UncommittedId || commit.IsVirtual)
						{
							commitId = commit.FirstParent.CommitId;
						}

						R result = await gitBranchService.CreateBranchAsync(workingFolder, branchName, commitId);
						if (result.IsOk)
						{
							Log.Debug($"Created branch {branchName}, from {commit.Branch}");

							if (dialog.IsPublish)
							{
								progress.SetText($"Publish branch {dialog.BranchName}...");

								R publish = await gitNetworkService.PublishBranchAsync(
									workingFolder, branchName, repositoryCommands.GetCredentialsHandler());
								if (publish.IsFaulted)
								{
									Message.ShowWarning(owner, $"Failed to publish the branch {branchName}.");
								}
							}

							repositoryCommands.ShowBranch(branchName);
						}
						else
						{
							Message.ShowWarning(owner, $"Failed to create branch {branchName}\n{result}");
						}

						progress.SetText($"Updating status after create branch {branchName} ...");
						await repositoryCommands.RefreshAfterCommandAsync(true);
					});
				}

				return Task.CompletedTask;
			}
		}


		public void PublishBranch(IRepositoryCommands repositoryCommands, Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				string workingFolder = repositoryCommands.WorkingFolder;
				Window owner = repositoryCommands.Owner;

				Progress.ShowDialog(owner, $"Publish branch {branch.Name} ...", async progress =>
				{
					R publish = await gitNetworkService.PublishBranchAsync(
						workingFolder, branch.Name, repositoryCommands.GetCredentialsHandler());

					if (publish.IsFaulted)
					{
						Message.ShowWarning(owner, $"Failed to publish the branch {branch.Name}.");
					}

					progress.SetText($"Updating status after publish {branch.Name} ...");
					await repositoryCommands.RefreshAfterCommandAsync(false);
				});
			}
		}


		public void PushBranch(IRepositoryCommands repositoryCommands, Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				string workingFolder = repositoryCommands.WorkingFolder;
				Window owner = repositoryCommands.Owner;

				Progress.ShowDialog(owner, $"Push branch {branch.Name} ...", async progress =>
				{
					await gitNetworkService.PushBranchAsync(
						workingFolder, branch.Name, repositoryCommands.GetCredentialsHandler());

					progress.SetText($"Updating status after push {branch.Name} ...");
					await repositoryCommands.RefreshAfterCommandAsync(true);
				});
			}
		}


		public void UpdateBranch(IRepositoryCommands repositoryCommands, Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				string workingFolder = repositoryCommands.WorkingFolder;
				Window owner = repositoryCommands.Owner;

				Progress.ShowDialog(owner, $"Update branch {branch.Name} ...", async progress =>
				{
					if (branch == branch.Repository.CurrentBranch || 
					branch.IsMainPart && branch.LocalSubBranch == branch.Repository.CurrentBranch)
					{
						Log.Debug("Update current branch");
						await gitNetworkService.FetchAsync(workingFolder);
						await gitBranchService.MergeCurrentBranchAsync(workingFolder);
					}
					else
					{
						Log.Debug($"Update branch {branch.Name}");
						await gitNetworkService.FetchBranchAsync(workingFolder, branch.Name);
					}	

					progress.SetText($"Updating status after update {branch.Name} ...");
					await repositoryCommands.RefreshAfterCommandAsync(false);
				});
			}
		}


		public Task SwitchBranchAsync(IRepositoryCommands repositoryCommands, Branch branch)
		{
			string workingFolder = repositoryCommands.WorkingFolder;
			Window owner = repositoryCommands.Owner;

			using (repositoryCommands.DisableStatus())
			{
				Progress.ShowDialog(owner, $"Switch to branch {branch.Name} ...", async progress =>
				{
					R result = await gitBranchService.SwitchToBranchAsync(workingFolder, branch.Name);
					if (result.IsFaulted)
					{
						Message.ShowWarning(owner, $"Failed to switch,\n{result}");
					}

					progress.SetText($"Updating status after switch to {branch.Name} ...");
					await repositoryCommands.RefreshAfterCommandAsync(true);
				});

				return Task.CompletedTask;
			}
		}


		public bool CanExecuteSwitchBranch(Branch branch)
		{
			return
				branch.Repository.Status.ConflictCount == 0
				&& !branch.Repository.Status.IsMerging
				&& !branch.IsCurrentBranch;
		}



		public Task SwitchToBranchCommitAsync(IRepositoryCommands repositoryCommands, Commit commit)
		{
			string workingFolder = repositoryCommands.WorkingFolder;
			Window owner = repositoryCommands.Owner;

			using (repositoryCommands.DisableStatus())
			{
				if (commit.IsRemoteAhead)
				{
					Message.ShowInfo(
						owner, "Commit is remote, you must first update before switching to this commit.");
					return Task.CompletedTask;
				}

				Progress.ShowDialog(owner, "Switch to commit ...", async progress =>
				{
					BranchName branchName = commit == commit.Branch.TipCommit ? commit.Branch.Name : null;

					R<BranchName> switchedNamed = await gitBranchService.SwitchToCommitAsync(
						workingFolder, commit.CommitId, branchName);

					if (switchedNamed.HasValue)
					{
						repositoryCommands.ShowBranch(switchedNamed.Value);
					}
					else
					{
						// Show current branch
						repositoryCommands.ShowBranch(null);
					}	

					progress.SetText("Updating status after switch to commit ...");
					await repositoryCommands.RefreshAfterCommandAsync(true);
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


		public void DeleteBranch(IRepositoryCommands repositoryCommands, Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				Window owner = repositoryCommands.Owner;

				if (branch.Name == BranchName.Master)
				{
					Message.ShowWarning(owner, "You cannot delete master branch.");
					return;
				}

				if (!branch.IsRemote && branch == branch.Repository.CurrentBranch)
				{
					Message.ShowWarning(owner, "You cannot delete current local branch.");
					return;
				}

				DeleteBranchDialog dialog = new DeleteBranchDialog(
					owner,
					branch.Name,
					branch.IsLocal && branch != branch.Repository.CurrentBranch,
					branch.IsRemote);

				if (dialog.ShowDialog() == true)
				{
					if (dialog.IsLocal && branch == branch.Repository.CurrentBranch)
					{
						Message.ShowWarning(owner, "You cannot delete current local branch.");
						return;
					}

					if (!dialog.IsLocal && !dialog.IsRemote)
					{
						Message.ShowWarning(owner, "Neither local nor remote branch was selected.");
						return;
					}

					DeleteBranch(repositoryCommands, branch, dialog.IsLocal, dialog.IsRemote);
				}
			}
		}


		private void DeleteBranch(
			IRepositoryCommands repositoryCommands,
			Branch branch,
			bool isLocal,
			bool isRemote)
		{
			Window owner = repositoryCommands.Owner;

			Progress.ShowDialog(owner, async progress =>
			{
				if (isLocal)
				{
					progress.SetText($"Delete local branch {branch.Name} ...");
					await DeleteBranchImpl(repositoryCommands, branch, false, false);
				}

				if (isRemote)
				{
					progress.SetText($"Delete remote branch {branch.Name} ...");
					await DeleteBranchImpl(repositoryCommands, branch, true, !isLocal);
				}

				progress.SetText($"Updating status after delete {branch.Name} ...");
				await repositoryCommands.RefreshAfterCommandAsync(true);
			});
		}

		private async Task DeleteBranchImpl(
			IRepositoryCommands repositoryCommands,
			Branch branch,
			bool isRemote,
			bool isNoLongerLocal)
		{
			string workingFolder = repositoryCommands.WorkingFolder;
			Window owner = repositoryCommands.Owner;
			string text = isRemote ? "Remote" : "Local";

			if (!IsBranchFullyMerged(branch, isRemote, isNoLongerLocal))
			{
				
				if (!Message.ShowWarningAskYesNo(owner,
					$"{text} branch '{branch.Name}' is not fully merged.\n" +
					"Do you want to delete the branch anyway?"))
				{
					return;
				}
			}

			R deleted;
			if (isRemote)
			{
				CredentialHandler credentialsHandler = repositoryCommands.GetCredentialsHandler();

				deleted = await gitNetworkService.DeleteRemoteBranchAsync(
					workingFolder, branch.Name, credentialsHandler);
			}
			else
			{
				deleted = await gitBranchService.DeleteLocalBranchAsync(workingFolder, branch.Name);
			}
		

			if (deleted.IsFaulted)
			{
				Message.ShowWarning(owner, $"Failed to delete {text} branch '{branch.Name}'");
			}	
		}


		private bool IsBranchFullyMerged(Branch branch, bool isRemote, bool isNoLongerLocal)
		{
			if (branch.TipCommit.IsVirtual && branch.TipCommit.Id != Commit.UncommittedId)
			{
				// OK to delete branch, which is just a branch tip with a commit on another branch
				return true;
			}

			if (isRemote && isNoLongerLocal)
			{
				return false;
			}

			Stack<Commit> stack = new Stack<Commit>();
			stack.Push(branch.TipCommit);

			while (stack.Any())
			{
				Commit commit = stack.Pop();

				if ((commit.Branch.IsLocal && commit.Branch.IsRemote)
					|| (commit.Branch != branch && commit.Branch.IsActive))
				{
					if (!(commit.Branch == branch && !isRemote && branch.LocalAheadCount > 0))
					{
						// The commit is on a branch that is both local and remote
						// or the commit is on another active branch
						// but branch has no unpublished local commits
						return true;
					}
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
					Message.ShowWarning(owner, "You cannot merge current branch into it self.");
					return;
				}

				if (branch.Repository.Status.ConflictCount > 0 || branch.Repository.Status.StatusCount > 0)
				{
					Message.ShowInfo(
						owner, "You must first commit uncommitted changes before merging.");
					return;
				}

				Branch currentBranch = branch.Repository.CurrentBranch;
				Progress.ShowDialog(owner, $"Merge branch {branch.Name} into {currentBranch.Name} ...",
					async progress =>
				{
					await gitBranchService.MergeAsync(workingFolder, branch.Name);

					repositoryCommands.SetCurrentMerging(branch);
					progress.SetText(
						$"Updating status after merge {branch.Name} into {currentBranch.Name} ...");
					await repositoryCommands.RefreshAfterCommandAsync(false);
				});

				//if (repositoryCommands.Repository.Status.StatusCount == 0)
				//{
				//	MessageDialog.ShowInfo(owner, "No changes in this merge, nothing to merge.");
				//	return;
				//}

				if (repositoryCommands.Repository.Status.ConflictCount == 0)
				{
					await commitService.CommitChangesAsync(repositoryCommands);
				}
			}
		}
	}
}