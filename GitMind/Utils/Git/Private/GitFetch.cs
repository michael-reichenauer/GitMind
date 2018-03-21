using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitFetch : IGitFetch
	{
		private static readonly string FetchArgs = "fetch -p -v --progress";

		private readonly IGitCmd gitCmd;


		public GitFetch(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<bool> FetchAsync(CancellationToken ct)
		{
			CmdResult2 result = await gitCmd.RunAsync(FetchArgs, ct);

			if (result.ExitCode != 0 && !result.IsCanceled)
			{
				Log.Warn($"Failed to fetch: {result}");
				return false;
			}

			return true;
		}
	}
}