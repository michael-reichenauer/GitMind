﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


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


		public async Task<R> FetchAsync(CancellationToken ct)
		{
			Log.Debug("Fetching ...");
			CmdResult2 result = await gitCmdService.RunCmdAsync(FetchArgs, ct);

			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return Error.From("Failed to fetch", result.AsError());
			}

			return R.Ok;
		}


		public async Task<R> FetchBranchAsync(string branchName, CancellationToken ct) =>
			await FetchRefsAsync(new[] { $"{branchName}:{branchName}" }, ct);


		public async Task<R> FetchRefsAsync(IEnumerable<string> refspecs, CancellationToken ct)
		{
			string refsText = string.Join(" ", refspecs);
			string args = $"{FetchRefsArgs} {refsText}";
			Log.Debug($"Fetching {refsText} ...");
			CmdResult2 result = await gitCmdService.RunCmdAsync(args, ct);

			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return Error.From("Failed to fetch", result.AsError());
			}

			return R.Ok;
		}


		public async Task<R> FetchPruneTagsAsync(CancellationToken ct)
		{
			Log.Debug("Fetching tags ...");
			CmdResult2 result = await gitCmdService.RunCmdAsync("fetch --prune origin \"+refs/tags/*:refs/tags/*\"", ct);

			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return Error.From("Failed to fetch", result.AsError());
			}

			return R.Ok;
		}


		private bool IsNoRemote(CmdResult2 result) =>
			result.Error.StartsWith("fatal: 'origin' does not appear to be a git repository");
	}
}