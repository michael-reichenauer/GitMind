using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git
{
	internal class GitMergeService2 : IGitMergeService2
	{
		private readonly IGitCmdService gitCmdService;


		public GitMergeService2(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R> MergeAsync(string name, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync(
				$"merge --no-ff --no-commit --stat --progress {name}", ct);

			if (result.IsFaulted)
			{
				if (result.ExitCode == 1 && result.OutputLines.Any(line => line.StartsWith("CONFLICT (")))
				{
					Log.Info($"Merge of {name} resulted in conflict(s)");
					return R.Ok;
				}

				return Error.From($"Failed to merge branch {name}",
					Error.From(string.Join("\n", result.ErrorLines.Take(10)), new GitException($"{result}")));
			}

			Log.Info($"Merge of {name} was OK");
			return result;
		}
	}
}