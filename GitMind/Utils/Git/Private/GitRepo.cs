using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitRepo : IGitRepo
	{
		private readonly IGitCmd gitCmd;


		public GitRepo(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<R> InitAsync(string path, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmd.RunAsync($"init \"{path}\"", ct);
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