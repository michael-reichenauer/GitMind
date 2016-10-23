using System.Threading.Tasks;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.Git
{
	internal class NetworkService : INetworkService
	{
		private readonly IGitNetworkService gitNetworkService;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;


		public NetworkService()
			: this(new GitNetworkService(), new GitCommitBranchNameService())
		{			
		}

		public NetworkService(
			IGitNetworkService gitNetworkService,
			IGitCommitBranchNameService gitCommitBranchNameService)
		{
			this.gitNetworkService = gitNetworkService;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
		}


		public Task<R> FetchAsync(string workingFolder)
		{
			return gitNetworkService.FetchAsync(workingFolder);
		}


		public Task<R> FetchBranchAsync(string workingFolder, BranchName branchName)
		{
			return gitNetworkService.FetchBranchAsync(workingFolder, branchName);
		}


		public Task<R> PushCurrentBranchAsync(string workingFolder, ICredentialHandler credentialHandler)
		{
			return gitNetworkService.PushCurrentBranchAsync(workingFolder, credentialHandler);
		}


		public Task<R> PushBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			return gitNetworkService.PushBranchAsync(workingFolder, branchName, credentialHandler);
		}


		public Task PushNotesAsync(string workingFolder, string rootId, ICredentialHandler credentialHandler)
		{
			return gitCommitBranchNameService.PushNotesAsync(workingFolder, rootId, credentialHandler);
		}


		public Task<R> FetchAllNotesAsync(string workingFolder)
		{
			return gitCommitBranchNameService.FetchAllNotesAsync(workingFolder);
		}
	}
}