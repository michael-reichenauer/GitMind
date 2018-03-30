using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitFetch : IGitFetch
	{
		private static readonly string FetchArgs = "fetch --prune --tags --progress origin";
		private static readonly string FetchRefsArgs = "fetch origin";


		private readonly IGitCmd gitCmd;


		public GitFetch(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<GitResult> FetchAsync(CancellationToken ct) =>
			await gitCmd.RunAsync(FetchArgs, ct);


		public async Task<GitResult> FetchBranchAsync(string branchName, CancellationToken ct) => 
			await FetchRefsAsync(new[] { $"{branchName}:{branchName}" }, ct);


		public async Task<GitResult> FetchRefsAsync(IEnumerable<string> refspecs, CancellationToken ct)
		{
			string refsText = string.Join(" ", refspecs);
			string args = $"{FetchRefsArgs} {refsText}";

			return await gitCmd.RunAsync(args, ct);
		}


		public async Task<GitResult> FetchPruneTagsAsync(CancellationToken ct) => 
			await gitCmd.RunAsync("fetch --prune origin \"+refs/tags/*:refs/tags/*\"", ct);
	}
}