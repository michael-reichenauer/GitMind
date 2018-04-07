﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitFetchService : IGitFetchService
	{
		private static readonly string FetchArgs = "fetch --prune --tags --progress origin";
		private static readonly string FetchRefsArgs = "fetch origin";


		private readonly IGitCmdService gitCmdService;


		public GitFetchService(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R> FetchAsync(CancellationToken ct) => await gitCmdService.RunAsync(FetchArgs, ct);


		public async Task<R> FetchBranchAsync(string branchName, CancellationToken ct) =>
			await FetchRefsAsync(new[] { $"{branchName}:{branchName}" }, ct);


		public async Task<R> FetchRefsAsync(IEnumerable<string> refspecs, CancellationToken ct)
		{
			string refsText = string.Join(" ", refspecs);
			string args = $"{FetchRefsArgs} {refsText}";

			return await gitCmdService.RunAsync(args, ct);
		}


		public async Task<R> FetchPruneTagsAsync(CancellationToken ct) =>
			await gitCmdService.RunAsync("fetch --prune origin \"+refs/tags/*:refs/tags/*\"", ct);
	}
}