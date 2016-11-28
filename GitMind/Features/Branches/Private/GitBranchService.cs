using System;
using System.Linq;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Features.Branches.Private
{
	internal class GitBranchService : IGitBranchService
	{
		private static readonly MergeOptions MergeNoFFNoCommit = new MergeOptions
		{ FastForwardStrategy = FastForwardStrategy.NoFastForward, CommitOnSuccess = false };

		private static readonly MergeOptions MergeFastForwardOnly =
			new MergeOptions { FastForwardStrategy = FastForwardStrategy.FastForwardOnly };

		private static readonly MergeOptions MergeNoFastForwardAndCommit =
			new MergeOptions { FastForwardStrategy = FastForwardStrategy.NoFastForward, CommitOnSuccess = true };


		private readonly WorkingFolder workingFolder;
		private readonly IRepoCaller repoCaller;


		public GitBranchService(
			WorkingFolder workingFolder,
			IRepoCaller repoCaller)
		{
			this.workingFolder = workingFolder;
			this.repoCaller = repoCaller;
		}


		public Task<R> MergeAsync(BranchName branchName)
		{
			Log.Debug($"Merge branch {branchName} into current branch ...");

			return repoCaller.UseLibRepoAsync(repository =>
			{
				Signature signature = GetSignature(repository);

				Branch localbranch = TryGetBranch(repository, branchName);
				Branch remoteBranch = TryGetBranch(repository, "origin/" + branchName);

				Branch branch = localbranch ?? remoteBranch;
				if (localbranch != null && remoteBranch != null)
				{
					// Both local and remote tip exists, use the branch with the most resent tip
					if (remoteBranch.Tip.Committer.When.LocalDateTime
					> localbranch.Tip.Committer.When.LocalDateTime)
					{
						branch = remoteBranch;
					}
				}

				if (branch != null)
				{
					repository.Merge(branch, signature, MergeNoFFNoCommit);

					//RepositoryStatus repositoryStatus = repository.RetrieveStatus(new StatusOptions());

					//if (!repositoryStatus.IsDirty)
					//{
					//	// Empty merge with no changes, lets reset merge since there is nothing to merge
					//	repository.Reset(ResetMode.Hard);
					//}
				}
			});
		}


		public Task<R> CreateBranchAsync(BranchName branchName, string commitId)
		{
			Log.Debug($"Create branch {branchName} at commit {commitId} ...");

			return repoCaller.UseLibRepoAsync(repository =>
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



		public Task<R> SwitchToBranchAsync(BranchName branchName)
		{
			Log.Debug($"Switch to branch {branchName} ...");
			return repoCaller.UseLibRepoAsync(repository =>
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


		public Task<R<BranchName>> SwitchToCommitAsync(string commitId, BranchName branchName)
		{
			Log.Debug($"Switch to commit {commitId} with branch name '{branchName}' ...");
			return repoCaller.UseLibRepoAsync(repository =>
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


		public Task<R> MergeCurrentBranchFastForwardOnlyAsync()
		{
			return repoCaller.UseRepoAsync(repo =>
			{
				Signature committer = repo.Config.BuildSignature(DateTimeOffset.Now);
				repo.MergeFetchedRefs(committer, MergeFastForwardOnly);
			});
		}


		public Task<R> MergeCurrentBranchAsync()
		{
			return repoCaller.UseRepoAsync(repo =>
			{
				Signature committer = repo.Config.BuildSignature(DateTimeOffset.Now);

				try
				{
					repo.MergeFetchedRefs(committer, MergeFastForwardOnly);
				}
				catch (NonFastForwardException)
				{
					// Failed with fast forward merge, trying no fast forward.
					repo.MergeFetchedRefs(committer, MergeNoFastForwardAndCommit);
				}
			});
		}


		public Task<R> DeleteLocalBranchAsync(BranchName branchName)
		{
			Log.Debug($"Delete local branch {branchName}  ...");

			return repoCaller.UseRepoAsync(repo =>
			{
				repo.Branches.Remove(branchName, false);
			});
		}


		public R<GitDivergence> CheckAheadBehind(string localTip, string remoteTip)
		{
			return repoCaller.UseRepo(
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


		private static Branch TryGetBranch(Repository repository, BranchName branchName)
		{
			return repository.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));
		}


		private static Signature GetSignature(Repository repository)
		{
			return repository.Config.BuildSignature(DateTimeOffset.Now);
		}
	}
}