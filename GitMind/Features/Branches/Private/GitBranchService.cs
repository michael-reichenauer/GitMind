using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;


namespace GitMind.Features.Branches.Private
{
	internal class GitBranchService : IGitBranchService
	{
		private readonly IGitBranchService2 gitBranchService2;
		private readonly IGitMergeService2 gitMergeService2;
		private readonly IGitCommitService2 gitCommitService2;
		private readonly IGitCheckoutService gitCheckoutService;


		public GitBranchService(
			IGitBranchService2 gitBranchService2,
			IGitMergeService2 gitMergeService2,
			IGitCommitService2 gitCommitService2,
			IGitCheckoutService gitCheckoutService)
		{
			this.gitBranchService2 = gitBranchService2;
			this.gitMergeService2 = gitMergeService2;
			this.gitCommitService2 = gitCommitService2;
			this.gitCheckoutService = gitCheckoutService;
		}


		public async Task<R> MergeAsync(BranchName branchName)
		{
			Log.Debug($"Merge branch {branchName} into current branch ...");

			R<IReadOnlyList<GitBranch2>> branches = await gitBranchService2.GetBranchesAsync(CancellationToken.None);
			if (branches.IsFaulted)
			{
				return Error.From("Failed to merge", branches);
			}

			// Trying to get both local and remote branch
			branches.Value.TryGet(branchName, out GitBranch2 localbranch);
			branches.Value.TryGet($"origin/{branchName}", out GitBranch2 remoteBranch);

			GitBranch2 branch = localbranch ?? remoteBranch;
			if (localbranch != null && remoteBranch != null)
			{
				// Both local and remote tip exists, use the branch with the most resent tip
				R<GitCommit> localTipCommit = await gitCommitService2.GetCommitAsync(localbranch.TipSha.Sha, CancellationToken.None);
				R<GitCommit> remoteTipCommit = await gitCommitService2.GetCommitAsync(remoteBranch.TipSha.Sha, CancellationToken.None);

				if (localTipCommit.IsFaulted || remoteTipCommit.IsFaulted)
				{
					return Error.From("Failed to merge", remoteTipCommit);
				}

				if (remoteTipCommit.Value.CommitDate > localTipCommit.Value.CommitDate)
				{
					branch = remoteBranch;
				}
			}

			if (branch == null)
			{
				return Error.From($"Failed to Merge, not valid branch {branchName}");
			}


			return await gitMergeService2.MergeAsync(branch.Name, CancellationToken.None);

			//return repoCaller.UseLibRepoAsync(repository =>
			//{
			//	Signature signature = GetSignature(repository);

			//	Branch localbranch = TryGetBranch(repository, branchName);
			//	Branch remoteBranch = TryGetBranch(repository, "origin/" + branchName);

			//	Branch branch = localbranch ?? remoteBranch;
			//	if (localbranch != null && remoteBranch != null)
			//	{
			//		// Both local and remote tip exists, use the branch with the most resent tip
			//		if (remoteBranch.Tip.Committer.When.LocalDateTime
			//		> localbranch.Tip.Committer.When.LocalDateTime)
			//		{
			//			branch = remoteBranch;
			//		}
			//	}

			//	if (branch != null)
			//	{
			//		repository.Merge(branch, signature, MergeNoFFNoCommit);

			//		//RepositoryStatus repositoryStatus = repository.RetrieveStatus(new StatusOptions());

			//		//if (!repositoryStatus.IsDirty)
			//		//{
			//		//	// Empty merge with no changes, lets reset merge since there is nothing to merge
			//		//	repository.Reset(ResetMode.Hard);
			//		//}
			//	}
			//});
		}

		public async Task<R> MergeAsync(CommitSha commitSha)
		{
			return await gitMergeService2.MergeAsync(commitSha.Sha, CancellationToken.None);

			//	return repoCaller.UseLibRepoAsync(repository =>
			//{
			//	Signature signature = GetSignature(repository);
			//	Commit commit = repository.Lookup<Commit>(new ObjectId(commitSha.Sha));

			//	repository.Merge(commit, signature, MergeNoFFNoCommit);
			//});
		}


		public Task<R> CreateBranchAsync(BranchName branchName, CommitSha commitSha)
		{
			return gitBranchService2.BranchFromCommitAsync(branchName, commitSha.Sha, true, CancellationToken.None);

			//return repoCaller.UseLibRepoAsync(repository =>
			//{
			//	Commit commit = repository.Lookup<Commit>(new ObjectId(commitSha.Sha));
			//	if (commit == null)
			//	{
			//		Log.Error($"Unknown commit id {commitSha}");
			//		return;
			//	}

			//	Branch branch = repository.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));

			//	if (branch != null)
			//	{
			//		Log.Warn($"Branch already exists {branchName}");
			//		return;
			//	}

			//	branch = repository.Branches.Add(branchName, commit);

			//	repository.Checkout(branch);
			//});
		}



		public async Task<R> SwitchToBranchAsync(BranchName branchName, CommitSha tipSha)
		{
			Log.Debug($"Switch to branch {branchName} ...");

			R<bool> checkoutResult = await gitCheckoutService.TryCheckoutAsync(branchName, CancellationToken.None);
			if (checkoutResult.IsFaulted)
			{
				return Error.From("Failed to switch branch", checkoutResult);
			}

			if (!checkoutResult.Value)
			{
				Log.Debug($"Branch {branchName} does not exist, lets try to create it");

				var branchResult = await gitBranchService2.BranchFromCommitAsync(branchName, tipSha.Sha, true, CancellationToken.None);

				if (branchResult.IsFaulted)
				{
					return Error.From("Failed to switch branch", branchResult);
				}
			}

			return R.Ok;
			//return repoCaller.UseLibRepoAsync(repository =>
			//{
			//	Branch branch = repository.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));

			//	if (branch != null)
			//	{
			//		repository.Checkout(branch);
			//	}
			//	else
			//	{
			//		Branch remoteBranch = repository.Branches.FirstOrDefault(b => b.FriendlyName == "origin/" + branchName);
			//		if (remoteBranch != null)
			//		{
			//			branch = repository.Branches.Add(branchName, remoteBranch.Tip);
			//			repository.Branches.Update(branch, b => b.TrackedBranch = remoteBranch.CanonicalName);

			//			repository.Checkout(branch);
			//		}
			//		else
			//		{
			//			// No existing branch with that name. Try create a local branch
			//			Commit commit = repository.Lookup<Commit>(new ObjectId(tipSha.Sha));
			//			if (commit != null)
			//			{
			//				branch = repository.Branches.Add(branchName, commit);
			//				repository.Checkout(branch);
			//			}
			//		}
			//	}
			//});
		}


		public async Task<R<BranchName>> SwitchToCommitAsync(CommitSha commitSha, BranchName branchName)
		{
			Log.Debug($"Switch to commit {commitSha} with branch name '{branchName}' ...");

			R<IReadOnlyList<GitBranch2>> branches = await gitBranchService2.GetBranchesAsync(CancellationToken.None);
			if (branches.IsFaulted)
			{
				return Error.From("Failed to switch to commit", branches);
			}

			if (branches.Value.TryGet(branchName, out GitBranch2 branch))
			{
				if (branch.TipSha == commitSha)
				{
					R checkoutResult = await gitCheckoutService.CheckoutAsync(branchName, CancellationToken.None);
					if (checkoutResult.IsFaulted)
					{
						return Error.From("Failed to switch to commit", checkoutResult);
					}

					return branchName;
				}
			}

			R checkoutCommitResult = await gitCheckoutService.CheckoutAsync(commitSha.Sha, CancellationToken.None);
			if (checkoutCommitResult.IsFaulted)
			{
				return Error.From("Failed to switch to commit", checkoutCommitResult);
			}

			return null;


			//return repoCaller.UseLibRepoAsync(repository =>
			//{
			//	Commit commit = repository.Lookup<Commit>(new ObjectId(commitSha.Sha));
			//	if (commit == null)
			//	{
			//		Log.Error($"Unknown commit id {commitSha}");
			//		return null;
			//	}

			//	if (branchName != null)
			//	{
			//		// Trying to get an existing switch branch) at that commit
			//		Branch branch = repository.Branches
			//		.FirstOrDefault(b =>
			//			!b.IsRemote
			//			&& branchName.IsEqual(b.FriendlyName)
			//			&& b.Tip.Sha == commitSha.Sha);

			//		if (branch != null)
			//		{
			//			repository.Checkout(branch);
			//			return branchName;
			//		}
			//	}

			//	// No branch with that name so lets check out commit (detached head)
			//	repository.Checkout(commit);

			//	return null;
			//});
		}


		public Task<R> MergeCurrentBranchFastForwardOnlyAsync()
		{
			return gitMergeService2.MergeAsync(null, CancellationToken.None);
			//return repoCaller.UseRepoAsync(repo =>
			//{
			//	Signature committer = repo.Config.BuildSignature(DateTimeOffset.Now);
			//	repo.MergeFetchedRefs(committer, MergeFastForwardOnly);
			//});
		}


		public async Task<R> MergeCurrentBranchAsync()
		{
			R<bool> ffResult = await gitMergeService2.TryMergeFastForwardAsync(null, CancellationToken.None);
			if (ffResult.IsFaulted)
			{
				return Error.From("Failed to merge current branch", ffResult);
			}

			if (!ffResult.Value)
			{
				R result = await gitMergeService2.MergeAsync(null, CancellationToken.None);
				if (result.IsFaulted)
				{
					return Error.From("Failed to merge current branch", ffResult);
				}
			}

			return R.Ok;
			//return repoCaller.UseRepoAsync(repo =>
			//{
			//	Signature committer = repo.Config.BuildSignature(DateTimeOffset.Now);

			//	try
			//	{
			//		repo.MergeFetchedRefs(committer, MergeFastForwardOnly);
			//	}
			//	catch (NonFastForwardException)
			//	{
			//		// Failed with fast forward merge, trying no fast forward.
			//		repo.MergeFetchedRefs(committer, MergeNoFastForwardAndCommit);
			//	}
			//});
		}


		public Task<R> DeleteLocalBranchAsync(BranchName branchName)
		{
			return gitBranchService2.DeleteLocalBranchAsync(branchName, CancellationToken.None);
			//Log.Debug($"Delete local branch {branchName}  ...");

			//return repoCaller.UseRepoAsync(repo =>
			//{
			//	repo.Branches.Remove(branchName, false);
			//});
		}


		//public R<GitDivergence> GetCommonAncestor(CommitSha localTip, CommitSha remoteTip)
		//{
		//	return repoCaller.UseRepo(
		//		repo =>
		//		{
		//			Commit local = repo.Lookup<Commit>(new ObjectId(localTip.Sha));
		//			Commit remote = repo.Lookup<Commit>(new ObjectId(remoteTip.Sha));

		//			if (local != null && remote != null)
		//			{
		//				HistoryDivergence div = repo.ObjectDatabase.CalculateHistoryDivergence(local, remote);

		//				return new GitDivergence(
		//					new CommitSha(div.One.Sha),
		//					new CommitSha(div.Another.Sha),
		//					new CommitSha(div.CommonAncestor.Sha),
		//					div.AheadBy ?? 0,
		//					div.BehindBy ?? 0);
		//			}
		//			else
		//			{
		//				return new GitDivergence(
		//					localTip,
		//					remoteTip,
		//					localTip,
		//					0,
		//					0);
		//			}
		//		});
		//}


		//private static Branch TryGetBranch(Repository repository, BranchName branchName)
		//{
		//	return repository.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));
		//}


		//private static Signature GetSignature(Repository repository)
		//{
		//	return repository.Config.BuildSignature(DateTimeOffset.Now);
		//}
	}
}