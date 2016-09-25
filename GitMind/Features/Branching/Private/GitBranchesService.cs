using System;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Features.Branching.Private
{
	internal class GitBranchesService : IGitBranchesService
	{
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
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.MergeBranchNoFastForward(branchName));
		}


		public Task CreateBranchAsync(string workingFolder, BranchName branchName, string commitId)
		{
			Log.Debug($"Create branch {branchName} at commit {commitId} ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.CreateBranch(branchName, commitId));
		}


		public Task SwitchToBranchAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Switch to branch {branchName} ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.Checkout(branchName));
		}


		public Task<R<BranchName>> SwitchToCommitAsync(
			string workingFolder, string commitId, BranchName branchName)
		{
			Log.Debug($"Switch to commit {commitId} with branch name '{branchName}' ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.SwitchToCommit(commitId, branchName));
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

		public Task<R> PublishBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Publish branch {branchName} ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.PublishBranch(branchName, credentialHandler));
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


		public R<GitDivergence> CheckAheadBehind(string workingFolder, string localTip, string remoteTip)
		{
			return repoCaller.UseRepo(workingFolder, repo => repo.CheckAheadBehind(localTip, remoteTip));
		}


		private Task<R> DeleteLocalBranchAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Delete local branch {branchName}  ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.DeleteLocalBranch(branchName));
		}


		private Task<R> DeleteRemoteBranchAsync(
			string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Delete remote branch {branchName} ...");
			return repoCaller.UseRepoAsync(workingFolder, PushTimeout, repo =>
				repo.DeleteRemoteBranch(branchName, credentialHandler));
		}

	}
}