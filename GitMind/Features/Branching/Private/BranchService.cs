﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Committing;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.MainWindowViews;
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
		private readonly WorkingFolder workingFolder;
		private readonly IProgressService progress;
		private readonly IMessage message;
		private readonly WindowOwner owner;
		private readonly Lazy<IRepositoryCommands> lazyRepositoryCommands;


		public BranchService(
			IGitBranchService gitBranchService,
			IGitNetworkService gitNetworkService,
			ICommitService commitService,
			WorkingFolder workingFolder,
			IProgressService progressService,
			IMessage message,
			WindowOwner owner,
			Lazy<IRepositoryCommands> repositoryCommands)
		{
			this.gitBranchService = gitBranchService;
			this.gitNetworkService = gitNetworkService;
			this.commitService = commitService;
			this.workingFolder = workingFolder;
			this.progress = progressService;
			this.message = message;
			this.owner = owner;
			this.lazyRepositoryCommands = repositoryCommands;
		}


		public IRepositoryCommands repositoryCommands => lazyRepositoryCommands.Value;

		public Task CreateBranchAsync(Branch branch)
		{
			return CreateBranchFromCommitAsync(branch.TipCommit);
		}


		public Task CreateBranchFromCommitAsync(Commit commit)
		{
			using (repositoryCommands.DisableStatus())
			{
				CrateBranchDialog dialog = new CrateBranchDialog(owner);

				if (dialog.ShowDialog() == true)
				{
					BranchName branchName = dialog.BranchName;
					Log.Debug($"Create branch {dialog.BranchName}, from {commit.Branch} ...");

					progress.Show($"Create branch {dialog.BranchName} ...", async state =>
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
								state.SetText($"Publish branch {dialog.BranchName}...");

								R publish = await gitNetworkService.PublishBranchAsync(
									workingFolder, branchName, repositoryCommands.GetCredentialsHandler());
								if (publish.IsFaulted)
								{
									message.ShowWarning($"Failed to publish the branch {branchName}.");
								}
							}

							repositoryCommands.ShowBranch(branchName);
						}
						else
						{
							message.ShowWarning($"Failed to create branch {branchName}\n{result.Error.Exception.Message}");
						}

						state.SetText($"Updating status after create branch {branchName} ...");
						await repositoryCommands.RefreshAfterCommandAsync(true);
					});
				}

				return Task.CompletedTask;
			}
		}


		public void PublishBranch(Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				progress.Show($"Publish branch {branch.Name} ...", async state =>
				{
					R publish = await gitNetworkService.PublishBranchAsync(
						workingFolder, branch.Name, repositoryCommands.GetCredentialsHandler());

					if (publish.IsFaulted)
					{
						message.ShowWarning($"Failed to publish the branch {branch.Name}.\n{publish.Error.Exception.Message}");
					}

					state.SetText($"Updating status after publish {branch.Name} ...");
					await repositoryCommands.RefreshAfterCommandAsync(false);
				});
			}
		}


		public void PushBranch(Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				progress.Show($"Push branch {branch.Name} ...", async state =>
				{
					R result = await gitNetworkService.PushBranchAsync(
						workingFolder, branch.Name, repositoryCommands.GetCredentialsHandler());

					if (result.IsFaulted)
					{
						message.ShowWarning($"Failed to push the branch {branch.Name}.\n{result.Error.Exception.Message}");
					}

					state.SetText($"Updating status after push {branch.Name} ...");
					await repositoryCommands.RefreshAfterCommandAsync(true);
				});
			}
		}


		public void UpdateBranch(Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				progress.Show($"Update branch {branch.Name} ...", async state =>
				{
					R result;
					if (branch == branch.Repository.CurrentBranch ||
						branch.IsMainPart && branch.LocalSubBranch == branch.Repository.CurrentBranch)
					{
						Log.Debug("Update current branch");
						result = await gitNetworkService.FetchAsync(workingFolder);
						if (result.IsOk)
						{
							result = await gitBranchService.MergeCurrentBranchAsync(workingFolder);
						}
					}
					else
					{
						Log.Debug($"Update branch {branch.Name}");
						result = await gitNetworkService.FetchBranchAsync(workingFolder, branch.Name);
					}

					if (result.IsFaulted)
					{
						message.ShowWarning($"Failed to update the branch {branch.Name}.\n{result.Error.Exception.Message}");
					}

					state.SetText($"Updating status after update {branch.Name} ...");
					await repositoryCommands.RefreshAfterCommandAsync(false);
				});
			}
		}


		public Task SwitchBranchAsync(Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				progress.Show($"Switch to branch {branch.Name} ...", async state =>
				{
					R result = await gitBranchService.SwitchToBranchAsync(workingFolder, branch.Name);
					if (result.IsFaulted)
					{
						message.ShowWarning($"Failed to switch,\n{result.Error.Exception.Message}");
					}

					state.SetText($"Updating status after switch to {branch.Name} ...");
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



		public Task SwitchToBranchCommitAsync(Commit commit)
		{
			using (repositoryCommands.DisableStatus())
			{
				if (commit.IsRemoteAhead)
				{
					message.ShowInfo("Commit is remote, you must first update before switching to this commit.");
					return Task.CompletedTask;
				}

				progress.Show("Switch to commit ...", async state =>
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
						message.ShowWarning($"Failed to switch to the branch {branchName}.\n{switchedNamed.Error.Exception.Message}");
						repositoryCommands.ShowBranch(null);
					}

					state.SetText("Updating status after switch to commit ...");
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


		public void DeleteBranch(Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{
				if (branch.Name == BranchName.Master)
				{
					message.ShowWarning("You cannot delete master branch.");
					return;
				}

				if (!branch.IsRemote && branch == branch.Repository.CurrentBranch)
				{
					message.ShowWarning("You cannot delete current local branch.");
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
						message.ShowWarning("You cannot delete current local branch.");
						return;
					}

					if (!dialog.IsLocal && !dialog.IsRemote)
					{
						message.ShowWarning("Neither local nor remote branch was selected.");
						return;
					}

					DeleteBranch(branch, dialog.IsLocal, dialog.IsRemote);
				}
			}
		}


		private void DeleteBranch(
			Branch branch,
			bool isLocal,
			bool isRemote)
		{
			progress.Show(async state =>
			{
				if (isLocal)
				{
					state.SetText($"Delete local branch {branch.Name} ...");
					await DeleteBranchImpl(branch, false, false);
				}

				if (isRemote)
				{
					state.SetText($"Delete remote branch {branch.Name} ...");
					await DeleteBranchImpl(branch, true, !isLocal);
				}

				state.SetText($"Updating status after delete {branch.Name} ...");
				await repositoryCommands.RefreshAfterCommandAsync(true);
			});
		}

		private async Task DeleteBranchImpl(
			Branch branch,
			bool isRemote,
			bool isNoLongerLocal)
		{
			string text = isRemote ? "Remote" : "Local";

			if (!IsBranchFullyMerged(branch, isRemote, isNoLongerLocal))
			{

				if (!message.ShowWarningAskYesNo($"{text} branch '{branch.Name}' is not fully merged.\n" +
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
				message.ShowWarning($"Failed to delete {text} branch '{branch.Name}'\n{deleted.Error.Exception.Message}");
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


		public async Task MergeBranchAsync(Branch branch)
		{
			using (repositoryCommands.DisableStatus())
			{

				if (branch == branch.Repository.CurrentBranch)
				{
					message.ShowWarning("You cannot merge current branch into it self.");
					return;
				}

				if (branch.Repository.Status.ConflictCount > 0 || branch.Repository.Status.StatusCount > 0)
				{
					message.ShowInfo("You must first commit uncommitted changes before merging.");
					return;
				}

				Branch currentBranch = branch.Repository.CurrentBranch;
				progress.Show($"Merge branch {branch.Name} into {currentBranch.Name} ...", async text =>
				{
					await gitBranchService.MergeAsync(workingFolder, branch.Name);

					repositoryCommands.SetCurrentMerging(branch);
					text.SetText(
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
					await commitService.CommitChangesAsync();
				}
			}
		}
	}
}