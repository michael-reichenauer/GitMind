﻿using System.Threading;
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
				return Error.From($"Failed to checkout {name}", result);
			}

			return result;
		}
	}
}