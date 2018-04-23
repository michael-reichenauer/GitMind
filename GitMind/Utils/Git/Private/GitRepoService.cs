using System;
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


		public Task<R> InitAsync(string path, CancellationToken ct) => InitAsync(path, false, ct);


		public Task<R> InitBareAsync(string path, CancellationToken ct) => InitAsync(path, true, ct);


		public async Task<R> CloneAsync(
			string uri, string path, Action<string> progress, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunWithProgressAsync(
				$"clone --progress \"{uri}\" \"{path}\"", progress, ct);
			if (result.IsFaulted)
			{
				return Error.From($"Failed to clone repo in: {path}", result);
			}

			Log.Info($"Cloned repo {uri}, into: {path}");
			return result;
		}


		private async Task<R> InitAsync(string path, bool isBare, CancellationToken ct)
		{
			string bareText = isBare ? " --bare " : "";

			R<CmdResult2> result = await gitCmdService.RunAsync($"init {bareText} \"{path}\"", ct);
			if (result.IsFaulted)
			{
				return Error.From($"Failed to create repo in: {path}", result);

			}

			Log.Info($"Created repo at: {path}");
			return result;
		}
	}
}