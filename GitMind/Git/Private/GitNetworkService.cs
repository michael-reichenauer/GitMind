using System;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitNetworkService : IGitNetworkService
	{
		private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan PushTimeout = TimeSpan.FromSeconds(30);

		private readonly IRepoCaller repoCaller;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;

		public GitNetworkService()
			: this(new RepoCaller(), new GitCommitBranchNameService())
		{			
		}

		public GitNetworkService(
			IRepoCaller repoCaller,
			IGitCommitBranchNameService gitCommitBranchNameService)
		{
			this.repoCaller = repoCaller;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
		}


		public async Task FetchAsync(string workingFolder)
		{
			await repoCaller.UseRepoAsync(workingFolder, FetchTimeout, repo => repo.Fetch());
		}


		public Task FetchBranchAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Fetch branch {branchName}...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.FetchBranch(branchName));
		}


		public Task FetchAllNotesAsync(string workingFolder)
		{
			return gitCommitBranchNameService.FetchAllNotesAsync(workingFolder);
		}


		public Task PushCurrentBranchAsync(
			string workingFolder, ICredentialHandler credentialHandler)
		{
			return repoCaller.UseRepoAsync(workingFolder, PushTimeout,
				repo => repo.PushCurrentBranch(credentialHandler));
		}


		public Task PushNotesAsync(
			string workingFolder, string rootId, ICredentialHandler credentialHandler)
		{
			return gitCommitBranchNameService.PushNotesAsync(workingFolder, rootId, credentialHandler);
		}


		public Task PushBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Push branch {branchName} ...");
			return repoCaller.UseRepoAsync(workingFolder, PushTimeout,
				repo => repo.PushBranch(branchName, credentialHandler));
		}
	}
}