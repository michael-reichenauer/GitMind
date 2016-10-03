using System.Threading.Tasks;
using GitMind.Git.Private;


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


		public Task FetchAsync(string workingFolder)
		{
			return gitNetworkService.FetchAsync(workingFolder);
		}


		public Task FetchBranchAsync(string workingFolder, BranchName branchName)
		{
			return gitNetworkService.FetchBranchAsync(workingFolder, branchName);
		}


		public Task PushCurrentBranchAsync(string workingFolder, ICredentialHandler credentialHandler)
		{
			return gitNetworkService.PushCurrentBranchAsync(workingFolder, credentialHandler);
		}


		public Task PushBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			return gitNetworkService.PushBranchAsync(workingFolder, branchName, credentialHandler);
		}


		public Task PushNotesAsync(string workingFolder, string rootId, ICredentialHandler credentialHandler)
		{
			return gitCommitBranchNameService.PushNotesAsync(workingFolder, rootId, credentialHandler);
		}


		public Task FetchAllNotesAsync(string workingFolder)
		{
			return gitCommitBranchNameService.FetchAllNotesAsync(workingFolder);
		}
	}
}