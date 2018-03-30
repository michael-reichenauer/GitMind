using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitInfo : IGitInfo
	{
		private readonly IGitCmd gitCmd;


		public GitInfo(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<string> GetWorkingFolderRootAsync(CancellationToken ct)
		{
			GitResult result = await gitCmd.RunAsync("rev-parse --show-toplevel", ct);

			if (result.ExitCode != 0 && !result.IsCanceled)
			{
				Log.Warn($"Failed to get version: {result}");
			}

			return result.Output.Trim();
		}


		public async Task<string> GetGitPathAsync(CancellationToken ct)
		{
			GitResult result = await gitCmd.RunAsync("--exec-path", ct);

			if (result.ExitCode != 0 && !result.IsCanceled)
			{
				Log.Warn($"Failed to get version: {result}");
			}

			return result.Output.Trim();
		}


		public async Task<string> GetVersionAsync(CancellationToken ct)
		{
			GitResult result = await gitCmd.RunAsync("version", ct);

			if (result.ExitCode != 0 && !result.IsCanceled)
			{
				Log.Warn($"Failed to get version: {result}");
				return "";
			}

			if (result.Output.StartsWith("git version "))
			{
				return result.Output.Substring(12).Trim();
			}

			return "";
		}
	}
}