using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCheckoutService : IGitCheckoutService
	{
		private readonly IGitCmdService gitCmdService;


		public GitCheckoutService(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}

		public async Task<R> CheckoutAsync(string name, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync($"checkout --progress {name}", ct);

			if (result.IsFaulted)
			{
				return R.Error($"Failed to checkout {name}", result.Exception);
			}

			Log.Info($"Checked out {name}");
			return result;
		}


		public async Task<R<bool>> TryCheckoutAsync(string name, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync($"checkout --progress {name}", ct);

			if (result.IsFaulted)
			{
				if (IsUnknownName(result, name))
				{
					Log.Info($"Unknown name: {name}");
					return false;
				}

				return R.Error($"Failed to checkout {name}", result.AsException());

			}

			Log.Info($"Checked out {name}");
			return true;
		}


		private static bool IsUnknownName(CmdResult2 result, string name) =>
			result.Error.StartsWith($"error: pathspec '{name}' did not match any file(s) known to git.");
	}
}