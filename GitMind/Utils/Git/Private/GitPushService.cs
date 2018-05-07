using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


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


		public async Task<R> PushAsync(CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync(PushArgs, ct);

			if (result.IsFaulted)
			{
				if (IsNoRemoteBranch(result))
				{
					return R.Ok;
				}
				return R.Error("Failed to push", result.AsException());
			}

			return R.Ok;
		}


		public async Task<R> PushBranchAsync(string branchName, CancellationToken ct)
		{
			Log.Debug($"Pushing branch {branchName} ...");
			string args = $"{PushArgs} -u refs/heads/{branchName}:refs/heads/{branchName}";

			CmdResult2 result = await gitCmdService.RunCmdAsync(args, ct);
			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return R.Error("Failed to push", result.AsException());
			}

			return R.Ok;
		}


		public async Task<R> PushDeleteRemoteBranchAsync(string branchName, CancellationToken ct)
		{
			Log.Debug($"Pushing delete branch {branchName} ...");
			CmdResult2 result = await gitCmdService.RunCmdAsync($"{PushArgs} --delete {branchName}", ct);
			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return R.Error("Failed to push", result.AsException());
			}

			return R.Ok;
		}


		public async Task<R> PushTagAsync(string tagName, CancellationToken ct)
		{
			Log.Debug($"Pushing tag {tagName} ...");

			CmdResult2 result = await gitCmdService.RunCmdAsync($"{PushArgs} {tagName}", ct);

			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return R.Error("Failed to push tag", result.AsException());
			}

			return R.Ok;
		}


		public async Task<R> PushDeleteRemoteTagAsync(string tagName, CancellationToken ct)
		{
			Log.Debug($"Pushing delete tag {tagName} ...");

			CmdResult2 result = await gitCmdService.RunCmdAsync($"{PushArgs} --delete {tagName}", ct);

			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return R.Error("Failed to delete remote tags", result.AsException());
			}

			return R.Ok;
		}


		public async Task<R> PushRefsAsync(IEnumerable<string> refspecs, CancellationToken ct)
		{
			string refsText = string.Join(" ", refspecs);
			Log.Debug($"Pushing refs {refsText} ...");

			CmdResult2 result = await gitCmdService.RunCmdAsync($"{PushArgs} {refsText}", ct);

			if (result.IsFaulted)
			{
				if (IsNoRemote(result))
				{
					return R.Ok;
				}
				return R.Error("Failed to push", result.AsException());
			}

			return R.Ok;
		}

		private bool IsNoRemote(CmdResult2 result) =>
			result.Error.StartsWith("fatal: 'origin' does not appear to be a git repository");

		private bool IsNoRemoteBranch(CmdResult2 result) =>
			result.Error.Contains(" has no upstream ");
	}
}