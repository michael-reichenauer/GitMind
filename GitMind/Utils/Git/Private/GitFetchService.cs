using System.Collections.Generic;
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

			void Progress(string text)
			{
				Log.Debug($"Progress: {text}");
			}

			CmdResult2 result = await gitCmdService.RunCmdWitProgressAsync(FetchArgs, Progress, ct);

			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return R.Error("Failed to fetch", result.AsException());
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

			void Progress(string text)
			{
				Log.Debug($"Progress: {text}");
			}

			CmdResult2 result = await gitCmdService.RunCmdWitProgressAsync(args, Progress, ct);

			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return R.Error("Failed to fetch", result.AsException());
			}

			return R.Ok;
		}


		public async Task<R> FetchPruneTagsAsync(CancellationToken ct)
		{
			Log.Debug("Fetching tags ...");
			string args = "fetch --prune origin \"+refs/tags/*:refs/tags/*\"";
			void Progress(string text)
			{
				Log.Debug($"Progress: {text}");
			}
			CmdResult2 result = await gitCmdService.RunCmdWitProgressAsync(args, Progress, ct);

			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return R.Error("Failed to fetch", result.AsException());
			}

			return R.Ok;
		}


		private bool IsNoRemote(CmdResult2 result) =>
			result.Error.StartsWith("fatal: 'origin' does not appear to be a git repository");
	}
}