using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	class GitFetch : IGitFetch
	{
		private readonly IGitCmd gitCmd;


		public GitFetch(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task FetchAsync(CancellationToken ct)
		{
			CmdResult2 result = await gitCmd.RunAsync("fetch", ct);

			if (result.ExitCode != 0 && !result.IsCanceled)
			{
				Log.Warn($"Failed to fetch {result}");
			}
		}
	}
}