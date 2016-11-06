using System.Threading.Tasks;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.Git
{
	internal class NetworkService : INetworkService
	{
		private readonly IGitNetworkService gitNetworkService;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;


		public NetworkService(
			IGitNetworkService gitNetworkService,
			IGitCommitBranchNameService gitCommitBranchNameService)
		{
			this.gitNetworkService = gitNetworkService;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
		}


		public Task<R> FetchAsync()
		{
			return gitNetworkService.FetchAsync();
		}


		public Task<R> FetchBranchAsync(BranchName branchName)
		{
			return gitNetworkService.FetchBranchAsync(branchName);
		}


		public Task<R> PushCurrentBranchAsync()
		{
			return gitNetworkService.PushCurrentBranchAsync();
		}


		public Task<R> PushBranchAsync(BranchName branchName)
		{
			return gitNetworkService.PushBranchAsync(branchName);
		}


		public Task PushNotesAsync(string rootId)
		{
			return gitCommitBranchNameService.PushNotesAsync(rootId);
		}


		public Task<R> FetchAllNotesAsync()
		{
			return gitCommitBranchNameService.FetchAllNotesAsync();
		}
	}
}