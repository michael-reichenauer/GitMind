using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitPullService : IGitPullService
	{
		private readonly IGitCmdService gitCmdService;


		public GitPullService(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R> PullAsync(CancellationToken ct)
		{
			return await gitCmdService.RunAsync("pull --ff --no-rebase --progress", ct);
		}
	}
}