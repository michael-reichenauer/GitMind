using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitPush : IGitPush
	{
		private readonly IGitCmd gitCmd;

		private static readonly string PushArgs = "push --porcelain origin";


		public GitPush(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<R> PushAsync(CancellationToken ct) => await gitCmd.RunAsync(PushArgs, ct);


		public async Task<R> PushBranchAsync(string branchName, CancellationToken ct)
		{
			string args = $"{PushArgs} -u refs/heads/{branchName}:refs/heads/{branchName}";

			return await gitCmd.RunAsync(args, ct);
		}


		public async Task<R> PushDeleteRemoteBranchAsync(string branchName, CancellationToken ct) =>
			await gitCmd.RunAsync($"{PushArgs} --delete {branchName}", ct);


		public async Task<R> PushTagAsync(string tagName, CancellationToken ct) =>
			await gitCmd.RunAsync($"{PushArgs} {tagName}", ct);


		public async Task<R> PushDeleteRemoteTagAsync(string tagName, CancellationToken ct) =>
			await gitCmd.RunAsync($"{PushArgs} --delete {tagName}", ct);


		public async Task<R> PushRefsAsync(IEnumerable<string> refspecs, CancellationToken ct)
		{
			string refsText = string.Join(" ", refspecs);

			return await gitCmd.RunAsync($"{PushArgs} {refsText}", ct);
		}
	}
}