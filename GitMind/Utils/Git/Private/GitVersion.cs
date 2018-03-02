using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitVersion : IGitVersion
	{
		private readonly IGitCmd gitCmd;


		public GitVersion(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<string> GetAsync(CancellationToken ct)
		{
			CmdResult2 result = await gitCmd.RunAsync("version", ct);

			if (result.ExitCode != 0 && !result.IsCanceled)
			{
				Log.Warn($"Failed to get version: {result}");
			}

			return result.Output;
		}
	}
}