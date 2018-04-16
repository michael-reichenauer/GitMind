using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitPushService : IGitPushService
	{
		private readonly IGitCmdService gitCmdService;

		private static readonly string PushArgs = "push --porcelain origin";


		public GitPushService(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R> PushAsync(CancellationToken ct) => await gitCmdService.RunAsync(PushArgs, ct);


		public async Task<R> PushBranchAsync(string branchName, CancellationToken ct)
		{
			Log.Debug($"Pushing branch {branchName} ...");
			string args = $"{PushArgs} -u refs/heads/{branchName}:refs/heads/{branchName}";

			return await gitCmdService.RunAsync(args, ct);
		}


		public async Task<R> PushDeleteRemoteBranchAsync(string branchName, CancellationToken ct)
		{
			Log.Debug($"Pushing delete branch {branchName} ...");
			return await gitCmdService.RunAsync($"{PushArgs} --delete {branchName}", ct);
		}


		public async Task<R> PushTagAsync(string tagName, CancellationToken ct)
		{
			Log.Debug($"Pushing tag {tagName} ...");

			return await gitCmdService.RunAsync($"{PushArgs} {tagName}", ct);
		}


		public async Task<R> PushDeleteRemoteTagAsync(string tagName, CancellationToken ct)
		{
			Log.Debug($"Pushing delete tag {tagName} ...");

			return await gitCmdService.RunAsync($"{PushArgs} --delete {tagName}", ct);
		}


		public async Task<R> PushRefsAsync(IEnumerable<string> refspecs, CancellationToken ct)
		{
			string refsText = string.Join(" ", refspecs);
			Log.Debug($"Pushing refs {refsText} ...");

			return await gitCmdService.RunAsync($"{PushArgs} {refsText}", ct);
		}
	}
}