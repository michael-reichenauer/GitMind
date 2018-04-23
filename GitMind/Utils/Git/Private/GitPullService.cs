using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


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
			R<CmdResult2> result = await gitCmdService.RunAsync("pull --ff --no-rebase --progress", ct);

			if (result.IsFaulted)
			{
				return Error.From("Failed to pull", result);
			}

			return result;
		}
	}
}