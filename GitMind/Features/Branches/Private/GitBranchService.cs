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
				return R.Error("Failed to merge", branches.Exception);
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
					return R.Error("Failed to merge", remoteTipCommit.Exception);
				}

				if (remoteTipCommit.Value.CommitDate > localTipCommit.Value.CommitDate)
				{
					branch = remoteBranch;
				}
			}

			if (branch == null)
			{
				return R.Error($"Failed to Merge, not valid branch {branchName}");
			}


			return await gitMergeService2.MergeAsync(branch.Name, CancellationToken.None);
		}

		public async Task<R> MergeAsync(CommitSha commitSha) => await gitMergeService2.MergeAsync(commitSha.Sha, CancellationToken.None);


		public Task<R> CreateBranchAsync(BranchName branchName, CommitSha commitSha) => gitBranchService2.BranchFromCommitAsync(branchName, commitSha.Sha, true, CancellationToken.None);


		public async Task<R> SwitchToBranchAsync(BranchName branchName, CommitSha tipSha)
		{
			Log.Debug($"Switch to branch {branchName} ...");

			R<bool> checkoutResult = await gitCheckoutService.TryCheckoutAsync(branchName, CancellationToken.None);
			if (checkoutResult.IsFaulted)
			{
				return R.Error("Failed to switch branch", checkoutResult.Exception);
			}

			if (!checkoutResult.Value)
			{
				Log.Debug($"Branch {branchName} does not exist, lets try to create it");

				var branchResult = await gitBranchService2.BranchFromCommitAsync(branchName, tipSha.Sha, true, CancellationToken.None);

				if (branchResult.IsFaulted)
				{
					return R.Error("Failed to switch branch", branchResult.Exception);
				}
			}

			return R.Ok;
		}


		public async Task<R<BranchName>> SwitchToCommitAsync(CommitSha commitSha, BranchName branchName)
		{
			Log.Debug($"Switch to commit {commitSha} with branch name '{branchName}' ...");

			R<IReadOnlyList<GitBranch2>> branches = await gitBranchService2.GetBranchesAsync(CancellationToken.None);
			if (branches.IsFaulted)
			{
				return R.Error("Failed to switch to commit", branches.Exception);
			}

			if (branches.Value.TryGet(branchName, out GitBranch2 branch))
			{
				if (branch.TipSha == commitSha)
				{
					R checkoutResult = await gitCheckoutService.CheckoutAsync(branchName, CancellationToken.None);
					if (checkoutResult.IsFaulted)
					{
						return R.Error("Failed to switch to commit", checkoutResult.Exception);
					}

					return branchName;
				}
			}

			R checkoutCommitResult = await gitCheckoutService.CheckoutAsync(commitSha.Sha, CancellationToken.None);
			if (checkoutCommitResult.IsFaulted)
			{
				return R.Error("Failed to switch to commit", checkoutCommitResult.Exception);
			}

			return null;
		}


		public Task<R> MergeCurrentBranchFastForwardOnlyAsync() => gitMergeService2.MergeAsync(null, CancellationToken.None);


		public async Task<R> MergeCurrentBranchAsync()
		{
			R<bool> ffResult = await gitMergeService2.TryMergeFastForwardAsync(null, CancellationToken.None);
			if (ffResult.IsFaulted)
			{
				return R.Error("Failed to merge current branch", ffResult.Exception);
			}

			if (!ffResult.Value)
			{
				R result = await gitMergeService2.MergeAsync(null, CancellationToken.None);
				if (result.IsFaulted)
				{
					return R.Error("Failed to merge current branch", ffResult.Exception);
				}
			}

			return R.Ok;
		}
	}
}