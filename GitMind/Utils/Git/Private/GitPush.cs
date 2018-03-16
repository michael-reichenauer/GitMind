﻿using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitPush : IGitPush
	{
		private readonly IGitCmd gitCmd;

		private static readonly string PushArgs = @"push --porcelain";


		public GitPush(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<bool> PushAsync(CancellationToken ct)
		{
			using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				ct = cts.Token;

				// In case login failes, we need to detect that 
				CmdOptions options = new CmdOptions
				{
					ErrorProgress = text => ErrorProgress(text, cts),
					//InputText = text => InputText(text, ct)
				};

				CmdResult2 result = await gitCmd.RunAsync(PushArgs, options, ct);

				if (result.ExitCode != 0 && !result.IsCanceled)
				{
					Log.Warn($"Failed to push: {result}");
					return false;
				}
			}

			return true;
		}


		private string InputText(CancellationToken text, CancellationToken ct)
		{
			//await Task.Yield();
			return "x";
		}


		private static void ErrorProgress(string text, CancellationTokenSource cts)
		{
			Log.Debug($"Push error: {text}");
			if (text.Contains("ctrl+c"))
			{
				Log.Warn($"Login failed, {text}");
				cts.Cancel();
			}
		}
	}
}