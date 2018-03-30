using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitPush : IGitPush
	{
		private readonly IGitCmd gitCmd;

		private static readonly string PushArgs = "push --porcelain origin";
		//private static readonly string PushBranchArgs = "push --porcelain -u origin";


		public GitPush(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<GitResult> PushAsync(CancellationToken ct) => 
			await gitCmd.RunAsync(PushArgs, ct);


		public async Task<GitResult> PushBranchAsync(string branchName, CancellationToken ct)
		{
			string args = $"{PushArgs} -u refs/heads/{branchName}:refs/heads/{branchName}";

			return await gitCmd.RunAsync(args, ct);
		}


		public async Task<GitResult> PushDeleteRemoteBranchAsync(string branchName, CancellationToken ct) => 
			await gitCmd.RunAsync($"{PushArgs} --delete {branchName}", ct);


		public async Task<GitResult> PushTagAsync(string tagName, CancellationToken ct) => 
			await gitCmd.RunAsync($"{PushArgs} {tagName}", ct);


		public async Task<GitResult> PushDeleteRemoteTagAsync(string tagName, CancellationToken ct) => 
			await gitCmd.RunAsync($"{PushArgs} --delete {tagName}", ct);


		public async Task<GitResult> PushRefsAsync(IEnumerable<string> refspecs, CancellationToken ct)
		{
			string refsText = string.Join(" ", refspecs);

			return await gitCmd.RunAsync($"{PushArgs} {refsText}", ct);
		}
	}
}