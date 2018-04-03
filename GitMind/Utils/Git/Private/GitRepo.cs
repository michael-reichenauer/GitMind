using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitRepo : IGitRepo
	{
		private readonly IGitCmd gitCmd;


		public GitRepo(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<R> InitAsync(string path, CancellationToken ct) =>
			await gitCmd.RunAsync($"init \"{path}\"", ct);
	}
}