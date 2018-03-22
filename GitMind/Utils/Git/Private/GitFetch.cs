using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitFetch : IGitFetch
	{
		private static readonly string FetchArgs = "fetch --prune --tags --progress";

		private readonly IGitCmd gitCmd;


		public GitFetch(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<GitResult> FetchAsync(CancellationToken ct)
		{
			return await gitCmd.RunAsync(FetchArgs, ct);
		}
	}
}