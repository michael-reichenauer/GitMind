using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitRepoService : IGitRepoService
	{
		private readonly IGitCmdService gitCmdService;


		public GitRepoService(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R> InitAsync(string path, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync($"init \"{path}\"", ct);
			if (result.IsOk)
			{
				Log.Info($"Created repo at: {path}");
				return result;
			}
			else
			{
				return Error.From($"Failed to create repo in: {path}", result);
			}
		}
	}
}