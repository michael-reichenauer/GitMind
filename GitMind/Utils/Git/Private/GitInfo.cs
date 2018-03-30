using System;
using System.IO;
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


		public async Task<string> TryGetWorkingFolderRootAsync(string path, CancellationToken ct)
		{
			if (path.EndsWith(".git"))
			{
				path = Path.GetDirectoryName(path);
			}

			GitResult result = await gitCmd.RunAsync($"-C \"{path}\" rev-parse --show-toplevel", ct);

			return result.IsOk && !string.IsNullOrWhiteSpace(result.Output)
				? result.Output.Replace("/", "\\").Trim() : null;
		}


		public async Task<string> TryGetGitPathAsync(CancellationToken ct)
		{
			GitResult result = await gitCmd.RunAsync("--exec-path", ct);

			return result.IsOk ? result.Output.Trim() : null;
		}


		public async Task<string> TryGetGitVersionAsync(CancellationToken ct)
		{
			GitResult result = await gitCmd.RunAsync("version", ct);

			return result.IsOk && result.Output.StartsWithOic("git version ")
				? result.Output.Substring(12).Trim() : null;
		}
	}
}