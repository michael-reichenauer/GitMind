using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitPush : IGitPush
	{
		private readonly IGitCmd gitCmd;

		private static readonly string PushArgs = "push --porcelain origin";
		private static readonly string PushBranchArgs = "push --porcelain -u origin";


		public GitPush(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<GitResult> PushAsync(CancellationToken ct) => 
			await gitCmd.RunAsync(PushArgs, ct);


		public async Task<GitResult> PushBranchAsync(string branchName, CancellationToken ct)
		{
			string[] refspecs = { $"refs/heads/{branchName}:refs/heads/{branchName}" };

			string refsText = string.Join(" ", refspecs);
			string pushArgs = $"{PushBranchArgs} {refsText}";

			return await gitCmd.RunAsync(pushArgs, ct);
		}


		public async Task<GitResult> PushDeleteRemoteBranchAsync(string branchName, CancellationToken ct)
		{
			string pushArgs = $"{PushArgs} --delete {branchName}";

			return await gitCmd.RunAsync(pushArgs, ct);
		}

		public async Task<GitResult> PushTagAsync(string tagName, CancellationToken ct)
		{
			string pushArgs = $"{PushArgs} {tagName}";

			return await gitCmd.RunAsync(pushArgs, ct);
		}


		public async Task<GitResult> PushRefsAsync(IEnumerable<string> refspecs, CancellationToken ct)
		{
			string refsText = string.Join(" ", refspecs);
			string pushArgs = $"{PushArgs} {refsText}";

			return await gitCmd.RunAsync(pushArgs, ct);
		}
	}
}