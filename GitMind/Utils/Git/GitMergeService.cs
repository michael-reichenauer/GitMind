using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git
{
	internal class GitMergeService : IGitMergeService
	{
		private readonly IGitCmdService gitCmdService;


		public GitMergeService(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R> MergeAsync(string name, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"merge --no-ff --no-commit --stat --progress {name}", ct);

			if (result.IsFaulted)
			{
				if (result.ExitCode == 1 && IsConflicts(result))
				{
					Log.Info($"Merge of {name} resulted in conflict(s)");
					return R.Ok;
				}

				return R.Error($"Failed to merge branch {name}", result.AsException());
			}

			Log.Info($"Merge of {name} was OK");
			return R.Ok;
		}


		public async Task<R<bool>> TryMergeFastForwardAsync(string name, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"merge --ff-only --stat --progress {name}", ct);

			if (result.IsFaulted)
			{
				if (result.Error.StartsWith("fatal: Not possible to fast-forward"))
				{
					Log.Info($"Merge of {name} could not be fast forward merged");
					return false;
				}

				return R.Error($"Failed to ff merge branch {name}", result.AsException());
			}

			Log.Info($"Merge of {name} was OK");
			return true;
		}


		private static bool IsConflicts(CmdResult2 result) =>
			result.OutputLines.Any(line => line.StartsWith("CONFLICT ("));
	}
}