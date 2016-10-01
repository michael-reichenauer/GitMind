using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Features.Branching.Private
{
	internal class GitBranchesService : IGitBranchesService
	{
		private static readonly MergeOptions MergeNoFastForward = new MergeOptions
			{ FastForwardStrategy = FastForwardStrategy.NoFastForward, CommitOnSuccess = false };

		private static readonly TimeSpan PushTimeout = TimeSpan.FromSeconds(30);

		private readonly IRepoCaller repoCaller;

		public GitBranchesService()
			: this(new RepoCaller())
		{
		}


		public GitBranchesService(IRepoCaller repoCaller)
		{
			this.repoCaller = repoCaller;
		}


		public Task<R<GitCommit>> MergeAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Merge branch {branchName} into current branch ...");
			return repoCaller.UseLibRepoAsync(workingFolder, repository =>
			{
				Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);

				Branch localbranch = repository.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));
				Branch remoteBranch = repository.Branches.FirstOrDefault(b => b.FriendlyName == "origin/" + branchName);

				Branch branch = localbranch ?? remoteBranch;
				if (localbranch != null && remoteBranch != null)
				{
					if (remoteBranch.Tip.Committer.When.LocalDateTime > localbranch.Tip.Committer.When.LocalDateTime)
					{
						branch = remoteBranch;
					}
				}

				if (branch != null)
				{
					MergeResult mergeResult = repository.Merge(branch, committer, MergeNoFastForward);
					if (mergeResult?.Commit != null)
					{
						return new GitCommit(mergeResult.Commit);
					}
					else
					{
						RepositoryStatus repositoryStatus = repository.RetrieveStatus(new StatusOptions());

						if (!repositoryStatus.IsDirty)
						{
							// Empty merge with no changes, lets reset merge since there is nothing to merge
							repository.Reset(ResetMode.Hard);
						}

						return null;
					}
				}

				return null;
			});
		}


		public Task CreateBranchAsync(string workingFolder, BranchName branchName, string commitId)
		{
			Log.Debug($"Create branch {branchName} at commit {commitId} ...");
			return repoCaller.UseLibRepoAsync(workingFolder, repository =>
			{
				Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));
				if (commit == null)
				{
					Log.Warn($"Unknown commit id {commitId}");
					return;
				}

				Branch branch = repository.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));

				if (branch != null)
				{
					Log.Warn($"Branch already exists {branchName}");
					return;
				}

				branch = repository.Branches.Add(branchName, commit);

				repository.Checkout(branch);
			});
		}



		public Task SwitchToBranchAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Switch to branch {branchName} ...");
			return repoCaller.UseLibRepoAsync(workingFolder, repository =>
			{
				Branch branch = repository.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));

				if (branch != null)
				{
					repository.Checkout(branch);
				}
				else
				{
					Branch remoteBranch = repository.Branches.FirstOrDefault(b => b.FriendlyName == "origin/" + branchName);
					if (remoteBranch != null)
					{
						branch = repository.Branches.Add(branchName, remoteBranch.Tip);
						repository.Branches.Update(branch, b => b.TrackedBranch = remoteBranch.CanonicalName);

						repository.Checkout(branch);
					}
				}
			});
		}


		public Task<R<BranchName>> SwitchToCommitAsync(
			string workingFolder, string commitId, BranchName branchName)
		{
			Log.Debug($"Switch to commit {commitId} with branch name '{branchName}' ...");
			return repoCaller.UseLibRepoAsync(workingFolder, repository =>
			{
				Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));
				if (commit == null)
				{
					Log.Warn($"Unknown commit id {commitId}");
					return null;
				}

				if (branchName != null)
				{
					// Trying to get an existing switch branch) at that commit
					Branch branch = repository.Branches
						.FirstOrDefault(b =>
							!b.IsRemote
							&& branchName.IsEqual(b.FriendlyName)
							&& b.Tip.Sha == commitId);

					if (branch != null)
					{
						repository.Checkout(branch);
						return branchName;
					}
				}

				// No branch with that name so lets check out commit (detached head)
				repository.Checkout(commit);

				return null;
			});
		}


		public Task MergeCurrentBranchFastForwardOnlyAsync(string workingFolder)
		{
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.MergeCurrentBranchFastForwardOnly());
		}


		public Task MergeCurrentBranchAsync(string workingFolder)
		{
			return repoCaller.UseRepoAsync(workingFolder, repo =>
			{
				// First try to update using fast forward merge only
				R result = repo.MergeCurrentBranchFastForwardOnly();

				if (result.Error.Is<NonFastForwardException>())
				{
					// Failed with fast forward merge, trying no fast forward.
					repo.MergeCurrentBranchNoFastForward();
				}
			});
		}


		public Task<R> PublishBranchAsync(
			string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Publish branch {branchName} ...");
			return repoCaller.UseLibRepoAsync(workingFolder, repository =>
			{
				Branch localBranch = repository.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));
				if (localBranch == null)
				{
					Log.Warn($"Local branch does not exists {branchName}");
					return;
				}

				PushOptions pushOptions = GetPushOptions(credentialHandler);

				// Check if corresponding remote branch exists
				Branch remoteBranch = repository.Branches
					.FirstOrDefault(b => b.FriendlyName == "origin/" + branchName);

				if (remoteBranch != null)
				{
					// Remote branch exists, so connect local and remote branch
					localBranch = repository.Branches.Add(branchName, remoteBranch.Tip);
					repository.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
				}
				else
				{
					// Remote branch does not yet exists
					Remote remote = repository.Network.Remotes["origin"];

					repository.Branches.Update(
						localBranch,
						b => b.Remote = remote.Name,
						b => b.UpstreamBranch = localBranch.CanonicalName);
				}

				repository.Network.Push(localBranch, pushOptions);
			});
		}


		public Task<R> DeleteBranchAsync(string workingFolder, BranchName branchName, bool isRemote, ICredentialHandler credentialHandler)
		{
			if (isRemote)
			{
				return DeleteRemoteBranchAsync(workingFolder, branchName, credentialHandler);
			}
			else
			{
				return DeleteLocalBranchAsync(workingFolder, branchName);
			}
		}


		private Task<R> DeleteLocalBranchAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Delete local branch {branchName}  ...");

			return repoCaller.UseRepoAsync(workingFolder, repo =>
			{
				repo.Branches.Remove(branchName, false);
			});
		}


		private Task<R> DeleteRemoteBranchAsync(
			string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Delete remote branch {branchName} ...");
			return repoCaller.UseRepoAsync(workingFolder, PushTimeout, repo =>

			{
				repo.Branches.Remove(branchName, true);

				PushOptions pushOptions = GetPushOptions(credentialHandler);

				Remote remote = repo.Network.Remotes["origin"];

				// Using a refspec, like you would use with git push...
				repo.Network.Push(remote, pushRefSpec: $":refs/heads/{branchName}", pushOptions: pushOptions);

				credentialHandler.SetConfirm(true);
			});
		}


		public R<GitDivergence> CheckAheadBehind(string workingFolder, string localTip, string remoteTip)
		{
			return repoCaller.UseRepo(workingFolder,
				repo =>
				{
					Commit local = repo.Lookup<Commit>(new ObjectId(localTip));
					Commit remote = repo.Lookup<Commit>(new ObjectId(remoteTip));

					if (local != null && remote != null)
					{
						HistoryDivergence div = repo.ObjectDatabase.CalculateHistoryDivergence(local, remote);

						return new GitDivergence(
							div.One.Sha,
							div.Another.Sha,
							div.CommonAncestor.Sha,
							div.AheadBy ?? 0,
							div.BehindBy ?? 0);
					}
					else
					{
						return new GitDivergence(
							localTip,
							remoteTip,
							localTip,
							0,
							0);
					}
				});
		}


		private static PushOptions GetPushOptions(ICredentialHandler credentialHandler)
		{
			PushOptions pushOptions = new PushOptions();
			pushOptions.CredentialsProvider = (url, usernameFromUrl, types) =>
			{
				NetworkCredential credential = credentialHandler.GetCredential(url, usernameFromUrl);

				if (credential == null)
				{
					throw new GitRepository.NoCredentialException();
				}

				return new UsernamePasswordCredentials
				{
					Username = credential?.UserName,
					Password = credential?.Password
				};
			};

			return pushOptions;
		}
	}
}